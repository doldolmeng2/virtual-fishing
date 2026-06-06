using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VirtualFishing.Core.Events;

public class CatchResultController : MonoBehaviour
{
    [Header("UI 텍스트 연결")]
    public TextMeshProUGUI fishNameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI gradeText;

    [Tooltip("깜빡일 안내 텍스트를 여기에 연결하세요")]
    public TextMeshProUGUI autoCloseText;

    [Header("이벤트")]
    [Tooltip("결과 확인 시 FishController 등 외부 구독자에 알립니다.")]
    public UnityEvent onConfirmEvent;

    [Tooltip("결과 확인 시 프로젝트 전역 VoidEventSO 채널로도 알립니다.")]
    [SerializeField] private VoidEventSO onResultConfirmedEvent;

    [Header("설정")]
    [Tooltip("결과창이 떠 있는 시간(초)입니다. 이 시간이 지나면 자동으로 닫힙니다.")]
    public float autoCloseDelay = 7.0f;

    private Coroutine autoCloseCoroutine;
    private Coroutine blinkCoroutine;
    private bool isConfirming;

    public void DisplayResult(string name, float size, float weight, int stars)
    {
        isConfirming = false;

        fishNameText.text = name;
        statsText.text = $"크기: {size:F1}cm | 무게: {weight:F1}kg";

        string starString = "";
        for (int i = 0; i < 5; i++)
        {
            starString += i < stars ? "★" : "☆";
        }

        gradeText.text = starString;

        if (autoCloseText != null)
        {
            autoCloseText.gameObject.SetActive(true);
            autoCloseText.text = "잠시 후 자동으로 창이 닫힙니다...";

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }

            blinkCoroutine = StartCoroutine(BlinkTextRoutine());
        }
        else
        {
            Debug.LogError("<color=red>[결과창 오류]</color> Auto Close Text가 인스펙터에 연결되지 않았습니다!");
        }

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
    }

    public void OnConfirmButtonClick()
    {
        ConfirmAndClose();
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        Debug.Log("<color=cyan>[결과창]</color> 대기 시간이 초과되어 UI를 닫습니다.");
        ConfirmAndClose();
    }

    private IEnumerator BlinkTextRoutine()
    {
        if (autoCloseText == null)
        {
            yield break;
        }

        Color originalColor = autoCloseText.color;
        const float speed = 2.0f;

        while (true)
        {
            float alpha = Mathf.Lerp(0.3f, 1.0f, Mathf.PingPong(Time.unscaledTime * speed, 1.0f));
            autoCloseText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }

    private void ConfirmAndClose()
    {
        if (isConfirming)
        {
            return;
        }

        isConfirming = true;

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        onConfirmEvent?.Invoke();
        onResultConfirmedEvent?.Raise();

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        isConfirming = false;
    }
}
