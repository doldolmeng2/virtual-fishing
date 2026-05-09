using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ExitSequenceController : MonoBehaviour
{
    [Header("저장 중 UI 요소")]
    public GameObject savingText;

    [Header("저장 완료 UI 요소")]
    public GameObject savedText;
    public GameObject exitButton;

    private void OnEnable()
    {
        // 캔버스가 켜질 때 초기화 (저장 중 상태로 시작)
        savingText.SetActive(true);
        savedText.SetActive(false);
        exitButton.SetActive(false);
    }

    // AccountManager 등이 저장을 완료했을 때 이 함수를 호출합니다.
    public void OnSaveCompleted()
    {
        // 1. 저장 중 UI 끄기
        savingText.SetActive(false);

        // 2. 저장 완료 UI 켜기
        savedText.SetActive(true);
        
        // 3. TTS 안내가 끝날 시간(약 5초)을 벌어준 뒤 종료 버튼을 띄움
        StartCoroutine(ShowExitButtonDelay(5.0f)); 
    }

    private IEnumerator ShowExitButtonDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        exitButton.SetActive(true); // '완전 종료' 버튼 표시
        
        // 자동 종료 타이머 (10초 대기)[cite: 8]
        yield return new WaitForSeconds(10.0f);
        ExecuteQuit();
    }

    // '완전 종료' 버튼의 OnClick 이벤트에 연결할 함수
    public void ExecuteQuit()
    {
        Debug.Log("게임을 완전히 종료합니다.");
        Application.Quit(); // 실제 앱 종료 명령어
    }
}