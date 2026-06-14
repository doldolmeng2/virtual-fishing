using System;
using System.Collections;
using VirtualFishing.Core.Events;
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
    public class FishController : MonoBehaviour, IFish, IVoidEventListener
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

        private readonly struct HookedMouthAnchor
        {
            public readonly Vector3 LocalPoint;
            public readonly Vector3 LocalDirection;
            public readonly string Source;

            public HookedMouthAnchor(Vector3 localPoint, Vector3 localDirection, string source)
            {
                LocalPoint = localPoint;
                LocalDirection = localDirection.sqrMagnitude > 0.0001f
                    ? localDirection.normalized
                    : Vector3.back;
                Source = source;
            }
        }

        private readonly struct HookedMouthPreset
        {
            public readonly Vector3 BoundsOffset;
            public readonly Vector3 LocalDirection;

            public HookedMouthPreset(Vector3 boundsOffset, Vector3 localDirection)
            {
                BoundsOffset = boundsOffset;
                LocalDirection = localDirection.sqrMagnitude > 0.0001f
                    ? localDirection.normalized
                    : Vector3.back;
            }
        }

        private readonly struct RuntimeFishProfile
        {
            public readonly Vector3 VisualScale;
            public readonly float SpeedMultiplier;

            public RuntimeFishProfile(Vector3 visualScale, float speedMultiplier)
            {
                VisualScale = visualScale;
                SpeedMultiplier = speedMultiplier;
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
        [SerializeField, Range(0.25f, 1f)] private float biteSplashScaleMultiplier = 0.7f;

        [Header("Hook Success Preview")]
        [SerializeField] private Transform hookSuccessAttachTarget;
        [SerializeField] private Vector3 hookSuccessLocalEuler = new(90f, 85f, 0f);
        [SerializeField] private bool hideVisualUntilHookSuccess = true;
        [SerializeField] private float reelingPullMinDistanceFromFloat = 0.15f;
        [SerializeField] private PondWaterSurface reelingWaterSurface;
        [SerializeField] private Transform reelingShoreReference;
        [SerializeField] private bool useEnvironmentShorePoint = true;
        [SerializeField] private float reelingShoreInset = 0.35f;
        [SerializeField] private Vector3 fallbackReelingShoreOffset = new(0f, 0f, -8f);
        [SerializeField] private float reelingTriangleBaseHalfWidth = 10f;
        [SerializeField] private bool keepFloatPinnedToFishWhileFishing = true;
        [SerializeField] private bool moveFloatDuringReelingPreview = true;
        [SerializeField] private bool createPreviewFloatWhenMissing = true;
        [SerializeField] private float previewFloatWaterHeight = 0.3f;
        [SerializeField] private float previewFloatAirDistance = 1.8f;
        [SerializeField] private float previewFloatAirDrop = 0.55f;
        [SerializeField] private float previewFloatFlyDuration = 1.2f;
        [SerializeField] private float hookSuccessSplashScale = 1.35f;
        [SerializeField] private float hookedVisualScaleMultiplier = 5.8f;
        [SerializeField] private float hookSuccessMouthTipInset = 0.015f;
        [SerializeField] private float hookSuccessFloatBottomInset = 0f;
        [SerializeField] private float hookedFlopAngle = 18f;
        [SerializeField] private float hookedFlopSpeed = 11f;
        [SerializeField] private float hookedConnectionSlack = 0.08f;
        [SerializeField] private float hookedConnectionSpring = 34f;
        [SerializeField] private float hookedConnectionDamping = 9f;
        [SerializeField] private float hookedConnectionGravity = 0.2f;
        [SerializeField] private float hookedSwingTiltAngle = 10f;
        [SerializeField] private bool logHookSuccessRenderDiagnostics = true;
        [SerializeField] private bool applyHookSuccessVisibleMaterial = true;
        [SerializeField] private bool normalizePrefabVisualToFishSize = true;
        [SerializeField] private float visualSizeToWorldScale = 0.014f;
        [SerializeField] private float minimumPrefabVisualLength = 0.34f;
        [SerializeField] private float maximumPrefabVisualLength = 1.65f;
        [SerializeField] private Vector3 defaultFishLocalBoundsSize = new(1f, 0.35f, 0.25f);
        [SerializeField] private float hookedMinimumVisibleExtent = 0.32f;
        [SerializeField] private float hookedMaximumVisibleExtent = 0.48f;
        [SerializeField] private float hookedFallbackExtentThreshold = 0.000001f;
        [SerializeField] private float hookedMaxScaleBoost = 10000f;

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
        [SerializeField] private VoidEventSO onResultConfirmedEvent;
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
        [SerializeField, Range(0f, 1f)] private float currentReelingProgress;
        [SerializeField] private MiniGameManager miniGameManager;
        [SerializeField] private DifficultySettingsSO difficultySettings;
        // 프로젝트에서 하나의 낚시터만 사용하므로 하나의 낚시터 타입만 사용 (추후 낚시터 추가시 수정 필요)
        [SerializeField] private BackgroundType miniGameSiteType = BackgroundType.Pond;

        private const float HookedVisualPresentationScaleFactor = 0.28f;
        private static readonly Vector3 HookedFishMouthAttachLocalPoint = Vector3.zero;
        private static readonly Vector3 HookedFishMouthLocalDirection = Vector3.back;
#if UNITY_EDITOR
        private const string OnResultConfirmedAssetPath = "Assets/_Project/SO/Events/OnResultConfirmed.asset";
#endif

        private GameObject currentVisualInstance;
        private ParticleSystem currentSplashInstance;
        private Vector3 visualBaseLocalScale = Vector3.one;
        private Transform previewFloatTransform;
        private Vector3 previewFloatStartPosition;
        private bool hasPreviewFloatStartPosition;
        private Coroutine previewFloatFlyCoroutine;
        private bool isHookSuccessPreviewActive;
        private Vector3 hookedMouthLocalPoint;
        private Vector3 hookedMouthDirectionLocal = Vector3.back;
        private Vector3 hookedLooseOffsetWorld;
        private Vector3 hookedLooseVelocityWorld;
        private Transform hookedFloatTarget;
        private Renderer[] hookedFloatRenderers = Array.Empty<Renderer>();
        private PondWaterSurface cachedReelingWaterSurface;
        private Vector3 cachedReelingShorePoint;
        private bool hasCachedReelingShorePoint;
        private bool miniGameEventsSubscribed;
        private bool rodEventsSubscribed;
        private bool resultConfirmEventSubscribed;
        private bool catchResultListenersSubscribed;
        private global::CatchResultController[] catchResultControllers = Array.Empty<global::CatchResultController>();

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
            SubscribeResultConfirmEvents();
        }

        private void Start()
        {
            SubscribeMiniGameEvents();
            SubscribeRodEvents();
            SubscribeResultConfirmEvents();
        }

        private void OnDisable()
        {
            UnsubscribeResultConfirmEvents();
            UnsubscribeMiniGameEvents();
            UnsubscribeRodEvents();
            StopPhaseMovement();
            StopSplashEffect();
        }

        void IVoidEventListener.OnEventRaised()
        {
            HandleResultConfirmed();
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
                SyncFishingFloatToFish();
                UpdateSplashPosition();
                UpdateHookedFlop();
                return;
            }

            float moveSpeed = GetMoveSpeed();
            currentVisualInstance.transform.position += movementDirection * (moveSpeed * Time.deltaTime);
            ClampVisualPosition();
            ApplyVisualDirection(movementDirection);
            UpdateSplashPosition();
            SyncFishingFloatToFish();
            UpdateHookedFlop();
        }

        private void LateUpdate()
        {
            if (isPhaseMovementActive && currentVisualInstance != null && !isHookSuccessPreviewActive)
            {
                SyncFishingFloatToFish();
            }
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
            currentReelingProgress = 0f;
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
            currentReelingProgress = 0f;
            isHookSuccessPreviewActive = false;
            hookedMouthLocalPoint = Vector3.zero;
            hookedMouthDirectionLocal = Vector3.back;
            hookedLooseOffsetWorld = Vector3.zero;
            hookedLooseVelocityWorld = Vector3.zero;
            hookedFloatTarget = null;
            hookedFloatRenderers = Array.Empty<Renderer>();
            cachedReelingWaterSurface = null;
            hasCachedReelingShorePoint = false;
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
                : FindHookSuccessFloatTransform();

            if (target == null)
            {
                target = spawnRoot != null ? spawnRoot : transform;
            }

            StopPhaseMovement();
            Renderer[] targetRenderers = GetHookTargetRenderers(target);
            Vector3 hookWaterPosition = GetHookSuccessEffectPosition();
            PinFloatToFishPosition(target);
            StopSplashEffect();

            currentPhase = FishPhase.None;
            phaseCompleteCount = 0;
            Transform presentationParent = spawnRoot != null ? spawnRoot : transform;
            currentVisualInstance.transform.SetParent(presentationParent, true);
            currentVisualInstance.transform.localScale = GetHookedLocalScale(presentationParent);
            EnsureHookedFishRenderable();
            EnsureHookedFishVisibleSize();

            HookedMouthAnchor mouthAnchor = ResolveHookedMouthAnchor();
            hookedMouthLocalPoint = mouthAnchor.LocalPoint;
            hookedMouthDirectionLocal = mouthAnchor.LocalDirection;
            hookedLooseOffsetWorld = Vector3.zero;
            hookedLooseVelocityWorld = Vector3.zero;
            currentVisualInstance.transform.rotation = GetHookedHangWorldRotation(Vector3.up, 0f);
            AlignHookedFishMouthToWorldPoint(GetHookTargetWorldPoint(target));
            TrackHookedFloatTarget(target, targetRenderers);
            isHookSuccessPreviewActive = true;
            LogHookSuccessRenderDiagnostics(target, "after attach");

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

            // StartMiniGame 이 _currentFishMoveState 를 Normal 로 리셋하므로,
            // 시작 시점에 물고기가 이미 좌/우로 움직이고 있으면 UI 화살표는 떠 있는데
            // 챔질 방향 판정(IsRodInOppositeDirection)은 Normal 로 막혀 낚싯대에 반응하지 않는다.
            // 현재 이동 모드를 즉시 다시 동기화해 첫 사이클부터 일치시킨다.
            NotifyMiniGameMoveState();
        }

        private void SpawnVisual(FishSpeciesDataSO speciesData)
        {
            ClearVisual();
            ClearPreviewFloat();

            Transform parent = spawnRoot != null ? spawnRoot : transform;
            Vector3 spawnPosition = parent.position + spawnOffset;
            visualSpawnPosition = spawnPosition;
            hasCachedReelingShorePoint = TryResolveEnvironmentShorePoint(
                visualSpawnPosition,
                out cachedReelingShorePoint);

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

                ApplySpeciesVisualProfile(speciesData);
                EnsureFishRendererBounds();
                if (speciesData.FishPrefab != null)
                {
                    NormalizePrefabVisualLength();
                }

                visualBaseLocalScale = currentVisualInstance.transform.localScale;
                SetVisualRenderersEnabled(!hideVisualUntilHookSuccess);
                ClampVisualPosition();
                ApplyVisualDirection(GetMovementDirectionByMode(currentMoveMode));
                SyncFishingFloatToFish();
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
            hookedMouthLocalPoint = Vector3.zero;
            hookedMouthDirectionLocal = Vector3.back;
            hookedLooseOffsetWorld = Vector3.zero;
            hookedLooseVelocityWorld = Vector3.zero;
            hookedFloatTarget = null;
            hookedFloatRenderers = Array.Empty<Renderer>();
            cachedReelingWaterSurface = null;
            hasCachedReelingShorePoint = false;
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
            currentSplashInstance.transform.localScale *= biteSplashScaleMultiplier;
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

        private Transform FindHookSuccessFloatTransform()
        {
            if (previewFloatTransform != null)
            {
                return previewFloatTransform;
            }

            return FindFloatOrPreviewTransform();
        }

        private void SyncFishingFloatToFish()
        {
            if (!keepFloatPinnedToFishWhileFishing || isHookSuccessPreviewActive || currentVisualInstance == null)
            {
                return;
            }

            Transform floatTransform = FindFloatTransform();
            floatTransform ??= EnsurePreviewFloatTransform();
            if (floatTransform == null)
            {
                return;
            }

            PinFloatToFishPosition(floatTransform);
        }

        private void PinFloatToFishPosition(Transform floatTransform)
        {
            if (floatTransform == null || currentVisualInstance == null)
            {
                return;
            }

            Vector3 fishPosition = currentVisualInstance.transform.position;
            floatTransform.position = new Vector3(
                fishPosition.x,
                GetFishingFloatWaterHeight(floatTransform),
                fishPosition.z);

            if (floatTransform == previewFloatTransform)
            {
                previewFloatStartPosition = floatTransform.position;
                hasPreviewFloatStartPosition = true;
            }
        }

        private float GetFishingFloatWaterHeight(Transform floatTransform = null)
        {
            if (floatTransform == previewFloatTransform)
            {
                return previewFloatWaterHeight;
            }

            return splashWaterHeight + splashFollowYOffset;
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

        private void EnsureHookedFishRenderable()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            currentVisualInstance.SetActive(true);
            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            bool hasNonFallbackRenderer = HasNonFallbackRenderer(renderers);
            if (hasNonFallbackRenderer)
            {
                RemoveHookedFallbackVisual();
                renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            }

            EnsureFishRendererBounds();

            if (renderers.Length == 0)
            {
                CreateHookedFallbackVisual();
                renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (hasNonFallbackRenderer && IsHookedFallbackRenderer(renderer))
                {
                    continue;
                }

                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.allowOcclusionWhenDynamic = false;

                if (applyHookSuccessVisibleMaterial)
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        renderer.sharedMaterial = CreateHookSuccessVisibleMaterial(GetHookSuccessFishPalette()[0], 0);
                    }
                    else
                    {
                        renderer.sharedMaterials = CreateHookSuccessVisibleMaterials(materials.Length);
                    }
                }
            }
        }

        private void EnsureFishRendererBounds()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsHookedFallbackRenderer(renderer))
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                if (GetMaxExtent(worldBounds) > hookedFallbackExtentThreshold)
                {
                    continue;
                }

                Bounds localBounds = GetFallbackLocalBounds(renderer);
                renderer.localBounds = localBounds;
                Debug.Log(
                    $"[FishController] Renderer local bounds repaired: " +
                    $"fish={currentVisualInstance.name}, renderer={renderer.name}, " +
                    $"localCenter={localBounds.center.ToString("F4")}, localSize={localBounds.size.ToString("F4")}");
            }
        }

        private Bounds GetFallbackLocalBounds(Renderer renderer)
        {
            Bounds localBounds = default;
            bool hasMeshBounds = false;

            if (renderer != null
                && renderer.TryGetComponent(out MeshFilter meshFilter)
                && meshFilter.sharedMesh != null)
            {
                localBounds = meshFilter.sharedMesh.bounds;
                hasMeshBounds = GetMaxExtent(localBounds) > hookedFallbackExtentThreshold;
            }

            if (hasMeshBounds)
            {
                return localBounds;
            }

            return new Bounds(Vector3.zero, defaultFishLocalBoundsSize);
        }

        private void CreateHookedFallbackVisual()
        {
            if (currentVisualInstance.transform.Find("HookedFishFallbackVisual") != null)
            {
                return;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "HookedFishFallbackVisual";
            fallback.transform.SetParent(currentVisualInstance.transform, false);
            fallback.transform.localPosition = Vector3.zero;
            fallback.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fallback.transform.localScale = GetPlaceholderScale(Mathf.Max(sizeCm, 25f));

            if (fallback.TryGetComponent(out Collider collider))
            {
                Destroy(collider);
            }
        }

        private void RemoveHookedFallbackVisual()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            Transform fallback = currentVisualInstance.transform.Find("HookedFishFallbackVisual");
            if (fallback != null)
            {
                fallback.gameObject.SetActive(false);
                Destroy(fallback.gameObject);
            }
        }

        private static bool HasNonFallbackRenderer(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && !IsHookedFallbackRenderer(renderer))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHookedFallbackRenderer(Renderer renderer)
        {
            return renderer != null && renderer.transform.name == "HookedFishFallbackVisual";
        }

        private void EnsureHookedFishVisibleSize()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                CreateHookedFallbackVisual();
                EnsureHookedFishRenderable();
                return;
            }

            if (!TryGetBestCurrentVisualBounds(out Bounds bounds))
            {
                return;
            }

            float maxExtent = Mathf.Max(GetMaxExtent(bounds), hookedFallbackExtentThreshold);
            float minimumVisibleExtent = hookedMinimumVisibleExtent * HookedVisualPresentationScaleFactor;
            if (maxExtent < minimumVisibleExtent)
            {
                float scaleBoost = Mathf.Clamp(
                    minimumVisibleExtent / maxExtent,
                    1f,
                    Mathf.Max(1f, hookedMaxScaleBoost));
                currentVisualInstance.transform.localScale *= scaleBoost;
                maxExtent *= scaleBoost;
            }

            float maximumVisibleExtent = Mathf.Max(minimumVisibleExtent, hookedMaximumVisibleExtent);
            if (maxExtent > maximumVisibleExtent)
            {
                currentVisualInstance.transform.localScale *= maximumVisibleExtent / maxExtent;
            }
        }

        private static float GetMaxExtent(Bounds bounds)
        {
            return Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }

        private Material[] CreateHookSuccessVisibleMaterials(int slotCount)
        {
            Color[] palette = GetHookSuccessFishPalette();
            int count = Mathf.Max(1, slotCount);
            Material[] materials = new Material[count];
            for (int i = 0; i < count; i++)
            {
                Color color = palette[Mathf.Min(i, palette.Length - 1)];
                materials[i] = CreateHookSuccessVisibleMaterial(color, i);
            }

            return materials;
        }

        private Material CreateHookSuccessVisibleMaterial(Color fishColor, int slotIndex)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader)
            {
                name = $"MAT_Runtime_HookedFish_{slotIndex}",
                hideFlags = HideFlags.DontSave,
                color = fishColor
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", fishColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", fishColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.38f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            return material;
        }

        private Color[] GetHookSuccessFishPalette()
        {
            string fishId = currentSpecies != null ? currentSpecies.FishId : string.Empty;
            return fishId switch
            {
                "Fish_Crucian" => new[]
                {
                    new Color(0.34f, 0.35f, 0.20f, 1f),
                    new Color(0.68f, 0.58f, 0.33f, 1f),
                    new Color(0.86f, 0.78f, 0.55f, 1f),
                    new Color(0.50f, 0.38f, 0.18f, 1f)
                },
                "Fish_Carp" => new[]
                {
                    new Color(0.35f, 0.32f, 0.18f, 1f),
                    new Color(0.88f, 0.57f, 0.23f, 1f),
                    new Color(0.95f, 0.79f, 0.50f, 1f),
                    new Color(0.68f, 0.22f, 0.12f, 1f)
                },
                "Fish_Bass" => new[]
                {
                    new Color(0.15f, 0.33f, 0.18f, 1f),
                    new Color(0.38f, 0.56f, 0.28f, 1f),
                    new Color(0.82f, 0.79f, 0.55f, 1f),
                    new Color(0.08f, 0.16f, 0.10f, 1f)
                },
                "Fish_Catfish" => new[]
                {
                    new Color(0.22f, 0.20f, 0.16f, 1f),
                    new Color(0.48f, 0.43f, 0.34f, 1f),
                    new Color(0.76f, 0.68f, 0.53f, 1f),
                    new Color(0.28f, 0.25f, 0.22f, 1f)
                },
                "Fish_Snakehead" => new[]
                {
                    new Color(0.10f, 0.18f, 0.10f, 1f),
                    new Color(0.30f, 0.42f, 0.22f, 1f),
                    new Color(0.66f, 0.63f, 0.43f, 1f),
                    new Color(0.05f, 0.08f, 0.05f, 1f)
                },
                _ => new[]
                {
                    Color.Lerp(GetPatternColor(pattern), Color.black, 0.35f),
                    GetPatternColor(pattern),
                    Color.Lerp(GetPatternColor(pattern), Color.white, 0.45f),
                    Color.Lerp(GetPatternColor(pattern), Color.black, 0.2f)
                }
            };
        }

        private static Renderer[] GetHookTargetRenderers(Transform target)
        {
            return target != null
                ? target.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
        }

        private void NormalizePrefabVisualLength()
        {
            if (!normalizePrefabVisualToFishSize || currentVisualInstance == null)
            {
                return;
            }

            if (!TryGetBestCurrentVisualBounds(out Bounds bounds))
            {
                return;
            }

            float currentLength = Mathf.Max(GetMaxExtent(bounds) * 2f, hookedFallbackExtentThreshold);
            float desiredLength = Mathf.Clamp(
                sizeCm * visualSizeToWorldScale,
                minimumPrefabVisualLength,
                maximumPrefabVisualLength);
            float scaleBoost = Mathf.Clamp(
                desiredLength / currentLength,
                0.001f,
                Mathf.Max(1f, hookedMaxScaleBoost));

            currentVisualInstance.transform.localScale *= scaleBoost;
        }

        private void AlignHookedFishMouthToFloat(Transform target)
        {
            if (target == null || currentVisualInstance == null)
            {
                return;
            }

            AlignHookedFishMouthToWorldPoint(GetHookTargetWorldPoint(target));
        }

        private void AlignHookedFishMouthToWorldPoint(Vector3 mouthWorldPoint)
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            Vector3 currentMouthPoint = currentVisualInstance.transform.TransformPoint(hookedMouthLocalPoint);
            currentVisualInstance.transform.position += mouthWorldPoint - currentMouthPoint;
        }

        private Quaternion GetHookedHangWorldRotation(Vector3 hangDirection, float flopAngle)
        {
            Vector3 headDirection = hangDirection.sqrMagnitude > 0.0001f
                ? hangDirection.normalized
                : Vector3.up;
            Quaternion hangRotation = Quaternion.FromToRotation(hookedMouthDirectionLocal, headDirection);
            Quaternion sideViewTwist = Quaternion.AngleAxis(-hookSuccessLocalEuler.y, headDirection);
            Quaternion flopTwist = Quaternion.AngleAxis(flopAngle, headDirection);
            return flopTwist * sideViewTwist * hangRotation;
        }

        private HookedMouthAnchor ResolveHookedMouthAnchor()
        {
            if (TryResolveHardcodedMouthAnchor(out HookedMouthAnchor hardcodedAnchor))
            {
                Debug.Log(
                    $"[FishController] Hook mouth anchor resolved: fish={SpeciesName}, " +
                    $"source={hardcodedAnchor.Source}, localPoint={hardcodedAnchor.LocalPoint.ToString("F4")}, " +
                    $"localDirection={hardcodedAnchor.LocalDirection.ToString("F4")}");
                return hardcodedAnchor;
            }

            if (TryGetCurrentVisualLocalBounds(out Bounds localBounds))
            {
                Vector3 fallbackDirection = HookedFishMouthLocalDirection;
                return new HookedMouthAnchor(
                    GetMouthLocalPoint(localBounds, fallbackDirection),
                    fallbackDirection,
                    "bounds fallback");
            }

            return new HookedMouthAnchor(Vector3.zero, GetHookSuccessMouthDirection(), "origin fallback");
        }

        private bool TryResolveHardcodedMouthAnchor(out HookedMouthAnchor anchor)
        {
            anchor = default;
            if (!TryGetCurrentVisualLocalBounds(out Bounds localBounds))
            {
                return false;
            }

            HookedMouthPreset preset = GetHardcodedMouthPreset();
            anchor = new HookedMouthAnchor(
                GetMouthLocalPoint(localBounds, preset.BoundsOffset, preset.LocalDirection),
                preset.LocalDirection,
                $"hardcoded {GetCurrentFishPrefabName()}");
            return true;
        }

        private HookedMouthPreset GetHardcodedMouthPreset()
        {
            // Floreswa fish prefabs have no mouth anchor object, so the mouth endpoint is pinned here.
            // BoundsOffset is normalized by local renderer bounds: -1/1 means each local bounds edge.
            string fishId = currentSpecies != null ? currentSpecies.FishId : string.Empty;
            if (!string.IsNullOrEmpty(fishId))
            {
                return fishId switch
                {
                    "Fish_Crucian" => new HookedMouthPreset(new Vector3(0f, -0.34f, -0.9f), Vector3.back),
                    "Fish_Carp" => new HookedMouthPreset(new Vector3(0f, -0.34f, -0.9f), Vector3.back),
                    "Fish_Bass" => new HookedMouthPreset(new Vector3(0f, 0f, -1f), Vector3.back),
                    "Fish_Catfish" => new HookedMouthPreset(new Vector3(0f, -0.36f, -0.95f), Vector3.back),
                    "Fish_Snakehead" => new HookedMouthPreset(new Vector3(0f, -0.36f, -0.95f), Vector3.back),
                    _ => GetPrefabMouthPreset()
                };
            }

            return GetPrefabMouthPreset();
        }

        private HookedMouthPreset GetPrefabMouthPreset()
        {
            return GetCurrentFishPrefabName() switch
            {
                "fish01" => new HookedMouthPreset(new Vector3(0f, -0.34f, -0.9f), Vector3.back),
                "fish01_shade" => new HookedMouthPreset(new Vector3(0f, -0.34f, -0.9f), Vector3.back),
                "fish02" or "fish02_shade" => new HookedMouthPreset(new Vector3(0f, 0f, -1f), Vector3.back),
                "fish03" => new HookedMouthPreset(new Vector3(0f, -0.36f, -0.95f), Vector3.back),
                "fish03_shade" => new HookedMouthPreset(new Vector3(0f, -0.36f, -0.95f), Vector3.back),
                _ => new HookedMouthPreset(HookedFishMouthLocalDirection, HookedFishMouthLocalDirection)
            };
        }

        private string GetCurrentFishPrefabName()
        {
            if (currentSpecies != null && currentSpecies.FishPrefab != null)
            {
                return currentSpecies.FishPrefab.name;
            }

            return currentVisualInstance != null
                ? currentVisualInstance.name.Replace("_Instance", string.Empty)
                : string.Empty;
        }

        private Vector3 GetMouthLocalPoint(Bounds localBounds, Vector3 direction)
        {
            return GetMouthLocalPoint(localBounds, direction, direction);
        }

        private Vector3 GetMouthLocalPoint(Bounds localBounds, Vector3 boundsOffset, Vector3 direction)
        {
            Vector3 clampedOffset = new(
                Mathf.Clamp(boundsOffset.x, -1f, 1f),
                Mathf.Clamp(boundsOffset.y, -1f, 1f),
                Mathf.Clamp(boundsOffset.z, -1f, 1f));
            Vector3 mouthPoint = localBounds.center + Vector3.Scale(localBounds.extents, clampedOffset);
            return mouthPoint - direction * Mathf.Max(0f, hookSuccessMouthTipInset);
        }

        private Vector3 GetHookSuccessMouthDirection()
        {
            return HookedFishMouthLocalDirection;
        }

        private bool TryGetCurrentVisualLocalBounds(out Bounds bounds)
        {
            bounds = default;
            if (currentVisualInstance == null)
            {
                return false;
            }

            Matrix4x4 worldToFishLocal = currentVisualInstance.transform.worldToLocalMatrix;
            MeshFilter[] meshFilters = currentVisualInstance.GetComponentsInChildren<MeshFilter>(true);
            bool hasBounds = false;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter == null
                    || meshFilter.sharedMesh == null
                    || meshFilter.transform.name == "HookedFishFallbackVisual")
                {
                    continue;
                }

                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                if (GetMaxExtent(meshBounds) <= hookedFallbackExtentThreshold)
                {
                    continue;
                }

                Matrix4x4 matrix = worldToFishLocal * meshFilter.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(ref bounds, ref hasBounds, matrix, meshBounds);
            }

            if (hasBounds)
            {
                return true;
            }

            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsHookedFallbackRenderer(renderer))
                {
                    continue;
                }

                Bounds localBounds = renderer.localBounds;
                if (GetMaxExtent(localBounds) <= hookedFallbackExtentThreshold)
                {
                    continue;
                }

                Matrix4x4 matrix = worldToFishLocal * renderer.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(ref bounds, ref hasBounds, matrix, localBounds);
            }

            return hasBounds;
        }

        private bool TryGetBestCurrentVisualBounds(out Bounds bounds)
        {
            if (TryGetCurrentVisualBounds(out bounds) && GetMaxExtent(bounds) > hookedFallbackExtentThreshold)
            {
                return true;
            }

            if (TryGetCurrentMeshWorldBounds(out bounds) && GetMaxExtent(bounds) > hookedFallbackExtentThreshold)
            {
                return true;
            }

            return TryGetCurrentRendererLocalWorldBounds(out bounds);
        }

        private bool TryGetCurrentVisualBounds(out Bounds bounds)
        {
            bounds = default;
            if (currentVisualInstance == null)
            {
                return false;
            }

            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsHookedFallbackRenderer(renderer))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private bool TryGetCurrentRendererLocalWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (currentVisualInstance == null)
            {
                return false;
            }

            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsHookedFallbackRenderer(renderer))
                {
                    continue;
                }

                Bounds localBounds = renderer.localBounds;
                if (GetMaxExtent(localBounds) <= hookedFallbackExtentThreshold)
                {
                    continue;
                }

                EncapsulateTransformedBounds(ref bounds, ref hasBounds, renderer.transform.localToWorldMatrix, localBounds);
            }

            return hasBounds;
        }

        private bool TryGetCurrentMeshWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (currentVisualInstance == null)
            {
                return false;
            }

            MeshFilter[] meshFilters = currentVisualInstance.GetComponentsInChildren<MeshFilter>(true);
            bool hasBounds = false;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                if (meshFilter.transform.name == "HookedFishFallbackVisual")
                {
                    continue;
                }

                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                if (GetMaxExtent(meshBounds) <= 0f)
                {
                    continue;
                }

                Matrix4x4 matrix = meshFilter.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(ref bounds, ref hasBounds, matrix, meshBounds);
            }

            return hasBounds;
        }

        private static void EncapsulateTransformedBounds(
            ref Bounds bounds,
            ref bool hasBounds,
            Matrix4x4 matrix,
            Bounds sourceBounds)
        {
            Vector3 center = sourceBounds.center;
            Vector3 extents = sourceBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z));
                        Vector3 worldCorner = matrix.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(worldCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(worldCorner);
                        }
                    }
                }
            }
        }

        private void LogHookSuccessRenderDiagnostics(Transform target, string phase)
        {
            if (!logHookSuccessRenderDiagnostics || currentVisualInstance == null)
            {
                return;
            }

            Renderer[] renderers = currentVisualInstance.GetComponentsInChildren<Renderer>(true);
            int enabledCount = 0;
            int activeCount = 0;
            int materialCount = 0;
            int missingMaterialCount = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.enabled)
                {
                    enabledCount++;
                }

                if (renderer.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }

                Material[] materials = renderer.sharedMaterials;
                materialCount += materials?.Length ?? 0;
                if (materials == null || materials.Length == 0)
                {
                    missingMaterialCount++;
                    continue;
                }

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null || material.shader == null)
                    {
                        missingMaterialCount++;
                    }
                }
            }

            string boundsText = TryGetBestCurrentVisualBounds(out Bounds bounds)
                ? $"boundsCenter={bounds.center.ToString("F4")}, boundsExtents={bounds.extents.ToString("F4")}"
                : "bounds=missing";
            string meshText = BuildMeshDiagnosticsText();

            Debug.Log(
                $"[FishController] Hook success render diagnostics ({phase}): " +
                $"target={(target != null ? target.name : "null")}, " +
                $"fish={currentVisualInstance.name}, renderers={renderers.Length}, " +
                $"enabled={enabledCount}, active={activeCount}, materials={materialCount}, " +
                $"missingMaterials={missingMaterialCount}, " +
                $"localScale={currentVisualInstance.transform.localScale.ToString("F4")}, " +
                $"lossyScale={currentVisualInstance.transform.lossyScale.ToString("F4")}, " +
                $"{boundsText}, {meshText}");
        }

        private string BuildMeshDiagnosticsText()
        {
            if (currentVisualInstance == null)
            {
                return "meshes=0";
            }

            MeshFilter[] meshFilters = currentVisualInstance.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                if (meshFilter.transform.name == "HookedFishFallbackVisual")
                {
                    continue;
                }

                Mesh mesh = meshFilter.sharedMesh;
                Renderer renderer = meshFilter.GetComponent<Renderer>();
                string rendererLocalBounds = renderer != null
                    ? renderer.localBounds.size.ToString("F4")
                    : "none";

                return
                    $"meshes={meshFilters.Length}, mesh={mesh.name}, " +
                    $"vertices={mesh.vertexCount}, subMeshes={mesh.subMeshCount}, " +
                    $"meshBoundsCenter={mesh.bounds.center.ToString("F4")}, " +
                    $"meshBoundsExtents={mesh.bounds.extents.ToString("F4")}, " +
                    $"rendererLocalBoundsSize={rendererLocalBounds}";
            }

            return $"meshes={meshFilters.Length}, mesh=missing";
        }

        private void TrackHookedFloatTarget(Transform target, Renderer[] targetRenderers)
        {
            hookedFloatTarget = target;
            hookedFloatRenderers = targetRenderers ?? Array.Empty<Renderer>();
        }

        private bool IsHookedFloatStillVisible()
        {
            if (hookedFloatTarget == null || !hookedFloatTarget.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (hookedFloatRenderers == null || hookedFloatRenderers.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < hookedFloatRenderers.Length; i++)
            {
                Renderer renderer = hookedFloatRenderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetHookTargetWorldPoint(Transform target)
        {
            if (target == null)
            {
                return currentVisualInstance != null
                    ? currentVisualInstance.transform.position
                    : transform.position;
            }

            if (hookedFloatRenderers != null && hookedFloatRenderers.Length > 0)
            {
                Bounds bounds = default;
                bool hasBounds = false;
                for (int i = 0; i < hookedFloatRenderers.Length; i++)
                {
                    Renderer renderer = hookedFloatRenderers[i];
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (hasBounds)
                {
                    return bounds.center
                        + Vector3.down * (bounds.extents.y + Mathf.Max(0f, hookSuccessFloatBottomInset));
                }
            }

            return target.TransformPoint(HookedFishMouthAttachLocalPoint);
        }

        private Vector3 GetHookedLocalScale(Transform target)
        {
            Vector3 desiredWorldScale = visualBaseLocalScale
                * hookedVisualScaleMultiplier
                * HookedVisualPresentationScaleFactor;
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
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.45f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 62f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.58f;
            shape.arc = 360f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.65f, 1.85f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);

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

        private Vector3 GetHookSuccessEffectPosition()
        {
            if (currentSplashInstance != null)
            {
                return currentSplashInstance.transform.position;
            }

            Vector3 position = currentVisualInstance != null
                ? currentVisualInstance.transform.position
                : visualSpawnPosition;
            position.y = splashWaterHeight + splashFollowYOffset;
            return position;
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

            float progress = SetReelingProgress(successGauge);
            Vector3 currentPosition = currentVisualInstance.transform.position;
            currentPosition = ClampPositionToReelingTriangle(currentPosition, visualSpawnPosition, progress);
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

            SetReelingProgress(successGauge);
            PinFloatToFishPosition(floatTransform);
            UpdateSplashPosition();
        }

        private float SetReelingProgress(float successGauge)
        {
            float gaugeProgress = Mathf.Clamp01(successGauge / 100f);
            currentReelingProgress = difficultySettings != null
                ? difficultySettings.GetReelingProgress(gaugeProgress)
                : gaugeProgress;
            return currentReelingProgress;
        }

        private Vector3 GetPreviewFloatStartPosition()
        {
            Vector3 position = currentVisualInstance != null
                ? currentVisualInstance.transform.position
                : visualSpawnPosition;
            position.y = previewFloatWaterHeight;
            return position;
        }

        private Vector3 ClampPositionToReelingTriangle(Vector3 currentPosition, Vector3 startPosition, float progress)
        {
            Vector3 shorePoint = GetReelingShorePoint(startPosition);
            Vector2 start2D = new(startPosition.x, startPosition.z);
            Vector2 shore2D = new(shorePoint.x, shorePoint.z);
            Vector2 toShore = shore2D - start2D;
            float distanceToShore = toShore.magnitude;
            if (distanceToShore < 0.001f)
            {
                return ClampPointInsideReelingWater(currentPosition, startPosition);
            }

            Vector2 forward = toShore / distanceToShore;
            Vector2 right = new(-forward.y, forward.x);
            Vector2 current2D = new(currentPosition.x, currentPosition.z);
            Vector2 center = start2D + forward * (distanceToShore * progress);
            float halfWidth = Mathf.Lerp(reelingTriangleBaseHalfWidth, 0f, progress);
            float lateral = Mathf.Clamp(Vector2.Dot(current2D - center, right), -halfWidth, halfWidth);
            Vector2 clamped2D = center + right * lateral;

            Vector3 clampedPosition = new(clamped2D.x, startPosition.y, clamped2D.y);
            return ClampPointInsideReelingWater(clampedPosition, startPosition);
        }

        private Vector3 GetReelingShorePoint(Vector3 startPosition)
        {
            if (hasCachedReelingShorePoint && IsCachedReelingShorePointValid())
            {
                cachedReelingShorePoint.y = startPosition.y;
                return cachedReelingShorePoint;
            }

            hasCachedReelingShorePoint = false;
            if (TryResolveEnvironmentShorePoint(startPosition, out cachedReelingShorePoint))
            {
                hasCachedReelingShorePoint = true;
                cachedReelingShorePoint.y = startPosition.y;
                return cachedReelingShorePoint;
            }

            return GetFallbackReelingShorePoint(startPosition);
        }

        private bool IsCachedReelingShorePointValid()
        {
            return !useEnvironmentShorePoint
                || (cachedReelingWaterSurface != null && cachedReelingWaterSurface.isActiveAndEnabled);
        }

        private bool TryResolveEnvironmentShorePoint(Vector3 startPosition, out Vector3 shorePoint)
        {
            shorePoint = default;
            if (useEnvironmentShorePoint
                && TryGetReelingWaterSurface(startPosition, out PondWaterSurface waterSurface)
                && waterSurface.TryGetClosestInsetShorePoint(
                    GetShoreReferencePosition(startPosition),
                    startPosition,
                    reelingShoreInset,
                    out shorePoint))
            {
                shorePoint = EnsureMinimumReelingDistance(startPosition, shorePoint, waterSurface);
                shorePoint.y = startPosition.y;
                return true;
            }

            return false;
        }

        private bool TryGetReelingWaterSurface(Vector3 startPosition, out PondWaterSurface waterSurface)
        {
            if (reelingWaterSurface != null && reelingWaterSurface.isActiveAndEnabled)
            {
                waterSurface = reelingWaterSurface;
                cachedReelingWaterSurface = waterSurface;
                return true;
            }

            if (cachedReelingWaterSurface != null && cachedReelingWaterSurface.isActiveAndEnabled)
            {
                waterSurface = cachedReelingWaterSurface;
                return true;
            }

            PondWaterSurface[] waterSurfaces = FindObjectsOfType<PondWaterSurface>();
            float bestDistance = float.PositiveInfinity;
            waterSurface = null;

            foreach (PondWaterSurface candidate in waterSurfaces)
            {
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                float distance = 0f;
                if (!candidate.ContainsWorldPoint(startPosition))
                {
                    distance = GetWaterBoundsDistance(candidate, startPosition);
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    waterSurface = candidate;
                }
            }

            cachedReelingWaterSurface = waterSurface;
            return waterSurface != null;
        }

        private static float GetWaterBoundsDistance(PondWaterSurface waterSurface, Vector3 position)
        {
            if (waterSurface == null)
            {
                return float.PositiveInfinity;
            }

            if (TryGetWaterBounds(waterSurface, out Bounds bounds))
            {
                Vector3 closest = bounds.ClosestPoint(position);
                return (closest - position).sqrMagnitude;
            }

            return float.PositiveInfinity;
        }

        private static bool TryGetWaterBounds(PondWaterSurface waterSurface, out Bounds bounds)
        {
            bounds = default;
            if (waterSurface == null)
            {
                return false;
            }

            Collider waterCollider = waterSurface.GetComponent<Collider>();
            if (waterCollider != null)
            {
                bounds = waterCollider.bounds;
                return true;
            }

            if (waterSurface.TryGetComponent(out Renderer renderer))
            {
                bounds = renderer.bounds;
                return true;
            }

            return false;
        }

        private Vector3 EnsureMinimumReelingDistance(
            Vector3 startPosition,
            Vector3 shorePoint,
            PondWaterSurface waterSurface)
        {
            if ((shorePoint - startPosition).sqrMagnitude <= reelingPullMinDistanceFromFloat * reelingPullMinDistanceFromFloat)
            {
                Vector3 direction = shorePoint - startPosition;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = GetFallbackReelingDirection();
                }

                shorePoint = startPosition + direction.normalized * reelingPullMinDistanceFromFloat;
                if (waterSurface != null)
                {
                    shorePoint = waterSurface.ClampWorldPointInside(shorePoint, reelingShoreInset);
                }
            }

            shorePoint.y = startPosition.y;
            return shorePoint;
        }

        private Vector3 ClampPointInsideReelingWater(Vector3 position, Vector3 startPosition)
        {
            if (TryGetReelingWaterSurface(startPosition, out PondWaterSurface waterSurface))
            {
                Vector3 clamped = waterSurface.ClampWorldPointInside(position, reelingShoreInset);
                clamped.y = startPosition.y;
                return clamped;
            }

            return position;
        }

        private Vector3 GetFallbackReelingShorePoint(Vector3 startPosition)
        {
            Vector3 shorePoint = startPosition + GetFallbackReelingDirection() * Mathf.Max(
                reelingPullMinDistanceFromFloat,
                fallbackReelingShoreOffset.magnitude);
            shorePoint.y = startPosition.y;
            return shorePoint;
        }

        private Vector3 GetFallbackReelingDirection()
        {
            Vector3 direction = fallbackReelingShoreOffset;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.back;
        }

        private Vector3 GetShoreReferencePosition(Vector3 startPosition)
        {
            if (reelingShoreReference != null)
            {
                return reelingShoreReference.position;
            }

            if (rodController != null)
            {
                return rodController.transform.position;
            }

            return spawnRoot != null
                ? spawnRoot.position
                : startPosition + fallbackReelingShoreOffset;
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

        private void SubscribeResultConfirmEvents()
        {
            TryResolveResultConfirmedEvent();

            if (onResultConfirmedEvent != null && !resultConfirmEventSubscribed)
            {
                onResultConfirmedEvent.Register(this);
                resultConfirmEventSubscribed = true;
            }

            if (catchResultListenersSubscribed)
            {
                return;
            }

            catchResultControllers = FindObjectsByType<global::CatchResultController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < catchResultControllers.Length; i++)
            {
                global::CatchResultController controller = catchResultControllers[i];
                if (controller != null && controller.onConfirmEvent != null)
                {
                    controller.onConfirmEvent.RemoveListener(HandleResultConfirmed);
                    controller.onConfirmEvent.AddListener(HandleResultConfirmed);
                }
            }

            catchResultListenersSubscribed = true;
        }

        private void UnsubscribeResultConfirmEvents()
        {
            if (onResultConfirmedEvent != null && resultConfirmEventSubscribed)
            {
                onResultConfirmedEvent.Unregister(this);
                resultConfirmEventSubscribed = false;
            }

            if (!catchResultListenersSubscribed)
            {
                return;
            }

            for (int i = 0; i < catchResultControllers.Length; i++)
            {
                global::CatchResultController controller = catchResultControllers[i];
                if (controller != null && controller.onConfirmEvent != null)
                {
                    controller.onConfirmEvent.RemoveListener(HandleResultConfirmed);
                }
            }

            catchResultControllers = Array.Empty<global::CatchResultController>();
            catchResultListenersSubscribed = false;
        }

        private void TryResolveResultConfirmedEvent()
        {
            if (onResultConfirmedEvent != null)
            {
                return;
            }

#if UNITY_EDITOR
            onResultConfirmedEvent = AssetDatabase.LoadAssetAtPath<VoidEventSO>(OnResultConfirmedAssetPath);
#endif
        }

        private void HandleRodReleased()
        {
            if (isHookSuccessPreviewActive)
            {
                return;
            }

            if (!clearFishWhenRodReleased)
            {
                return;
            }

            ClearFishForScenario("rod released");
        }

        private void HandleResultConfirmed()
        {
            ClearFishForScenario("result confirmed");
        }

        private void HandleRodStateChanged(RodStateTransition transition)
        {
            if (isHookSuccessPreviewActive
                && IsRodReadyState(transition.Previous)
                && IsRodActiveFishingState(transition.Current))
            {
                ClearFishForScenario($"new fishing started: {transition.Current}");
                return;
            }

            if (!clearFishWhenRodReturnsToReady)
            {
                return;
            }

            if (transition.Previous == RodState.MiniGame && IsRodReadyState(transition.Current))
            {
                // MiniGameManager raises the generic result event before its C# success callback.
                // The rod may reel in first, so keep the fish visual alive until HandleMiniGameEnded decides success/failure.
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

            if (!IsHookedFloatStillVisible())
            {
                ClearFishForScenario("hooked float hidden");
                return;
            }

            float deltaTime = Time.deltaTime;
            float wave = Mathf.Sin(Time.time * hookedFlopSpeed);
            UpdateHookedLooseConnection(deltaTime);

            Vector3 swingVector = Vector3.ProjectOnPlane(
                hookedLooseOffsetWorld + hookedLooseVelocityWorld * 0.035f,
                Vector3.up);
            Vector3 hangDirection = Vector3.up;
            if (swingVector.sqrMagnitude > 0.000001f)
            {
                Vector3 swingAxis = Vector3.Cross(Vector3.up, swingVector.normalized);
                float slack = Mathf.Max(0.001f, hookedConnectionSlack);
                float swingAngle = Mathf.Clamp(
                    swingVector.magnitude / slack * hookedSwingTiltAngle,
                    0f,
                    hookedSwingTiltAngle);
                hangDirection = Quaternion.AngleAxis(swingAngle, swingAxis) * Vector3.up;
            }

            currentVisualInstance.transform.rotation = GetHookedHangWorldRotation(
                hangDirection,
                wave * hookedFlopAngle);

            AlignHookedFishMouthToWorldPoint(GetHookTargetWorldPoint(hookedFloatTarget));
        }

        private void UpdateHookedLooseConnection(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            hookedLooseVelocityWorld += Vector3.down * (hookedConnectionGravity * deltaTime);
            hookedLooseVelocityWorld += -hookedLooseOffsetWorld * (hookedConnectionSpring * deltaTime);
            hookedLooseVelocityWorld *= Mathf.Exp(-hookedConnectionDamping * deltaTime);
            hookedLooseOffsetWorld += hookedLooseVelocityWorld * deltaTime;

            float slack = Mathf.Max(0f, hookedConnectionSlack);
            float distance = hookedLooseOffsetWorld.magnitude;
            if (slack <= 0f || distance <= slack)
            {
                return;
            }

            Vector3 normal = hookedLooseOffsetWorld / distance;
            hookedLooseOffsetWorld = normal * slack;
            float outwardVelocity = Vector3.Dot(hookedLooseVelocityWorld, normal);
            if (outwardVelocity > 0f)
            {
                hookedLooseVelocityWorld -= normal * outwardVelocity;
            }
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

            return patternSpeed
                * debugMoveSpeed
                * phaseSpeedMultiplier
                * GetDifficultySpeedMultiplier()
                * GetRuntimeProfile(currentSpecies).SpeedMultiplier;
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

        private void ApplySpeciesVisualProfile(FishSpeciesDataSO speciesData)
        {
            RuntimeFishProfile profile = GetRuntimeProfile(speciesData);
            currentVisualInstance.transform.localScale = Vector3.Scale(
                currentVisualInstance.transform.localScale,
                profile.VisualScale);
        }

        private static RuntimeFishProfile GetRuntimeProfile(FishSpeciesDataSO speciesData)
        {
            string fishId = speciesData != null ? speciesData.FishId : string.Empty;
            return fishId switch
            {
                "Fish_Crucian" => new RuntimeFishProfile(new Vector3(0.95f, 1.08f, 0.92f), 0.82f),
                "Fish_Carp" => new RuntimeFishProfile(new Vector3(1.18f, 1.12f, 1.08f), 0.74f),
                "Fish_Bass" => new RuntimeFishProfile(new Vector3(1.02f, 0.92f, 1.22f), 1.15f),
                "Fish_Catfish" => new RuntimeFishProfile(new Vector3(1.12f, 0.86f, 1.38f), 0.68f),
                "Fish_Snakehead" => new RuntimeFishProfile(new Vector3(1.04f, 0.82f, 1.55f), 1.32f),
                _ => new RuntimeFishProfile(Vector3.one, 1f)
            };
        }

        private void ClampVisualPosition()
        {
            if (!clampMovementWithinRange || currentVisualInstance == null)
            {
                return;
            }

            Vector3 currentPosition = currentVisualInstance.transform.position;
            Vector3 clampedPosition = ClampPositionToReelingTriangle(
                currentPosition,
                visualSpawnPosition,
                currentReelingProgress);
            currentVisualInstance.transform.position = clampedPosition;

            bool wasClamped = (clampedPosition - currentPosition).sqrMagnitude > 0.0001f;
            if (!wasClamped)
            {
                isWaitingAtMovementLimit = false;
                return;
            }

            if (!isWaitingAtMovementLimit)
            {
                isWaitingAtMovementLimit = true;
                Debug.Log("[FishController] Reeling triangle boundary reached. Movement is constrained along the active fishing area.");
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
            reelingPullMinDistanceFromFloat = Mathf.Max(0f, reelingPullMinDistanceFromFloat);
            reelingShoreInset = Mathf.Max(0f, reelingShoreInset);
            reelingTriangleBaseHalfWidth = Mathf.Max(0.1f, reelingTriangleBaseHalfWidth);
            phaseCompleteSlowdownStep = Mathf.Max(0f, phaseCompleteSlowdownStep);
            minimumPhaseCompleteSpeedMultiplier = Mathf.Clamp01(minimumPhaseCompleteSpeedMultiplier);
        }

    }
}
