using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Prism.RenderingTest.UI
{
    /// <summary>
    /// UI controller for the Rendering Test HUD.
    /// Displays rendering mode selection, light controls, and performance stats.
    /// Uses UI Toolkit's KeyDownEvent for keyboard input.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class RenderingTestUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RenderPipelineSwitcher pipelineSwitcher;
        [SerializeField] private LightSpawner lightSpawner;

        [Header("FPS Settings")]
        [SerializeField] private float fpsUpdateInterval = 0.5f;

        private UIDocument uiDocument;
        private VisualElement root;

        // Mode panel elements
        private DropdownField modeDropdown;

        // Light panel elements
        private Label lightCountText;
        private SliderInt lightSlider;
        private Button addLightsButton;
        private Button removeLightsButton;
        private Toggle animationToggle;

        // Stats panel elements
        private Label fpsValue;
        private Label frameTimeValue;
        private Label drawCallsValue;
        private Label batchesValue;
        private Label setPassCallsValue;
        private Label trianglesValue;

        // FPS calculation
        private float fps;
        private float frameTime;
        private float fpsAccumulator;
        private int frameCount;
        private float fpsTimer;

        private bool isUIVisible = true;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;

            QueryElements();
            SetupModeDropdown();
            SetupLightControls();
            SetupKeyboardInput();
        }

        private void OnDisable()
        {
            if (root != null)
            {
                root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }
        }

        private void Update()
        {
            UpdateFPS();
            UpdateStats();
            UpdateLightUI();
        }

        private void SetupKeyboardInput()
        {
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.schedule.Execute(() => root.Focus());
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                // Rendering mode controls
                case KeyCode.R:
                    pipelineSwitcher?.CycleMode();
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha1:
                    pipelineSwitcher?.SetMode(0);
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha2:
                    pipelineSwitcher?.SetMode(1);
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha3:
                    pipelineSwitcher?.SetMode(2);
                    evt.StopPropagation();
                    break;

                // Light controls
                case KeyCode.UpArrow:
                    lightSpawner?.AddLights(10);
                    evt.StopPropagation();
                    break;
                case KeyCode.DownArrow:
                    lightSpawner?.RemoveLights(10);
                    evt.StopPropagation();
                    break;
                case KeyCode.A:
                    if (lightSpawner != null)
                    {
                        lightSpawner.AnimateLights = !lightSpawner.AnimateLights;
                        if (animationToggle != null)
                        {
                            animationToggle.SetValueWithoutNotify(lightSpawner.AnimateLights);
                        }
                    }
                    evt.StopPropagation();
                    break;

                // UI toggle
                case KeyCode.F1:
                    isUIVisible = !isUIVisible;
                    root.style.display = isUIVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    evt.StopPropagation();
                    break;
            }
        }

        private void QueryElements()
        {
            // Mode panel
            modeDropdown = root.Q<DropdownField>("ModeDropdown");

            // Light panel
            lightCountText = root.Q<Label>("LightCountText");
            lightSlider = root.Q<SliderInt>("LightSlider");
            addLightsButton = root.Q<Button>("AddLightsButton");
            removeLightsButton = root.Q<Button>("RemoveLightsButton");
            animationToggle = root.Q<Toggle>("AnimationToggle");

            // Stats panel
            fpsValue = root.Q<Label>("FPSValue");
            frameTimeValue = root.Q<Label>("FrameTimeValue");
            drawCallsValue = root.Q<Label>("DrawCallsValue");
            batchesValue = root.Q<Label>("BatchesValue");
            setPassCallsValue = root.Q<Label>("SetPassCallsValue");
            trianglesValue = root.Q<Label>("TrianglesValue");
        }

        private void SetupModeDropdown()
        {
            if (modeDropdown == null || pipelineSwitcher == null) return;

            var choices = new List<string> { "Forward", "Forward+", "Deferred" };
            modeDropdown.choices = choices;
            modeDropdown.index = (int)pipelineSwitcher.CurrentMode;

            modeDropdown.RegisterValueChangedCallback(evt =>
            {
                int index = choices.IndexOf(evt.newValue);
                if (index >= 0)
                {
                    pipelineSwitcher.SetMode(index);
                }
            });
        }

        private void SetupLightControls()
        {
            if (lightSpawner == null) return;

            // Slider
            if (lightSlider != null)
            {
                lightSlider.highValue = lightSpawner.MaxLightCount;
                lightSlider.value = lightSpawner.LightCount;
                lightSlider.RegisterValueChangedCallback(evt =>
                {
                    lightSpawner.SetLightCount(evt.newValue);
                });
            }

            // Buttons
            if (addLightsButton != null)
            {
                addLightsButton.clicked += () => lightSpawner.AddLights(10);
            }

            if (removeLightsButton != null)
            {
                removeLightsButton.clicked += () => lightSpawner.RemoveLights(10);
            }

            // Animation toggle
            if (animationToggle != null)
            {
                animationToggle.value = lightSpawner.AnimateLights;
                animationToggle.RegisterValueChangedCallback(evt =>
                {
                    lightSpawner.AnimateLights = evt.newValue;
                });
            }
        }

        private void UpdateFPS()
        {
            fpsAccumulator += Time.unscaledDeltaTime;
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= fpsUpdateInterval)
            {
                fps = frameCount / fpsAccumulator;
                frameTime = fpsAccumulator / frameCount * 1000f;

                fpsAccumulator = 0f;
                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        private void UpdateStats()
        {
            // FPS with color coding
            if (fpsValue != null)
            {
                fpsValue.text = $"{fps:F1}";
                UpdateFPSColor(fpsValue, fps);
            }

            if (frameTimeValue != null)
            {
                frameTimeValue.text = $"{frameTime:F2} ms";
            }

#if UNITY_EDITOR
            if (drawCallsValue != null)
                drawCallsValue.text = UnityEditor.UnityStats.drawCalls.ToString();

            if (batchesValue != null)
                batchesValue.text = UnityEditor.UnityStats.batches.ToString();

            if (setPassCallsValue != null)
                setPassCallsValue.text = UnityEditor.UnityStats.setPassCalls.ToString();

            if (trianglesValue != null)
                trianglesValue.text = FormatNumber(UnityEditor.UnityStats.triangles);
#endif

            // Update mode dropdown if changed externally
            if (modeDropdown != null && pipelineSwitcher != null)
            {
                int currentMode = (int)pipelineSwitcher.CurrentMode;
                if (modeDropdown.index != currentMode)
                {
                    modeDropdown.SetValueWithoutNotify(modeDropdown.choices[currentMode]);
                }
            }
        }

        private void UpdateLightUI()
        {
            if (lightSpawner == null) return;

            if (lightCountText != null)
            {
                lightCountText.text = $"{lightSpawner.LightCount} / {lightSpawner.MaxLightCount}";
            }

            if (lightSlider != null && lightSlider.value != lightSpawner.LightCount)
            {
                lightSlider.SetValueWithoutNotify(lightSpawner.LightCount);
            }
        }

        private void UpdateFPSColor(Label label, float currentFps)
        {
            label.RemoveFromClassList("rendering-test__stats-value--good");
            label.RemoveFromClassList("rendering-test__stats-value--warning");
            label.RemoveFromClassList("rendering-test__stats-value--bad");

            if (currentFps >= 60f)
            {
                label.AddToClassList("rendering-test__stats-value--good");
            }
            else if (currentFps >= 30f)
            {
                label.AddToClassList("rendering-test__stats-value--warning");
            }
            else
            {
                label.AddToClassList("rendering-test__stats-value--bad");
            }
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F2}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString();
        }
    }
}
