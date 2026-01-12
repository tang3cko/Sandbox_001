using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;

namespace Prism.Rain
{
    /// <summary>
    /// URP Render Pass for GPU-based rain simulation and rendering.
    /// Uses Unity 6 RenderGraph API with proper buffer imports and dependency tracking.
    /// </summary>
    public sealed class RainRenderPass : ScriptableRenderPass
    {
        private const int THREAD_GROUP_SIZE = 128;
        private const int MAX_RAIN_LIGHTS = 8;

        private readonly ComputeShader computeShader;
        private readonly Material rainMaterial;
        private readonly RainRendererFeature.RainSettings settings;

        private GraphicsBuffer rainBuffer;
        private GraphicsBuffer visibleRainBuffer;
        private GraphicsBuffer argsBuffer;
        private GraphicsBuffer lightBuffer;
        private Mesh quadMesh;

        private int initKernelID;
        private int updateKernelID;
        private int threadGroupsX;

        private bool isInitialized;
        private uint frameCount;

        private readonly RainLightData[] lightDataArray = new RainLightData[MAX_RAIN_LIGHTS];

        // Shader property IDs
        private static readonly int DropsID = Shader.PropertyToID("_Drops");
        private static readonly int VisibleDropsID = Shader.PropertyToID("_VisibleDrops");
        private static readonly int RainBufferID = Shader.PropertyToID("_RainBuffer");
        private static readonly int LightBufferID = Shader.PropertyToID("_LightBuffer");
        private static readonly int LightCountID = Shader.PropertyToID("_LightCount");
        private static readonly int DeltaTimeID = Shader.PropertyToID("_DeltaTime");
        private static readonly int TimeID = Shader.PropertyToID("_Time");
        private static readonly int FrameCountID = Shader.PropertyToID("_FrameCount");
        private static readonly int SpawnRadiusID = Shader.PropertyToID("_SpawnRadius");
        private static readonly int HeightMinID = Shader.PropertyToID("_HeightMin");
        private static readonly int HeightMaxID = Shader.PropertyToID("_HeightMax");
        private static readonly int GravityID = Shader.PropertyToID("_Gravity");
        private static readonly int CameraPositionID = Shader.PropertyToID("_CameraPosition");
        private static readonly int CullDistanceID = Shader.PropertyToID("_CullDistance");
        private static readonly int RainDropScaleID = Shader.PropertyToID("_RainDropScale");
        private static readonly int RainBaseAlphaID = Shader.PropertyToID("_RainBaseAlpha");
        private static readonly int RainLitAlphaID = Shader.PropertyToID("_RainLitAlpha");
        private static readonly int ViewProjectionMatrixID = Shader.PropertyToID("_ViewProjectionMatrix");

        private class PassData
        {
            public ComputeShader computeShader;
            public Material rainMaterial;
            public RainRendererFeature.RainSettings settings;

            // Buffer handles for RenderGraph dependency tracking
            public BufferHandle rainBufferHandle;
            public BufferHandle visibleRainBufferHandle;
            public BufferHandle argsBufferHandle;
            public BufferHandle lightBufferHandle;

            // Render target handles for UnsafePass
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;

            // Raw buffers for API calls
            public GraphicsBuffer rainBuffer;
            public GraphicsBuffer visibleRainBuffer;
            public GraphicsBuffer argsBuffer;
            public GraphicsBuffer lightBuffer;

            public Mesh quadMesh;

            public int updateKernelID;
            public int threadGroupsX;
            public uint frameCount;
            public float deltaTime;
            public float time;

            public Vector3 cameraPosition;
            public Matrix4x4 viewProjectionMatrix;
            public int lightCount;
        }

        public RainRenderPass(ComputeShader computeShader, Material rainMaterial, RainRendererFeature.RainSettings settings)
        {
            this.computeShader = computeShader;
            this.rainMaterial = rainMaterial;
            this.settings = settings;

            profilingSampler = new ProfilingSampler("Rain");
        }

        private void Initialize()
        {
            if (isInitialized) return;

            int rainDropStride = sizeof(float) * 12;

            rainBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                settings.rainDropCount,
                rainDropStride
            );

            visibleRainBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Append,
                settings.rainDropCount,
                rainDropStride
            );

            lightBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                MAX_RAIN_LIGHTS,
                RainLightData.Stride
            );

            uint[] args = new uint[] { 6, 0, 0, 0, 0 };
            argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                sizeof(uint) * 5
            );
            argsBuffer.SetData(args);

            quadMesh = new Mesh();
            quadMesh.name = "RainQuad";
            quadMesh.vertices = new Vector3[6];
            quadMesh.triangles = new int[] { 0, 1, 2, 3, 4, 5 };
            quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one * settings.renderBoundsSize);

            initKernelID = computeShader.FindKernel("InitRain");
            updateKernelID = computeShader.FindKernel("UpdateRain");
            threadGroupsX = Mathf.CeilToInt(settings.rainDropCount / (float)THREAD_GROUP_SIZE);

            // Initialize rain drops (one-time setup)
            computeShader.SetFloat(SpawnRadiusID, settings.spawnRadius);
            computeShader.SetFloat(HeightMinID, settings.heightMin);
            computeShader.SetFloat(HeightMaxID, settings.heightMax);
            computeShader.SetVector(CameraPositionID, Vector3.zero); // 初期化時は原点
            computeShader.SetFloat(RainDropScaleID, settings.dropScale);
            computeShader.SetBuffer(initKernelID, DropsID, rainBuffer);
            computeShader.Dispatch(initKernelID, threadGroupsX, 1, 1);

            isInitialized = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Initialize();
            if (!isInitialized) return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            int lightCount = CollectLights(lightData.visibleLights, lightData.additionalLightsCount, lightData.mainLightIndex);
            lightBuffer.SetData(lightDataArray);

            frameCount++;

            using (var builder = renderGraph.AddUnsafePass<PassData>("Rain Pass", out var passData, profilingSampler))
            {
                // Import buffers into RenderGraph
                passData.rainBufferHandle = renderGraph.ImportBuffer(rainBuffer);
                passData.visibleRainBufferHandle = renderGraph.ImportBuffer(visibleRainBuffer);
                passData.argsBufferHandle = renderGraph.ImportBuffer(argsBuffer);
                passData.lightBufferHandle = renderGraph.ImportBuffer(lightBuffer);

                // Get render targets from UniversalResourceData
                passData.colorTarget = resourceData.activeColorTexture;
                passData.depthTarget = resourceData.activeDepthTexture;

                // Declare buffer usage for dependency tracking
                builder.UseBuffer(passData.rainBufferHandle, AccessFlags.ReadWrite);
                builder.UseBuffer(passData.visibleRainBufferHandle, AccessFlags.ReadWrite);
                builder.UseBuffer(passData.argsBufferHandle, AccessFlags.ReadWrite);
                builder.UseBuffer(passData.lightBufferHandle, AccessFlags.Read);

                // Declare render target usage (required for UnsafePass)
                // Color: Write (drawing rain particles)
                // Depth: Read (for depth testing, ZWrite is Off in shader)
                builder.UseTexture(passData.colorTarget, AccessFlags.Write);
                builder.UseTexture(passData.depthTarget, AccessFlags.Read);

                // Pass raw buffers for API calls
                passData.rainBuffer = rainBuffer;
                passData.visibleRainBuffer = visibleRainBuffer;
                passData.argsBuffer = argsBuffer;
                passData.lightBuffer = lightBuffer;

                passData.computeShader = computeShader;
                passData.rainMaterial = rainMaterial;
                passData.settings = settings;
                passData.quadMesh = quadMesh;
                passData.updateKernelID = updateKernelID;
                passData.threadGroupsX = threadGroupsX;
                passData.frameCount = frameCount;
                passData.deltaTime = Time.deltaTime;
                passData.time = Time.time;
                passData.cameraPosition = cameraData.camera.transform.position;
                passData.viewProjectionMatrix = GL.GetGPUProjectionMatrix(cameraData.camera.projectionMatrix, true)
                                               * cameraData.camera.worldToCameraMatrix;
                passData.lightCount = lightCount;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    // Set render target using UnsafeCommandBuffer (accepts TextureHandle directly)
                    // Reference: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-unsafe-pass.html
                    context.cmd.SetRenderTarget(data.colorTarget, data.depthTarget);

                    // Get native CommandBuffer for other operations
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                    // Reset AppendBuffer counter
                    cmd.SetBufferCounterValue(data.visibleRainBuffer, 0);

                    // Set compute parameters
                    cmd.SetComputeFloatParam(data.computeShader, DeltaTimeID, data.deltaTime);
                    cmd.SetComputeFloatParam(data.computeShader, TimeID, data.time);
                    cmd.SetComputeIntParam(data.computeShader, FrameCountID, (int)data.frameCount);

                    // Spawn area parameters
                    cmd.SetComputeFloatParam(data.computeShader, SpawnRadiusID, data.settings.spawnRadius);
                    cmd.SetComputeFloatParam(data.computeShader, HeightMinID, data.settings.heightMin);
                    cmd.SetComputeFloatParam(data.computeShader, HeightMaxID, data.settings.heightMax);

                    cmd.SetComputeFloatParam(data.computeShader, GravityID, data.settings.gravity);
                    cmd.SetComputeFloatParam(data.computeShader, CullDistanceID, data.settings.cullDistance);
                    cmd.SetComputeFloatParam(data.computeShader, RainDropScaleID, data.settings.dropScale);
                    cmd.SetComputeVectorParam(data.computeShader, CameraPositionID, data.cameraPosition);
                    cmd.SetComputeMatrixParam(data.computeShader, ViewProjectionMatrixID, data.viewProjectionMatrix);

                    // Set compute buffers
                    cmd.SetComputeBufferParam(data.computeShader, data.updateKernelID, DropsID, data.rainBuffer);
                    cmd.SetComputeBufferParam(data.computeShader, data.updateKernelID, VisibleDropsID, data.visibleRainBuffer);

                    // Dispatch compute
                    cmd.DispatchCompute(data.computeShader, data.updateKernelID, data.threadGroupsX, 1, 1);

                    // Copy visible count to args buffer
                    cmd.CopyCounterValue(data.visibleRainBuffer, data.argsBuffer, sizeof(uint));

                    // Set material properties
                    data.rainMaterial.SetBuffer(RainBufferID, data.visibleRainBuffer);
                    data.rainMaterial.SetBuffer(LightBufferID, data.lightBuffer);
                    data.rainMaterial.SetInt(LightCountID, data.lightCount);
                    data.rainMaterial.SetFloat(RainBaseAlphaID, data.settings.baseAlpha);
                    data.rainMaterial.SetFloat(RainLitAlphaID, data.settings.litAlpha);

                    // Draw
                    cmd.DrawMeshInstancedIndirect(data.quadMesh, 0, data.rainMaterial, 0, data.argsBuffer);
                });
            }
        }

        private int CollectLights(NativeArray<VisibleLight> visibleLights, int additionalLightsCount, int mainLightIndex)
        {
            int lightCount = 0;
            int additionalLightIndex = 0;

            for (int i = 0; i < MAX_RAIN_LIGHTS; i++)
            {
                lightDataArray[i] = default;
            }

            int maxIndex = Mathf.Min(visibleLights.Length, additionalLightsCount + 1);
            for (int i = 0; i < maxIndex && lightCount < MAX_RAIN_LIGHTS; i++)
            {
                // Skip main light (it's not an additional light)
                if (i == mainLightIndex)
                    continue;

                Light light = visibleLights[i].light;
                if (light == null)
                {
                    additionalLightIndex++;
                    continue;
                }

                if (!light.TryGetComponent<RainAdditionalLight>(out var rainLight))
                {
                    additionalLightIndex++;
                    continue;
                }

                if (!rainLight.enabled || !rainLight.gameObject.activeInHierarchy)
                {
                    additionalLightIndex++;
                    continue;
                }

                if (light.type != LightType.Point && light.type != LightType.Spot)
                {
                    additionalLightIndex++;
                    continue;
                }

                lightDataArray[lightCount] = RainLightData.FromLight(light, additionalLightIndex);
                lightCount++;
                additionalLightIndex++;
            }

            return lightCount;
        }

        public void Dispose()
        {
            rainBuffer?.Release();
            visibleRainBuffer?.Release();
            argsBuffer?.Release();
            lightBuffer?.Release();

            if (quadMesh != null)
            {
                Object.DestroyImmediate(quadMesh);
            }

            isInitialized = false;
        }
    }
}
