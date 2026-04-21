using System;
using UnityEngine;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Core.Fish
{
    public class FishController : MonoBehaviour, IFish
    {
        [Header("Visual Test Setup")]
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Vector3 spawnOffset = new(0f, 0.5f, 2.5f);
        [SerializeField] private bool createPlaceholderWhenPrefabMissing = true;
        [SerializeField] private bool clampMovementWithinRange = true;
        [SerializeField] private float horizontalMoveLimit = 10.0f;

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
        [SerializeField] private Vector3 visualSpawnPosition;
        [SerializeField] private bool isWaitingAtMovementLimit;
        [SerializeField] private FishPhase inspectorDebugPhase = FishPhase.Phase2;

        private GameObject currentVisualInstance;

        public FishSpeciesDataSO CurrentSpecies => currentSpecies;
        public string SpeciesName => currentSpecies != null ? currentSpecies.DisplayName : string.Empty;
        public float Weight => weight;
        public float Resistance => resistance;
        public MovementPattern Pattern => pattern;
        public float SizeCm => sizeCm;
        public FishPhase CurrentPhase => currentPhase;
        public FishMoveMode CurrentMoveMode => currentMoveMode;
        public FishPhase InspectorDebugPhase
        {
            get => inspectorDebugPhase;
            set => inspectorDebugPhase = value;
        }

        public event Action<Vector3> OnFishMoved;

        private void Update()
        {
            if (!isPhaseMovementActive || currentVisualInstance == null)
            {
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
            currentVisualInstance.transform.forward = movementDirection;
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
            SpawnVisual(speciesData);
            BeginPhaseMovement();

            Debug.Log(
                $"[FishController] Initialized fish: id={speciesData.FishId}, name={speciesData.DisplayName}, " +
                $"weight={weight:F2}kg, size={sizeCm:F1}cm, resistance={resistance:F2}, pattern={pattern}, phase={currentPhase}, moveMode={currentMoveMode}");

            // TODO: Replace this local test visual flow with the team's production fish presentation pipeline.
        }

        public void ResetFish()
        {
            StopPhaseMovement();
            ClearVisual();
            currentSpecies = null;
            weight = 0f;
            resistance = 0f;
            pattern = MovementPattern.Calm;
            sizeCm = 0f;
            currentPhase = FishPhase.None;
            currentMoveMode = FishMoveMode.Stop;
            isWaitingAtMovementLimit = false;

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
                    currentVisualInstance.transform.forward = movementDirection;
                }
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
                StopPhaseMovement();
                Debug.Log("[FishController] Phase cleared. Fish movement stopped.");
                return;
            }

            if (nextPhase == currentPhase)
            {
                Debug.Log($"[FishController] SetPhase ignored: already in {currentPhase}.");
                return;
            }

            currentPhase = nextPhase;
            FlipDirectionForPhaseChange();
            isPhaseMovementActive = true;
            isWaitingAtMovementLimit = false;

            Debug.Log(
                $"[FishController] Phase changed: species={SpeciesName}, phase={currentPhase}, moveMode={currentMoveMode}, speed={GetMoveSpeed():F2}");
        }

        public void ApplyInspectorDebugPhase()
        {
            SetPhase(inspectorDebugPhase);
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

        private void SpawnVisual(FishSpeciesDataSO speciesData)
        {
            ClearVisual();

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
                currentVisualInstance.transform.localScale = GetPlaceholderScale(sizeCm);
                ClampVisualPosition();
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
        }

        private static Vector3 GetPlaceholderScale(float fishSizeCm)
        {
            float normalizedLength = Mathf.Clamp(fishSizeCm / 40f, 0.4f, 2f);
            return new Vector3(0.35f, normalizedLength, 0.35f);
        }

        private void BeginPhaseMovement()
        {
            currentPhase = FishPhase.Phase1;
            currentMoveMode = UnityEngine.Random.value < 0.5f ? FishMoveMode.MoveLeft : FishMoveMode.MoveRight;
            isPhaseMovementActive = true;
            isWaitingAtMovementLimit = false;
        }

        private void StopPhaseMovement()
        {
            isPhaseMovementActive = false;
            currentMoveMode = FishMoveMode.Stop;
            isWaitingAtMovementLimit = false;
        }

        private void FlipDirectionForPhaseChange()
        {
            currentMoveMode = currentMoveMode == FishMoveMode.MoveLeft
                ? FishMoveMode.MoveRight
                : FishMoveMode.MoveLeft;
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

        private float GetMoveSpeed()
        {
            float patternSpeed = pattern switch
            {
                MovementPattern.Calm => 0.55f,
                MovementPattern.Aggressive => 0.9f,
                MovementPattern.Erratic => 1.2f,
                _ => 1f
            };

            float phaseSpeedMultiplier = currentPhase switch
            {
                FishPhase.Phase1 => 1.35f,
                FishPhase.Phase3 => 1.15f,
                FishPhase.Phase2 => 0.95f,
                FishPhase.Phase4 => 0.7f,
                _ => 0f
            };

            return patternSpeed * debugMoveSpeed * phaseSpeedMultiplier;
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
        }

        private void OnDisable()
        {
            StopPhaseMovement();
        }
    }
}
