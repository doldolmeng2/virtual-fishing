using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events; // 이벤트를 사용하기 위해 추가

public class CatchResultController : MonoBehaviour
{
    [Header("UI 텍스트 연결")]
    public TextMeshProUGUI fishNameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI gradeText;

    [Header("설정")]
    public float autoConfirmDelay = 10.0f; // 자동 확인 타이머 시간
    
    // 버튼이나 타이머가 작동했을 때 외부(FeedbackManager 등)로 알려줄 신호
    public UnityEvent onConfirmEvent;

    private Coroutine autoConfirmCoroutine;

    // 테스트용 임시 함수 (나중엔 실제 FishData를 받는 형태로 바뀝니다)
    public void DisplayResult(string name, float size, float weight, int stars)
    {
        // 1. UI 텍스트 갱신[cite: 5]
        fishNameText.text = name;
        statsText.text = $"크기: {size}cm\n무게: {weight}kg";
        
        // 2. 별 등급 그리기 (별 개수만큼 꽉 찬 별, 나머진 빈 별)
        string starString = "";
        for (int i = 0; i < 5; i++)
        {
            starString += (i < stars) ? "★" : "☆";
        }
        gradeText.text = starString;

        // 3. 자동 확인 타이머 시작[cite: 5]
        if (autoConfirmCoroutine != null) StopCoroutine(autoConfirmCoroutine);
        autoConfirmCoroutine = StartCoroutine(AutoConfirmRoutine());
    }

    private IEnumerator AutoConfirmRoutine()
    {
        // 설정된 시간(10초)만큼 대기[cite: 5]
        yield return new WaitForSeconds(autoConfirmDelay);
        
        // 시간이 지나면 자동으로 버튼을 누른 것과 같은 효과 발생[cite: 5]
        Debug.Log("[결과창] 시간이 초과되어 자동으로 수족관에 넣습니다.");
        ConfirmAndClose();
    }

    // '수족관에 넣기' 버튼을 클릭했을 때 실행할 함수[cite: 5]
    public void OnConfirmButtonClick()
    {
        Debug.Log("[결과창] 수동으로 수족관에 넣기 버튼을 눌렀습니다.");
        ConfirmAndClose();
    }

    private void ConfirmAndClose()
    {
        // 코루틴(타이머)이 아직 돌고 있다면 정지
        if (autoConfirmCoroutine != null) StopCoroutine(autoConfirmCoroutine);
        
        // 외부(이벤트를 구독하는 매니저)에 '저장 진행해라'고 알림
        onConfirmEvent.Invoke();
        
        // UI 닫기
        gameObject.SetActive(false);
    }
}