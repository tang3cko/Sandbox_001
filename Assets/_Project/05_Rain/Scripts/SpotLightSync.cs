using UnityEngine;
using Tang3cko.ReactiveSO;

namespace Prism.Rain
{
    /// <summary>
    /// Syncs Light component parameters to VariableSO assets for GPU access.
    /// Attach to the same GameObject as the Light component.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class SpotLightSync : MonoBehaviour
    {
        [Header("Light Reference")]
        [SerializeField] private Light spotLight;

        [Header("VariableSO References")]
        [SerializeField] private Vector3VariableSO lightPositionVar;
        [SerializeField] private Vector3VariableSO lightDirectionVar;
        [SerializeField] private FloatVariableSO lightRangeVar;
        [SerializeField] private FloatVariableSO spotAngleVar;
        [SerializeField] private FloatVariableSO innerSpotAngleVar;
        [SerializeField] private FloatVariableSO lightIntensityVar;
        [SerializeField] private ColorVariableSO lightColorVar;

        private void Reset()
        {
            spotLight = GetComponent<Light>();
        }

        private void OnEnable()
        {
            if (spotLight == null)
            {
                spotLight = GetComponent<Light>();
            }

            SyncToVariables();
        }

        private void Update()
        {
            SyncToVariables();
        }

        private void SyncToVariables()
        {
            if (spotLight == null) return;

            if (lightPositionVar != null)
            {
                lightPositionVar.Value = transform.position;
            }

            if (lightDirectionVar != null)
            {
                lightDirectionVar.Value = transform.forward;
            }

            if (lightRangeVar != null)
            {
                lightRangeVar.Value = spotLight.range;
            }

            if (spotAngleVar != null)
            {
                spotAngleVar.Value = spotLight.spotAngle;
            }

            if (innerSpotAngleVar != null)
            {
                innerSpotAngleVar.Value = spotLight.innerSpotAngle;
            }

            if (lightIntensityVar != null)
            {
                lightIntensityVar.Value = spotLight.intensity;
            }

            if (lightColorVar != null)
            {
                lightColorVar.Value = spotLight.color;
            }
        }
    }
}
