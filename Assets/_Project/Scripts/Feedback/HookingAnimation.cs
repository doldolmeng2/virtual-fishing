using UnityEngine;

public class HookingAnimation : MonoBehaviour
{
    private RectTransform rectTransform;
    private float originalY;
    public float floatSpeed = 5f; // 깜빡이는 속도
    public float floatHeight = 50f; // 위로 올라가는 높이 (픽셀 단위)

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalY = rectTransform.anchoredPosition.y;
    }

    void OnEnable()
    {
        // UI가 켜질 때마다 원래 위치로 초기화
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, originalY);
    }

    void Update()
    {
        // Sin 함수를 이용해 위아래로 부드럽게 움직임 (위로 올라가는 느낌 강조)
        float newY = originalY + Mathf.Abs(Mathf.Sin(Time.time * floatSpeed)) * floatHeight;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);
    }
}