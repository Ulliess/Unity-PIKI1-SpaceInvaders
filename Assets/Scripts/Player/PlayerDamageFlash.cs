using UnityEngine;
using System.Collections;

/// <summary>
/// Мигание корабля при получении урона. Красная вспышка на 0.15 сек.
/// </summary>
public class PlayerDamageFlash : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        if (spriteRenderer == null) return;
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}
