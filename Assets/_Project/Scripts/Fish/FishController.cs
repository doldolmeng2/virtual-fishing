using System;
using System.Collections;
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
        [SerializeField] private bool reverseDirectionAtHorizontalLimit = true;

        [Header("Runtime State")]
        [SerializeField] private FishSpeciesDataSO currentSpecies;
        [SerializeField] private float weight;
        [SerializeField] private float resistance;
        [SerializeField] private MovementPattern pattern;
        [SerializeField] private float sizeCm;
        [SerializeField] private FishMoveMode currentMoveMode = FishMoveMode.Stop;
        [SerializeField] private float currentModeDuration;
        [SerializeField] private bool isRandomMovementLoopRunning;
        [SerializeField] private float debugMoveSpeed = 1.5f;
        [SerializeField] private Vector3 visualSpawnPosition;
        [SerializeField] private bool allowStopModeInRandomLoop = false;

        private GameObject currentVisualInstance;
        private Coroutine movementModeRoutine;

        public FishSpeciesDataSO CurrentSpecies => currentSpecies;
        public string SpeciesName => currentSpecies != null ? currentSpecies.DisplayName : string.Empty;
        public float Weight => weight;
        public float Resistance => resistance;
        public MovementPattern Pattern => pattern;
        public float SizeCm => sizeCm;
        public FishMoveMode CurrentMoveMode => currentMoveMode;

        public event Action<Vector3> OnFishMoved;

        private void Update()
        {
            if (!isRandomMovementLoopRunning || currentVisualInstance == null)
            {
                return;
            }

            Vector3 movementDirection = GetMovementDirectionByMode(currentMoveMode);
            if (movementDirection == Vector3.zero)
            {
                return;
            }

            float moveSpeed = GetMoveSpeedByPattern();
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
            SetMoveMode(FishMoveMode.Stop, 0f);
            SpawnVisual(speciesData);

            Debug.Log(
                $"[FishController] Initialized fish: id={speciesData.FishId}, name={speciesData.DisplayName}, " +
                $"weight={weight:F2}kg, size={sizeCm:F1}cm, resistance={resistance:F2}, pattern={pattern}, moveMode={currentMoveMode}");

            // TODO: Replace this local test visual flow with the team's production fish presentation pipeline.
        }

        public void ResetFish()
        {
            StopRandomMovementModeLoop();
            ClearVisual();
            currentSpecies = null;
            weight = 0f;
            resistance = 0f;
            pattern = MovementPattern.Calm;
            sizeCm = 0f;
            SetMoveMode(FishMoveMode.Stop, 0f);

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

        public void StartRandomMovementModeLoop()
        {
            if (currentSpecies == null)
            {
                Debug.LogWarning("[FishController] StartRandomMovementModeLoop skipped: no fish has been initialized.");
                return;
            }

            if (movementModeRoutine != null)
            {
                StopCoroutine(movementModeRoutine);
            }

            isRandomMovementLoopRunning = true;
            movementModeRoutine = StartCoroutine(RandomMovementModeRoutine());
            Debug.Log("[FishController] Random movement mode loop started.");
        }

        public void StopRandomMovementModeLoop()
        {
            if (movementModeRoutine != null)
            {
                StopCoroutine(movementModeRoutine);
                movementModeRoutine = null;
            }

            isRandomMovementLoopRunning = false;
            SetMoveMode(FishMoveMode.Stop, 0f);
            Debug.Log("[FishController] Random movement mode loop stopped.");
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

        private IEnumerator RandomMovementModeRoutine()
        {
            while (true)
            {
                FishMoveMode nextMode = GetRandomMoveMode();
                float duration = currentSpecies != null ? currentSpecies.GetRandomMoveModeDuration() : 1f;

                SetMoveMode(nextMode, duration);
                yield return new WaitForSeconds(duration);
            }
        }

        private FishMoveMode GetRandomMoveMode()
        {
            if (!allowStopModeInRandomLoop)
            {
                return UnityEngine.Random.value < 0.5f
                    ? FishMoveMode.MoveLeft
                    : FishMoveMode.MoveRight;
            }

            int randomValue = UnityEngine.Random.Range(0, 5);
            return randomValue switch
            {
                0 => FishMoveMode.MoveLeft,
                1 => FishMoveMode.MoveLeft,
                2 => FishMoveMode.Stop,
                3 => FishMoveMode.MoveRight,
                _ => FishMoveMode.MoveRight
            };
        }

        private void SetMoveMode(FishMoveMode nextMode, float duration)
        {
            currentMoveMode = nextMode;
            currentModeDuration = duration;

            Debug.Log(
                $"[FishController] Move mode changed: species={SpeciesName}, mode={currentMoveMode}, duration={currentModeDuration:F2}s");
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

        private float GetMoveSpeedByPattern()
        {
            float patternSpeed = pattern switch
            {
                MovementPattern.Calm => 0.55f,
                MovementPattern.Aggressive => 0.9f,
                MovementPattern.Erratic => 1.2f,
                _ => 1f
            };

            return patternSpeed * debugMoveSpeed;
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

            if (!reverseDirectionAtHorizontalLimit || !isRandomMovementLoopRunning)
            {
                return;
            }

            if (hitLeftLimit && currentMoveMode == FishMoveMode.MoveLeft)
            {
                SetMoveMode(FishMoveMode.MoveRight, currentModeDuration);
                Debug.Log("[FishController] Reached left movement limit. Reversing to MoveRight.");
            }
            else if (hitRightLimit && currentMoveMode == FishMoveMode.MoveRight)
            {
                SetMoveMode(FishMoveMode.MoveLeft, currentModeDuration);
                Debug.Log("[FishController] Reached right movement limit. Reversing to MoveLeft.");
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
            StopRandomMovementModeLoop();
        }
    }
}
