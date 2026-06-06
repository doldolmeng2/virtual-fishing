using UnityEngine;
using TMPro;
using System.Collections;

public class CatchResultController : MonoBehaviour
{
    [Header("UI 텍스트 연결")]
    public TextMeshProUGUI fishNameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI gradeText;
    
    [Tooltip("깜빡일 안내 텍스트를 여기에 연결하세요")]
    public TextMeshProUGUI autoCloseText;

    [Header("설정")]
    [Tooltip("결과창이 떠 있는 시간(초)입니다. 이 시간이 지나면 자동으로 닫힙니다.")]
    public float autoCloseDelay = 7.0f; 

    private Coroutine autoCloseCoroutine;
    private Coroutine blinkCoroutine;

    public void DisplayResult(string name, float size, float weight, int stars)
    {
        // 1. UI 텍스트 갱신
        fishNameText.text = name;
        statsText.text = $"크기: {size:F1}cm | 무게: {weight:F1}kg";
        
        // 2. 별 등급 그리기
        string starString = "";
        for (int i = 0; i < 5; i++)
        {
            starString += (i < stars) ? "★" : "☆";
        }
        gradeText.text = starString;

        // 3. 안내 텍스트 세팅 및 깜빡임 효과 시작
        if (autoCloseText != null)
        {
            autoCloseText.gameObject.SetActive(true);
            autoCloseText.text = "잠시 후 자동으로 창이 닫힙니다...";
            
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkTextRoutine());
        }
        else
        {
            Debug.LogError("<color=red>[결과창 오류]</color> Auto Close Text가 인스펙터에 연결되지 않았습니다!");
        }

        // 4. 자동 닫기 타이머 시작
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        Debug.Log("<color=cyan>[결과창]</color> 대기 시간이 초과되어 UI를 닫습니다.");
        gameObject.SetActive(false); 
    }

    private IEnumerator BlinkTextRoutine()
    {
        if (autoCloseText == null) yield break;

        Color originalColor = autoCloseText.color;
        float speed = 2.0f; 

        while (true)
        {
            float alpha = Mathf.Lerp(0.3f, 1.0f, Mathf.PingPong(Time.unscaledTime * speed, 1.0f));
            
            autoCloseText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            yield return null;
        }
    }

    // UI가 비활성화될 때 안전하게 코루틴을 멈춰줍니다.
    private void OnDisable()
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
    }
}