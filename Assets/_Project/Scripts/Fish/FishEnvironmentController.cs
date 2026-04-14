using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.Core.Fish
{
    [RequireComponent(typeof(AudioSource))]
    public class FishEnvironmentController : MonoBehaviour
    {
        [Header("Site Data")]
        [SerializeField] private FishingSiteDataSO currentSite;

        [Header("Scene References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Transform environmentRoot;

        [Header("Debug Visuals")]
        [SerializeField] private bool createDebugEnvironment = true;
        [SerializeField] private bool applyOnStart = true;

        private AudioSource audioSource;
        private GameObject debugEnvironmentInstance;

        public FishingSiteDataSO CurrentSite => currentSite;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;

            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyEnvironment();
            }
        }

        public void ApplyEnvironment()
        {
            if (currentSite == null)
            {
                Debug.LogWarning("[FishEnvironmentController] ApplyEnvironment skipped: currentSite is not assigned.");
                return;
            }

            ApplySkybox();
            ApplyAmbientSound();

            if (createDebugEnvironment)
            {
                BuildDebugEnvironment();
            }

            Debug.Log(
                $"[FishEnvironmentController] Environment applied: site={currentSite.DisplayName}, " +
                $"scene={currentSite.SceneName}, backgroundType={currentSite.BackgroundType}");
        }

        private void ApplySkybox()
        {
            if (currentSite.SkyboxMaterial != null)
            {
                RenderSettings.skybox = currentSite.SkyboxMaterial;
                DynamicGI.UpdateEnvironment();
                return;
            }

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = GetFallbackBackgroundColor(currentSite.BackgroundType);
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        private void ApplyAmbientSound()
        {
            audioSource.clip = currentSite.AmbientSound;

            if (audioSource.clip != null)
            {
                audioSource.Play();
                Debug.Log($"[FishEnvironmentController] Ambient sound started: {audioSource.clip.name}");
            }
            else
            {
                audioSource.Stop();
                Debug.Log("[FishEnvironmentController] Ambient sound is not assigned. Visual-only environment applied.");
            }
        }

        private void BuildDebugEnvironment()
        {
            if (debugEnvironmentInstance != null)
            {
                Destroy(debugEnvironmentInstance);
            }

            Transform parent = environmentRoot != null ? environmentRoot : transform;
            debugEnvironmentInstance = new GameObject($"ENV_{currentSite.SiteId}_Debug");
            debugEnvironmentInstance.transform.SetParent(parent);
            debugEnvironmentInstance.transform.localPosition = Vector3.zero;
            debugEnvironmentInstance.transform.localRotation = Quaternion.identity;

            CreateGround(debugEnvironmentInstance.transform);
            CreateBackdrop(debugEnvironmentInstance.transform);
            CreateSiteLabel(debugEnvironmentInstance.transform);
            ApplyDirectionalLightTint();
        }

        private void CreateGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetGroundColor(currentSite.BackgroundType);
            }
        }

        private void CreateBackdrop(Transform parent)
        {
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Backdrop";
            backdrop.transform.SetParent(parent);
            backdrop.transform.localPosition = new Vector3(0f, 3f, 8f);
            backdrop.transform.localScale = new Vector3(20f, 6f, 0.5f);

            Renderer renderer = backdrop.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetFallbackBackgroundColor(currentSite.BackgroundType);
            }
        }

        private void CreateSiteLabel(Transform parent)
        {
            GameObject labelRoot = new GameObject("SiteLabel");
            labelRoot.transform.SetParent(parent);
            labelRoot.transform.localPosition = new Vector3(0f, 2.25f, 4f);

            TextMesh textMesh = labelRoot.AddComponent<TextMesh>();
            textMesh.text = $"{currentSite.DisplayName}\n{currentSite.BackgroundType}";
            textMesh.characterSize = 0.15f;
            textMesh.fontSize = 48;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }

        private void ApplyDirectionalLightTint()
        {
            if (directionalLight == null)
            {
                return;
            }

            directionalLight.color = GetLightColor(currentSite.BackgroundType);
        }

        private static Color GetFallbackBackgroundColor(BackgroundType backgroundType)
        {
            return backgroundType switch
            {
                BackgroundType.River => new Color(0.45f, 0.75f, 0.92f),
                BackgroundType.Lake => new Color(0.36f, 0.62f, 0.85f),
                BackgroundType.Sea => new Color(0.2f, 0.42f, 0.72f),
                BackgroundType.Pond => new Color(0.55f, 0.76f, 0.62f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        private static Color GetGroundColor(BackgroundType backgroundType)
        {
            return backgroundType switch
            {
                BackgroundType.River => new Color(0.45f, 0.42f, 0.32f),
                BackgroundType.Lake => new Color(0.38f, 0.48f, 0.3f),
                BackgroundType.Sea => new Color(0.76f, 0.72f, 0.52f),
                BackgroundType.Pond => new Color(0.32f, 0.45f, 0.28f),
                _ => new Color(0.35f, 0.35f, 0.35f)
            };
        }

        private static Color GetLightColor(BackgroundType backgroundType)
        {
            return backgroundType switch
            {
                BackgroundType.River => new Color(1f, 0.96f, 0.86f),
                BackgroundType.Lake => new Color(0.95f, 0.95f, 0.9f),
                BackgroundType.Sea => new Color(0.85f, 0.92f, 1f),
                BackgroundType.Pond => new Color(1f, 0.93f, 0.82f),
                _ => Color.white
            };
        }
    }
}
