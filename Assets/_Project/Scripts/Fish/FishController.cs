using System;
using System.Collections;
using UnityEngine;
using VirtualFishing.Data;
using VirtualFishing.Fishing;
using VirtualFishing.Fishing.Events;
using VirtualFishing.Interfaces;
using VirtualFishing.MiniGame;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VirtualFishing.Core.Fish
{
    public class FishController : MonoBehaviour, IFish
    {
        [Serializable]
        private sealed class SpeciesCatchEffect
        {
            public FishSpeciesDataSO species;
            public string fishId;
            public GameObject effectPrefab;

            public bool Matches(FishSpeciesDataSO current)
            {
                if (current == null || effectPrefab == null)
                {
                    return false;
                }

                if (species == current)
                {
                    return true;
                }

                return !string.IsNullOrWhiteSpace(fishId)
                    && string.Equals(fishId, current.FishId, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Header("Visual Test Setup")]
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Vector3 spawnOffset = new(0f, 0.5f, 2.5f);
        [SerializeField] private bool flipVisualForward = true;
        [SerializeField] private bool createPlaceholderWhenPrefabMissing = true;
        [SerializeField] private bool clampMovementWithinRange = true;
        [SerializeField] private float horizontalMoveLimit = 10.0f;

        [Header("Bite Splash")]
        [SerializeField] private Transform splashRoot;
        [SerializeField] private ParticleSystem splashPrefab;
        [SerializeField] private bool createDefaultSplashWhenMissing = true;
        [SerializeField] private float splashWaterHeight = 0.14f;
        [SerializeField] private float splashFollowYOffset = 0.04f;

        [Header("Hook Success Preview")]
        [SerializeField] private Transform hookSuccessAttachTarget;
        [SerializeField] private Vector3 hookSuccessLocalOffset = new(0f, -0.05f, 0f);
        [SerializeField] private Vector3 hookSuccessLocalEuler = new(90f, 85f, 0f);
        [SerializeField] private bool hideVisualUntilHookSuccess = true;
        [SerializeField] private float reelingPullMinDistanceFromFloat = 0.15f;
        [SerializeField] private bool moveFloatDuringReelingPreview = true;
        [SerializeField] private bool createPreviewFloatWhenMissing = true;
        [SerializeField] private float previewFloatWaterHeight = 0.2f;
        [SerializeField] private float previewFloatAirDistance = 1.8f;
        [SerializeField] private float previewFloatAirDrop = 0.55f;
        [SerializeField] private float previewFloatFlyDuration = 1.2f;
        [SerializeField] private float hookSuccessSplashScale = 2f;
        [SerializeField] private float hookedVisualScaleMultiplier = 1f;
        [SerializeField] private float hookedFlopAngle = 18f;
        [SerializeField] private float hookedFlopSpeed = 11f;
        [SerializeField] private float hookedFlopVerticalAmount = 0.035f;

        [Header("Catch Success Effect")]
        [SerializeField] private Transform catchEffectRoot;
        [SerializeField] private GameObject calmCatchEffectPrefab;
        [SerializeField] private GameObject aggressiveCatchEffectPrefab;
        [SerializeField] private GameObject erraticCatchEffectPrefab;
        [SerializeField] private GameObject rareCatchEffectPrefab;
        [SerializeField] private SpeciesCatchEffect[] speciesCatchEffects = new SpeciesCatchEffect[0];
        [SerializeField] private float catchEffectLifetime = 3f;
        [SerializeField] private bool useEditorNamuFxFallback = true;

        [Header("Scenario Lifecycle")]
        [SerializeField] private FishingRodController rodController;
        [SerializeField] private bool clearFishWhenRodReturnsToReady = true;
        [SerializeField] private bool clearFishWhenRodReleased = true;
        [SerializeField] private bool clearFishOnMiniGameFailure = true;

        [Header("Runtime State")]
        [SerializeField] private FishSpeciesDataSO currentSpecies;
        [SerializeField] private float weight;
        [SerializeField] private float resistance;
        [SerializeField] private MovementPattern pattern;
        [SerializeField] private float sizeCm;
        [SerializeField] private FishPhase currentPhase = FishPhase.None;
        [SerializeField] private FishMoveMode currentMoveMode = FishMoveMode.Stop;
        [SerializeField] private bool isPhaseMovementActive;
        [SerializeField] private float debugMoveSpeed = 1.5f;
        [SerializeField] private float phaseCompleteSlowdownStep = 0.15f;
        [SerializeField] private float minimumPhaseCompleteSpeedMultiplier = 0.45f;
        [SerializeField] private Vector3 visualSpawnPosition;
        [SerializeField, Range(0f, 100f)] private float inspectorDebugSuccessGauge = 50f;
        [SerializeField] private bool isWaitingAtMovementLimit;
        [SerializeField] private int phaseCompleteCount;
        [SerializeField] private FishPhase inspectorDebugPhase = FishPhase.Phase2;
        [SerializeField] private float currentDifficulty;
        [SerializeField] private MiniGameManager miniGameManager;
        // 프로젝트에서 하나의 낚시터만 사용하므로 하나의 낚시터 타입만 사용 (추후 낚시터 추가시 수정 필요)
        [SerializeField] private BackgroundType miniGameSiteType = BackgroundType.Pond;

        private GameObject currentVisualInstance;
        private ParticleSystem currentSplashInstance;
        private Vector3 visualBaseLocalScale = Vector3.one;
        private Transform previewFloatTransform;
        private Vector3 previewFloatStartPosition;
        private bool hasPreviewFloatStartPosition;
        private Coroutine previewFloatFlyCoroutine;
        private bool isHookSuccessPreviewActive;
        private Vector3 hookedBaseLocalPosition;
        private Quaternion hookedBaseLocalRotation;
        private bool miniGameEventsSubscribed;
        private bool rodEventsSubscribed;

        public FishSpeciesDataSO CurrentSpecies => currentSpecies;
        public string SpeciesName => currentSpecies != null ? currentSpecies.DisplayName : string.Empty;
        public float Weight => weight;
        public float Resistance => resistance;
        public MovementPattern Pattern => pattern;
        public float SizeCm => sizeCm;
        public float CurrentDifficulty => currentDifficulty;
        public FishPhase CurrentPhase => currentPhase;
        public FishMoveMode CurrentMoveMode => currentMoveMode;
        public bool IsHookSuccessPreviewActive => isHookSuccessPreviewActive;
        public FishPhase InspectorDebugPhase
        {
            get => inspectorDebugPhase;
            set => inspectorDebugPhase = value;
        }
        // UI가 물고기 비주얼을 따라다닐 때 사용. 비주얼이 없으면 자신의 transform 반환
        public Transform VisualTransform => currentVisualInstance != null
            ? currentVisualInstance.transform
            : transform;

        public event Action<Vector3> OnFishMoved;

        private void OnEnable()
        {
            SubscribeMiniGameEvents();
            SubscribeRodEvents();
        }

        private void Start()
        {
            SubscribeMiniGameEvents();
            SubscribeRodEvents();
        }

        private void OnDisable()
        {
            UnsubscribeMiniGameEvents();
            UnsubscribeRodEvents();
            StopPhaseMovement();
            StopSplashEffect();
        }

        private void Update()
        {
            if (!isPhaseMovementActive || currentVisualInstance == null)
            {
                UpdateHookedFlop();
                return;
            }

            Vector3 movementDirection = GetMovementDirectionByMode(currentMoveMode);
            if (movementDirection == Vector3.zero)
            {
                return;
            }

            float moveSpeed = GetMoveSpeed();
            currentVisualInstance.transform.position += movementDirection * (moveSpeed * Time.deltaTime);
            ClampVisualPosition();
            ApplyVisualDirection(movementDirection);
            UpdateSplashPosition();
            UpdateHookedFlop();
        }

        public void Initialize(FishSpeciesDataSO speciesData)
        {
            if (speciesData == null)
            {
                Debug.LogWarning("[FishController] Initialize failed: speciesData is null.");
                ResetFish();
                return;
            }

            currentSpecies = speciesData;
            weight = speciesData.GetRandomWeightKg();
            sizeCm = speciesData.GetRandomSizeCm();
            resistance = speciesData.BaseResistance;
            pattern = speciesData.MovementPattern;
            currentDifficulty = CalculateDifficulty();
            SpawnVisual(speciesData);
            BeginPhaseMovement();
            StartSplashEffect();

            Debug.Log(
                $"[FishController] Initialized fish: id={speciesData.FishId}, name={speciesData.DisplayName}, " +
                $"weight={weight:F2}kg, size={sizeCm:F1}cm, resistance={resistance:F2}, difficulty={currentDifficulty:F2}, pattern={pattern}, phase={currentPhase}, moveMode={currentMoveMode}");

            // TODO: Replace this local test visual flow with the team's production fish presentation pipeline.
        }

        public void ResetFish()
        {
            StopPhaseMovement();
            StopSplashEffect();
            ClearVisual();
            currentSpecies = null;
            weight = 0f;
            resistance = 0f;
            pattern = MovementPattern.Calm;
            sizeCm = 0f;
            currentDifficulty = 0f;
            currentPhase = FishPhase.None;
            currentMoveMode = FishMoveMode.Stop;
            phaseCompleteCount = 0;
            isWaitingAtMovementLimit = false;
            isHookSuccessPreviewActive = false;
            ClearPreviewFloat();

            Debug.Log("[FishController] Fish state reset.");
        }

        public void ExecuteMovement()
        {
            if (currentSpecies == null)
            {
                Debug.LogWarning("[FishController] ExecuteMovement skipped: no fish has been initialized.");
                return;
            }

            Vector3 movementDirection = GetMovementDirectionByMode(currentMoveMode);

            if (currentVisualInstance != null)
            {
                currentVisualInstance.transform.position += movementDirection * 0.25f;
                ClampVisualPosition();

                if (movementDirection != Vector3.zero)
                {
                    ApplyVisualDirection(movementDirection);
                }

                UpdateSplashPosition();
            }

            Debug.Log($"[FishController] ExecuteMovement: mode={currentMoveMode}, direction={movementDirection}");
            OnFishMoved?.Invoke(movementDirection);

            // TODO: Replace the placeholder movement output with actual fish AI during mini-game integration.
        }

        public void SetPhase(FishPhase nextPhase)
        {
            if (currentSpecies == null)
            {
                Debug.LogWarning($"[FishController] SetPhase skipped: no fish has been initialized. requestedPhase={nextPhase}");
                return;
            }

            if (nextPhase is FishPhase.None)
            {
                currentPhase = FishPhase.None;
                StopPhaseMovement();
                NotifyMiniGameMoveState();
                Debug.Log("[FishController] Phase cleared. Fish movement stopped.");
                return;
            }

            if (nextPhase == currentPhase)
            {
                Debug.Log($"[FishController] SetPhase ignored: already in {currentPhase}.");
                return;
            }

            currentPhase = nextPhase;
            currentMoveMode = GetWeightedRandomMoveMode();
            isPhaseMovementActive = true;
            isWaitingAtMovementLimit = false;
            NotifyMiniGameMoveState();

            Debug.Log(
                $"[FishController] Phase changed: species={SpeciesName}, phase={currentPhase}, moveMode={currentMoveMode}, speed={GetMoveSpeed():F2}");
        }

        public void ApplyInspectorDebugPhase()
        {
            SetPhase(inspectorDebugPhase);
        }

        public void TriggerRandomMoveMode()
        {
            SetMoveMode(GetWeightedRandomMoveMode());
            Debug.Log($"[FishController] Random move trigger applied: mode={currentMoveMode}");
        }

        public void SetMoveMode(FishMoveMode nextMoveMode)
        {
            currentMoveMode = nextMoveMode;
            isPhaseMovementActive = currentSpecies != null;
            isWaitingAtMovementLimit = false;
            NotifyMiniGameMoveState();
            Debug.Log($"[FishController] Move mode changed by external trigger: mode={currentMoveMode}");
        }

        public void PreviewHookSuccess()
        {
            if (currentVisualInstance == null)
            {
                Debug.LogWarning("[FishController] PreviewHookSuccess skipped: no active fish visual. Start a bite first.");
                return;
            }

            AttachHookedFishToFloat();

            Debug.Log("[FishController] Hook success preview applied.");
        }

        public void SimulateLineBreak()
        {
            ClearFishForScenario("line break");
        }

        public void SimulateFishEscape()
        {
            ClearFishForScenario("fish escaped");
        }

        public void PreviewReelingPull()
        {
            PreviewReelingPull(inspectorDebugSuccessGauge);
        }

        public void PreviewReelingPull(float successGauge)
        {
            if (currentVisualInstance == null)
            {
                Debug.LogWarning("[FishController] PreviewReelingPull skipped: no active fish visual. Start a bite first.");
                return;
            }

            MoveFishTowardFloat(successGauge);
            MoveFloatForReelingPreview(successGauge);
            Debug.Log($"[FishController] Reeling pull preview applied: successGauge={successGauge:F1}");
        }

        private void AttachHookedFishToFloat()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            Transform target = hookSuccessAttachTarget != null
                ? hookSuccessAttachTarget
                : FindFloatOrPreviewTransform();

            if (target == null)
            {
                target = spawnRoot != null ? spawnRoot : transform;
            }

            StopPhaseMovement();
            Vector3 hookWaterPosition = currentVisualInstance.transform.position;
            hookWaterPosition.y = splashWaterHeight + splashFollowYOffset;
            StopSplashEffect();

            currentPhase = FishPhase.None;
            phaseCompleteCount = 0;
            currentVisualInstance.transform.SetParent(target, false);
            currentVisualInstance.transform.localPosition = target == previewFloatTransform
                ? new Vector3(0f, -0.12f, 0.08f)
                : hookSuccessLocalOffset;
            currentVisualInstance.transform.localRotation = Quaternion.Euler(hookSuccessLocalEuler);
            currentVisualInstance.transform.localScale = GetHookedLocalScale(target);
            hookedBaseLocalPosition = currentVisualInstance.transform.localPosition;
            hookedBaseLocalRotation = currentVisualInstance.transform.localRotation;
            isHookSuccessPreviewActive = true;
            SetVisualRenderersEnabled(true);

            PlayHookSuccessSplash(hookWaterPosition);

            FloatController floatController = target.GetComponent<FloatController>();
            if (floatController != null)
            {
                floatController.ResetFloat();
            }
            else if (target == previewFloatTransform)
            {
                StartPreviewFloatFlyToUser(target);
            }
        }

        public FishCatchData BuildCatchData(BackgroundType siteType, string caughtAt)
        {
            return new FishCatchData
            {
                species = currentSpecies,
                weight = weight,
                caughtAt = caughtAt,
                siteType = siteType
            };
        }
        public void TryStartMiniGame()
        {
            if (miniGameManager == null)
            {
                Debug.LogWarning("[FishController] MiniGameManager 미할당.");
                return;
            }
            if (currentSpecies == null)
            {
                Debug.LogWarning("[FishController] 입질 후 초기화된 물고기 없음.");
                return;
            }
            string caughtAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            FishCatchData data = BuildCatchData(miniGameSiteType, caughtAt);
            miniGameManager.StartMiniGame(data);
        }

        private void SpawnVisual(FishSpeciesDataSO speciesData)
        {
            ClearVisual();
            ClearPreviewFloat();

            Transform parent = spawnRoot != null ? spawnRoot : transform;
            Vector3 spawnPosition = parent.position + spawnOffset;
            visualSpawnPosition = spawnPosition;

            if (speciesData.FishPrefab != null)
            {
                currentVisualInstance = Instantiate(speciesData.FishPrefab, spawnPosition, Quaternion.identity, parent);
                currentVisualInstance.name = $"{speciesData.DisplayName}_Instance";
            }
            else if (createPlaceholderWhenPrefabMissing)
            {
                currentVisualInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                currentVisualInstance.name = $"{speciesData.DisplayName}_Placeholder";
                currentVisualInstance.transform.SetParent(parent);
                currentVisualInstance.transform.position = spawnPosition;
                currentVisualInstance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                currentVisualInstance.transform.localScale = GetPlaceholderScale(sizeCm);

                Renderer renderer = currentVisualInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetPatternColor(pattern);
                }
            }

            if (currentVisualInstance != null)
            {
                if (speciesData.FishPrefab == null)
                {
                    currentVisualInstance.transform.localScale = GetPlaceholderScale(sizeCm);
                }

                visualBaseLocalScale = currentVisualInstance.transform.localScale;
                SetVisualRenderersEnabled(!hideVisualUntilHookSuccess);
                ClampVisualPosition();
                ApplyVisualDirection(GetMovementDirectionByMode(currentMoveMode));
            }
        }

        private void ClearVisual()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            Destroy(currentVisualInstance);
            currentVisualInstance = null;
            visualBaseLocalScale = Vector3.one;
            isHookSuccessPreviewActive = false;
        }

        private void ClearPreviewFloat()
        {
            if (previewFloatFlyCoroutine != null)
            {
                StopCoroutine(previewFloatFlyCoroutine);
                previewFloatFlyCoroutine = null;
            }

            if (previewFloatTransform != null)
            {
                Destroy(previewFloatTransform.gameObject);
                previewFloatTransform = null;
            }

            hasPreviewFloatStartPosition = false;
        }

        private void StartSplashEffect()
        {
            StopSplashEffect();

            if (currentVisualInstance == null)
            {
                return;
            }

            Transform parent = splashRoot != null ? splashRoot : transform;
            currentSplashInstance = splashPrefab != null
                ? Instantiate(splashPrefab, parent)
                : CreateDefaultSplash(parent);

            if (currentSplashInstance == null)
            {
                return;
            }

            currentSplashInstance.name = $"{SpeciesName}_BiteSplash";
            UpdateSplashPosition();
            currentSplashInstance.Play();
        }

        private void StopSplashEffect()
        {
            if (currentSplashInstance == null)
            {
                return;
            }

            Destroy(currentSplashInstance.gameObject);
            currentSplashInstance = null;
        }

        private static Transform FindFloatTransform()
        {
            FloatController floatController = FindObjectOfType<FloatController>();
            return floatController != null ? floatController.transform : null;
        }

        private Transform FindFloatOrPreviewTransform()
        {
            Transform floatTransform = FindFloatTransform();
            return floatTransform != null ? floatTransform : EnsurePreviewFloatTransform();
        }

        private Transform EnsurePreviewFloatTransform()
        {
            if (!createPreviewFloatWhenMissing)
            {
                return null;
            }

            if (previewFloatTransform != null)
            {
                return previewFloatTransform;
            }

            GameObject previewFloat = new("Generated_PreviewFloat");
            previewFloat.name = "Generated_PreviewFloat";
            previewFloat.transform.SetParent(spawnRoot != null ? spawnRoot : transform, false);
            previewFloat.transform.position = GetPreviewFloatStartPosition();
            previewFloat.transform.localScale = Vector3.one;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "PreviewFloatMarker";
            marker.transform.SetParent(previewFloat.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.18f;

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                renderer.material = new Material(shader) { color = new Color(1f, 0.18f, 0.12f) };
            }

            previewFloatTransform = previewFloat.transform;
            return previewFloatTransform;
        }

        private void SetVisualRenderersEnabled(bool isEnabled)
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            foreach (Renderer renderer in currentVisualInstance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = isEnabled;
            }
        }

        private Vector3 GetHookedLocalScale(Transform target)
        {
            Vector3 desiredWorldScale = visualBaseLocalScale * hookedVisualScaleMultiplier;
            if (target == null)
            {
                return desiredWorldScale;
            }

            Vector3 parentScale = target.lossyScale;
            return new Vector3(
                SafeDivide(desiredWorldScale.x, parentScale.x),
                SafeDivide(desiredWorldScale.y, parentScale.y),
                SafeDivide(desiredWorldScale.z, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        private ParticleSystem CreateDefaultSplash(Transform parent)
        {
            if (!createDefaultSplashWhenMissing)
            {
                return null;
            }

            GameObject splashObject = new("Generated_BiteSplash");
            splashObject.transform.SetParent(parent);

            ParticleSystem particleSystem = splashObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = 3.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.48f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 240;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 96f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.84f;
            shape.arc = 360f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.9f, 2.6f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);

            Renderer renderer = particleSystem.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"))
                {
                    color = new Color(0.72f, 0.9f, 1f, 0.72f)
                };
            }

            return particleSystem;
        }

        private void PlayHookSuccessSplash(Vector3 position)
        {
            GameObject effectPrefab = ResolveCatchEffectPrefab();
            if (effectPrefab == null)
            {
                Debug.LogWarning($"[FishController] Catch effect skipped: no NamuFX prefab resolved for {SpeciesName}.");
                return;
            }

            Transform parent = catchEffectRoot != null
                ? catchEffectRoot
                : splashRoot != null
                    ? splashRoot
                    : transform;

            GameObject effectInstance = Instantiate(effectPrefab, position, Quaternion.identity, parent);
            effectInstance.name = $"{SpeciesName}_CatchSuccessEffect";
            effectInstance.transform.localScale *= hookSuccessSplashScale;

            foreach (ParticleSystem particleSystem in effectInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Play(true);
            }

            Destroy(effectInstance, catchEffectLifetime);
            Debug.Log($"[FishController] NamuFX catch effect played: fish={SpeciesName}, prefab={effectPrefab.name}");
        }

        private GameObject ResolveCatchEffectPrefab()
        {
            if (currentSpecies != null && speciesCatchEffects != null)
            {
                foreach (SpeciesCatchEffect effectOverride in speciesCatchEffects)
                {
                    if (effectOverride != null && effectOverride.Matches(currentSpecies))
                    {
                        return effectOverride.effectPrefab;
                    }
                }
            }

            GameObject configuredPrefab = GetConfiguredCatchEffectPrefab();
            if (configuredPrefab != null)
            {
                return configuredPrefab;
            }

#if UNITY_EDITOR
            if (useEditorNamuFxFallback)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(GetEditorNamuFxPath());
            }
#endif

            return null;
        }

        private GameObject GetConfiguredCatchEffectPrefab()
        {
            if (currentSpecies != null && currentSpecies.Rarity >= 4 && rareCatchEffectPrefab != null)
            {
                return rareCatchEffectPrefab;
            }

            return pattern switch
            {
                MovementPattern.Aggressive => aggressiveCatchEffectPrefab,
                MovementPattern.Erratic => erraticCatchEffectPrefab,
                _ => calmCatchEffectPrefab
            };
        }

#if UNITY_EDITOR
        private string GetEditorNamuFxPath()
        {
            const string root = "Assets/Art/Environment/Water/NamuFX/StylizedWaterEffects/Prefabs";

            if (currentSpecies != null)
            {
                switch (currentSpecies.FishId)
                {
                    case "Fish_Crucian":
                        return $"{root}/Water_Impact.prefab";
                    case "Fish_Carp":
                        return $"{root}/Water_Splash_A.prefab";
                    case "Fish_Bass":
                        return $"{root}/Water_Splash_B.prefab";
                    case "Fish_Snakehead":
                        return $"{root}/Water_Explosion.prefab";
                    case "Fish_Catfish":
                        return $"{root}/Water_Ball_Explosion.prefab";
                }
            }

            if (currentSpecies != null && currentSpecies.Rarity >= 4)
            {
                return $"{root}/Water_Ball_Explosion.prefab";
            }

            return pattern switch
            {
                MovementPattern.Aggressive => $"{root}/Water_Splash_B.prefab",
                MovementPattern.Erratic => $"{root}/Water_Explosion.prefab",
                _ => $"{root}/Water_Impact.prefab"
            };
        }
#endif

        private void UpdateSplashPosition()
        {
            if (currentSplashInstance == null || currentVisualInstance == null)
            {
                return;
            }

            Vector3 fishPosition = currentVisualInstance.transform.position;
            currentSplashInstance.transform.position = new Vector3(
                fishPosition.x,
                splashWaterHeight + splashFollowYOffset,
                fishPosition.z);
        }

        private void HandleMiniGamePhaseComplete()
        {
            phaseCompleteCount++;
            TriggerRandomMoveMode();
            Debug.Log($"[FishController] Phase complete count updated: count={phaseCompleteCount}, speed={GetMoveSpeed():F2}");
        }

        private void HandleSuccessGaugeChanged(float successGauge)
        {
            MoveFishTowardFloat(successGauge);
        }

        private void HandleMiniGameEnded(bool success)
        {
            if (success)
            {
                AttachHookedFishToFloat();
                return;
            }

            if (clearFishOnMiniGameFailure)
            {
                ClearFishForScenario("mini game failed");
            }
        }

        private void MoveFishTowardFloat(float successGauge)
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            float progress = Mathf.Clamp01(successGauge / 100f);
            Vector3 currentPosition = currentVisualInstance.transform.position;
            currentPosition.y = visualSpawnPosition.y;
            currentPosition.z = Mathf.Lerp(visualSpawnPosition.z, GetUserNearWaterZ(visualSpawnPosition.z), progress);
            currentVisualInstance.transform.position = currentPosition;
            UpdateSplashPosition();
        }

        private void MoveFloatForReelingPreview(float successGauge)
        {
            if (!moveFloatDuringReelingPreview)
            {
                return;
            }

            Transform floatTransform = FindFloatTransform();
            floatTransform ??= EnsurePreviewFloatTransform();
            if (floatTransform == null) return;

            if (!hasPreviewFloatStartPosition)
            {
                previewFloatStartPosition = floatTransform.position;
                previewFloatStartPosition.y = previewFloatWaterHeight;
                hasPreviewFloatStartPosition = true;
            }

            float progress = Mathf.Clamp01(successGauge / 100f);
            Vector3 floatPosition = previewFloatStartPosition;
            floatPosition.z = Mathf.Lerp(previewFloatStartPosition.z, GetUserNearWaterZ(previewFloatStartPosition.z), progress);
            floatTransform.position = floatPosition;
            UpdateSplashPosition();
        }

        private Vector3 GetPreviewFloatStartPosition()
        {
            Vector3 position = currentVisualInstance != null
                ? currentVisualInstance.transform.position
                : visualSpawnPosition;
            position.y = previewFloatWaterHeight;
            return position;
        }

        private float GetUserNearWaterZ(float startZ)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return startZ - 8f;
            }

            return Mathf.Min(startZ - reelingPullMinDistanceFromFloat, camera.transform.position.z + 3f);
        }

        private void StartPreviewFloatFlyToUser(Transform target)
        {
            if (target == null)
            {
                return;
            }

            if (previewFloatFlyCoroutine != null)
            {
                StopCoroutine(previewFloatFlyCoroutine);
            }

            Camera camera = Camera.main;
            Vector3 destination = camera != null
                ? camera.transform.position + camera.transform.forward * previewFloatAirDistance + Vector3.down * previewFloatAirDrop
                : target.position + new Vector3(0f, 1.2f, -2f);

            previewFloatFlyCoroutine = StartCoroutine(AnimatePreviewFloatToUser(target, destination));
        }

        private IEnumerator AnimatePreviewFloatToUser(Transform target, Vector3 destination)
        {
            Vector3 start = target.position;
            float elapsed = 0f;

            while (target != null && elapsed < previewFloatFlyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, previewFloatFlyDuration));
                t = t * t * (3f - 2f * t);
                target.position = Vector3.Lerp(start, destination, t);
                yield return null;
            }

            if (target != null)
            {
                target.position = destination;
            }

            previewFloatFlyCoroutine = null;
        }

        private void NotifyMiniGameMoveState()
        {
            if (miniGameManager == null)
            {
                return;
            }

            FishMoveState moveState = currentMoveMode switch
            {
                FishMoveMode.MoveLeft => FishMoveState.Left,
                FishMoveMode.MoveRight => FishMoveState.Right,
                _ => FishMoveState.Normal
            };
            miniGameManager.SetFishMoveState(moveState);
        }

        private void SubscribeMiniGameEvents()
        {
            if (miniGameManager == null)
            {
                miniGameManager = FindObjectOfType<MiniGameManager>();
            }

            if (miniGameManager == null || miniGameEventsSubscribed)
            {
                return;
            }

            miniGameManager.OnPhaseComplete += HandleMiniGamePhaseComplete;
            miniGameManager.OnSuccessGaugeChanged += HandleSuccessGaugeChanged;
            miniGameManager.OnMiniGameEnded += HandleMiniGameEnded;
            miniGameEventsSubscribed = true;
        }

        private void UnsubscribeMiniGameEvents()
        {
            if (miniGameManager == null || !miniGameEventsSubscribed)
            {
                return;
            }

            miniGameManager.OnPhaseComplete -= HandleMiniGamePhaseComplete;
            miniGameManager.OnSuccessGaugeChanged -= HandleSuccessGaugeChanged;
            miniGameManager.OnMiniGameEnded -= HandleMiniGameEnded;
            miniGameEventsSubscribed = false;
        }

        private void SubscribeRodEvents()
        {
            if (rodController == null)
            {
                rodController = FindObjectOfType<FishingRodController>();
            }

            if (rodController == null || rodEventsSubscribed)
            {
                return;
            }

            rodController.OnRodStateChanged += HandleRodStateChanged;
            rodController.OnReleased += HandleRodReleased;
            rodEventsSubscribed = true;
        }

        private void UnsubscribeRodEvents()
        {
            if (rodController == null || !rodEventsSubscribed)
            {
                return;
            }

            rodController.OnRodStateChanged -= HandleRodStateChanged;
            rodController.OnReleased -= HandleRodReleased;
            rodEventsSubscribed = false;
        }

        private void HandleRodReleased()
        {
            if (!clearFishWhenRodReleased)
            {
                return;
            }

            ClearFishForScenario("rod released");
        }

        private void HandleRodStateChanged(RodStateTransition transition)
        {
            if (!clearFishWhenRodReturnsToReady)
            {
                return;
            }

            if (isHookSuccessPreviewActive
                && transition.Previous == RodState.MiniGame
                && IsRodReadyState(transition.Current))
            {
                return;
            }

            if (IsRodActiveFishingState(transition.Previous) && IsRodReadyState(transition.Current))
            {
                ClearFishForScenario($"rod returned to {transition.Current}");
            }
        }

        private void ClearFishForScenario(string reason)
        {
            if (currentSpecies == null && currentVisualInstance == null && currentSplashInstance == null)
            {
                return;
            }

            ResetFish();
            Debug.Log($"[FishController] Fish cleared for scenario: {reason}");
        }

        private static bool IsRodReadyState(RodState state)
        {
            return state is RodState.Idle or RodState.Attached;
        }

        private static bool IsRodActiveFishingState(RodState state)
        {
            return state is RodState.Casting or RodState.WaitingForBite or RodState.Hit or RodState.MiniGame;
        }

        private static FishMoveMode GetWeightedRandomMoveMode()
        {
            int roll = UnityEngine.Random.Range(0, 5);
            return roll switch
            {
                0 => FishMoveMode.MoveLeft,
                1 => FishMoveMode.MoveRight,
                _ => FishMoveMode.Stop
            };
        }

        private static Vector3 GetPlaceholderScale(float fishSizeCm)
        {
            float normalizedLength = Mathf.Clamp(fishSizeCm / 40f, 0.4f, 2f);
            return new Vector3(0.35f, normalizedLength, 0.35f);
        }

        private void BeginPhaseMovement()
        {
            currentPhase = FishPhase.Phase1;
            phaseCompleteCount = 0;
            currentMoveMode = GetWeightedRandomMoveMode();
            isPhaseMovementActive = true;
            isWaitingAtMovementLimit = false;
            NotifyMiniGameMoveState();
        }

        private void StopPhaseMovement()
        {
            isPhaseMovementActive = false;
            currentMoveMode = FishMoveMode.Stop;
            isWaitingAtMovementLimit = false;
        }

        private void UpdateHookedFlop()
        {
            if (!isHookSuccessPreviewActive || currentVisualInstance == null)
            {
                return;
            }

            float wave = Mathf.Sin(Time.time * hookedFlopSpeed);
            Vector3 localPosition = hookedBaseLocalPosition;
            localPosition.y += Mathf.Abs(wave) * hookedFlopVerticalAmount;

            currentVisualInstance.transform.localPosition = localPosition;
            currentVisualInstance.transform.localRotation =
                hookedBaseLocalRotation * Quaternion.Euler(0f, 0f, wave * hookedFlopAngle);
        }

        private Vector3 GetMovementDirectionByMode(FishMoveMode moveMode)
        {
            return moveMode switch
            {
                FishMoveMode.MoveLeft => Vector3.left,
                FishMoveMode.MoveRight => Vector3.right,
                _ => Vector3.zero
            };
        }

        private void ApplyVisualDirection(Vector3 movementDirection)
        {
            if (currentVisualInstance == null || movementDirection == Vector3.zero)
            {
                return;
            }

            currentVisualInstance.transform.forward = flipVisualForward
                ? -movementDirection
                : movementDirection;
        }

        private float GetMoveSpeed()
        {
            float patternSpeed = pattern switch
            {
                MovementPattern.Calm => 0.55f,
                MovementPattern.Aggressive => 0.9f,
                MovementPattern.Erratic => 1.2f,
                _ => 1f
            };

            float phaseSpeedMultiplier = Mathf.Max(
                minimumPhaseCompleteSpeedMultiplier,
                1f - phaseCompleteCount * phaseCompleteSlowdownStep);

            return patternSpeed * debugMoveSpeed * phaseSpeedMultiplier * GetDifficultySpeedMultiplier();
        }

        private float CalculateDifficulty()
        {
            float patternDifficulty = pattern switch
            {
                MovementPattern.Calm => 0.85f,
                MovementPattern.Aggressive => 1.15f,
                MovementPattern.Erratic => 1.35f,
                _ => 1f
            };

            return Mathf.Max(0.1f, resistance) * patternDifficulty;
        }

        private float GetDifficultySpeedMultiplier()
        {
            float normalizedDifficulty = Mathf.InverseLerp(0.6f, 3.2f, currentDifficulty);
            return Mathf.Lerp(0.85f, 1.35f, normalizedDifficulty);
        }

        private void ClampVisualPosition()
        {
            if (!clampMovementWithinRange || currentVisualInstance == null)
            {
                return;
            }

            float minX = visualSpawnPosition.x - horizontalMoveLimit;
            float maxX = visualSpawnPosition.x + horizontalMoveLimit;

            Vector3 currentPosition = currentVisualInstance.transform.position;
            bool hitLeftLimit = currentPosition.x <= minX;
            bool hitRightLimit = currentPosition.x >= maxX;

            Vector3 clampedPosition = currentPosition;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);

            currentVisualInstance.transform.position = clampedPosition;

            if (!hitLeftLimit && !hitRightLimit)
            {
                isWaitingAtMovementLimit = false;
                return;
            }

            if (!isWaitingAtMovementLimit)
            {
                isWaitingAtMovementLimit = true;
                Debug.Log($"[FishController] Reached movement limit while staying in {currentPhase}. Waiting for external phase change.");
            }
        }

        private static Color GetPatternColor(MovementPattern movementPattern)
        {
            return movementPattern switch
            {
                MovementPattern.Calm => new Color(0.85f, 0.75f, 0.35f),
                MovementPattern.Aggressive => new Color(0.25f, 0.7f, 0.3f),
                MovementPattern.Erratic => new Color(0.35f, 0.45f, 0.85f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            if (spawnRoot == null)
            {
                spawnRoot = transform;
            }

            horizontalMoveLimit = Mathf.Max(0.1f, horizontalMoveLimit);
            phaseCompleteSlowdownStep = Mathf.Max(0f, phaseCompleteSlowdownStep);
            minimumPhaseCompleteSpeedMultiplier = Mathf.Clamp01(minimumPhaseCompleteSpeedMultiplier);
        }

    }
}
