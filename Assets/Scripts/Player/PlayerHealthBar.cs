using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Полоска HP над кораблём игрока. Создаётся автоматически при спавне.
/// Не требует никакой ручной настройки в Unity Editor.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    private PlayerShip ship;
    private SpriteRenderer bgBar;   // Серый фон
    private SpriteRenderer hpBar;   // Зелёная полоска
    private SpriteRenderer shipSprite;

    private float barWidth = 0.8f;
    private float barHeight = 0.08f;
    private float barOffsetY = 0.5f;

    void Start()
    {
        ship = GetComponent<PlayerShip>();
        shipSprite = GetComponent<SpriteRenderer>();
        
        if (shipSprite != null)
        {
            barOffsetY = shipSprite.bounds.extents.y + 0.15f;
        }
        
        CreateBar();
    }

    void CreateBar()
    {
        // Создаём фон полоски (серый)
        GameObject bgObj = new GameObject("HP_Background");
        bgObj.transform.SetParent(transform);
        bgObj.transform.localPosition = new Vector3(0, barOffsetY, 0);
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgBar = bgObj.AddComponent<SpriteRenderer>();
        bgBar.sprite = MakeSquareSprite();
        bgBar.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgBar.sortingOrder = 10;

        // Создаём полоску HP (зелёная)
        GameObject hpObj = new GameObject("HP_Fill");
        hpObj.transform.SetParent(transform);
        hpObj.transform.localPosition = new Vector3(0, barOffsetY, 0);
        hpObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        hpBar = hpObj.AddComponent<SpriteRenderer>();
        hpBar.sprite = MakeSquareSprite();
        hpBar.color = Color.green;
        hpBar.sortingOrder = 11;
    }

    void Update()
    {
        if (ship == null || hpBar == null) return;

        float ratio = ship.GetHealthRatio();
        
        // Масштабируем полоску по X
        Vector3 scale = hpBar.transform.localScale;
        scale.x = barWidth * ratio;
        hpBar.transform.localScale = scale;

        // Сдвигаем, чтобы полоска "убывала" справа налево
        float offset = (barWidth - scale.x) * 0.5f;
        Vector3 pos = hpBar.transform.localPosition;
        pos.x = -offset;
        hpBar.transform.localPosition = pos;

        // Меняем цвет: зелёный → жёлтый → красный
        if (ratio > 0.5f)
            hpBar.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
        else
            hpBar.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
    }

    private Sprite MakeSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
