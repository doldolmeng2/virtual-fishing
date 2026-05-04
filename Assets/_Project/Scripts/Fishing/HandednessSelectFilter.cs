using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 특정 손(Left/Right)으로만 select(grab) 가능하도록 제한하는 필터.
    /// XRGrabInteractable의 Starting Select Filters 리스트에 등록해 사용.
    /// </summary>
    public class HandednessSelectFilter : MonoBehaviour, IXRSelectFilter
    {
        [Tooltip("이 손으로만 grab을 허용. None을 지정하면 모든 손 허용.")]
        [SerializeField] private InteractorHandedness allowedHand = InteractorHandedness.Right;

        public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (allowedHand == InteractorHandedness.None) return true;
            return interactor.handedness == allowedHand;
        }
    }
}
