using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VirtualFishing.Core.Fish
{
    public class FishTestManager : MonoBehaviour
    {
        [SerializeField] private FishSpawner fishSpawner;
        [SerializeField] private KeyCode triggerKey = KeyCode.Space;
        [FormerlySerializedAs("movementTestKey")]
        [SerializeField] private KeyCode startRandomMovementKey = KeyCode.G;
        [SerializeField] private KeyCode stopRandomMovementKey = KeyCode.S;
        [SerializeField] private FishController fishController;
        [SerializeField] private FishEnvironmentController environmentController;

        private bool startMovementOnNextBite;

        private void Awake()
        {
            NormalizeLegacyKeyBindings();

            if (fishSpawner == null)
            {
                fishSpawner = GetComponent<FishSpawner>();
            }

            if (fishController == null)
            {
                fishController = GetComponent<FishController>();
            }

            if (environmentController == null)
            {
                environmentController = GetComponent<FishEnvironmentController>();
            }
        }

        private void OnEnable()
        {
            if (fishSpawner != null)
            {
                fishSpawner.OnBiteOccurred += HandleBiteOccurred;
            }
        }

        private void OnDisable()
        {
            if (fishSpawner != null)
            {
                fishSpawner.OnBiteOccurred -= HandleBiteOccurred;
            }
        }

        private void Update()
        {
            if (fishSpawner == null)
            {
                return;
            }

            if (WasKeyPressed(triggerKey))
            {
                Debug.Log("[FishTestManager] Space pressed. Starting fish bite test cycle.");
                fishSpawner.StartBiteTimer();
            }

            if (WasKeyPressed(startRandomMovementKey))
            {
                if (fishController == null)
                {
                    Debug.LogWarning("[FishTestManager] Random movement start skipped: FishController is not assigned.");
                    return;
                }

                if (fishController.CurrentSpecies == null)
                {
                    startMovementOnNextBite = true;
                    Debug.Log("[FishTestManager] G pressed before fish initialization. Movement will start automatically on the next bite.");
                    return;
                }

                startMovementOnNextBite = false;
                Debug.Log("[FishTestManager] G pressed. Starting random movement mode loop.");
                fishController.StartRandomMovementModeLoop();
            }

            if (WasKeyPressed(stopRandomMovementKey))
            {
                if (fishController == null)
                {
                    Debug.LogWarning("[FishTestManager] Random movement stop skipped: FishController is not assigned.");
                    return;
                }

                startMovementOnNextBite = false;
                Debug.Log("[FishTestManager] S pressed. Stopping random movement mode loop.");
                fishController.StopRandomMovementModeLoop();
            }
        }

        [ContextMenu("Apply Environment")]
        private void ApplyEnvironment()
        {
            if (environmentController == null)
            {
                Debug.LogWarning("[FishTestManager] ApplyEnvironment skipped: FishEnvironmentController is not assigned.");
                return;
            }

            environmentController.ApplyEnvironment();
        }

        private static bool WasKeyPressed(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key key = ConvertToInputSystemKey(keyCode);
                return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
            }
            return false;
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static Key ConvertToInputSystemKey(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Space => Key.Space,
                KeyCode.G => Key.G,
                KeyCode.S => Key.S,
                _ => Key.None
            };
        }
#endif

        private void OnValidate()
        {
            NormalizeLegacyKeyBindings();
        }

        private void NormalizeLegacyKeyBindings()
        {
            if (startRandomMovementKey == KeyCode.M)
            {
                startRandomMovementKey = KeyCode.G;
            }
        }

        private void HandleBiteOccurred(Data.FishSpeciesDataSO speciesData)
        {
            if (speciesData == null)
            {
                Debug.LogWarning("[FishTestManager] BiteOccurred received with null species.");
                return;
            }

            Debug.Log($"[FishTestManager] BiteOccurred event received: {speciesData.DisplayName}");

            if (!startMovementOnNextBite || fishController == null)
            {
                return;
            }

            startMovementOnNextBite = false;
            Debug.Log("[FishTestManager] Pending movement request detected. Starting random movement mode loop after bite.");
            fishController.StartRandomMovementModeLoop();
        }
    }
}
