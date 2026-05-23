using UnityEngine;

namespace VirtualFishing.UI
{
    public class ProjectionBillboard : MonoBehaviour
    {
        private Camera mainCamera;

        private void Start()
        {
            // 씬에 있는 메인 카메라(VR HMD 카메라)를 캐싱합니다.
            mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                Debug.LogWarning("메인 카메라를 찾을 수 없습니다. MainCamera 태그가 설정되어 있는지 확인해주세요.");
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null) return;

            // 핵심 로직: 카메라의 렌즈 방향(Forward)과 캔버스의 방향을 완벽하게 평행(일치)시킵니다.
            transform.forward = mainCamera.transform.forward;
            
            // 만약 UI 텍스트가 좌우 반전되어(거울처럼) 보인다면 위 코드를 지우고 아래 코드를 사용하세요.
            // transform.forward = -mainCamera.transform.forward;
        }
    }
}