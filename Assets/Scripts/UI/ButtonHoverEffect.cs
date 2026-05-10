using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Adds a smooth scale-up animation on hover for UI buttons.
/// Attach to any Button GameObject.
/// </summary>
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 8f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private UnityEngine.UI.Button btn;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        btn = GetComponent<UnityEngine.UI.Button>();
    }

    private void OnDisable()
    {
        // Когда меню прячется (SetActive(false)), OnPointerExit не срабатывает.
        // Поэтому вручную сбрасываем масштаб, чтобы при следующем открытии кнопка не была уже большой.
        if (originalScale != Vector3.zero)
        {
            targetScale = originalScale;
            transform.localScale = originalScale;
        }
    }

    private void Update()
    {
        // Если кнопка вдруг заблокировалась, пока мышка была на ней — сжимаем обратно
        if (btn != null && !btn.interactable && targetScale != originalScale)
        {
            targetScale = originalScale;
        }

        // Оптимизация: меняем scale только если мы ещё не достигли цели.
        // Иначе Canvas будет перестраиваться каждый кадр даже в состоянии покоя!
        if (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }
        else if (transform.localScale != targetScale)
        {
            transform.localScale = targetScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Не проигрываем анимацию, если кнопка заблокирована (серая)
        if (btn != null && !btn.interactable) return;
        
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}
