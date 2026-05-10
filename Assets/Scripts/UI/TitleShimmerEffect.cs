using UnityEngine;
using TMPro;

/// <summary>
/// Creates a sweeping shimmer/gloss effect across TextMeshPro text.
/// A bright highlight moves left-to-right across individual characters.
/// </summary>
public class TitleShimmerEffect : MonoBehaviour
{
    [Header("Shimmer Settings")]
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float shimmerWidth = 0.15f;
    [SerializeField] private Color baseColor = new Color(0.55f, 0.55f, 0.6f, 1f);
    [SerializeField] private Color shimmerColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float pauseBetweenSweeps = 1.5f;

    private TextMeshProUGUI textMesh;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (textMesh == null) return;

        textMesh.ForceMeshUpdate();
        TMP_TextInfo textInfo = textMesh.textInfo;

        if (textInfo.characterCount == 0) return;

        // Calculate sweep position (0 to 1) with pause
        float cycleDuration = 1f / speed;
        float totalDuration = cycleDuration + pauseBetweenSweeps;
        timer += Time.deltaTime;
        if (timer > totalDuration) timer -= totalDuration;

        float sweepPos;
        if (timer < cycleDuration)
        {
            // Sweeping phase: move from left (-shimmerWidth) to right (1 + shimmerWidth)
            float t = timer / cycleDuration;
            sweepPos = Mathf.Lerp(-shimmerWidth * 2f, 1f + shimmerWidth * 2f, t);
        }
        else
        {
            // Pause phase: no shimmer visible
            sweepPos = -1f;
        }

        // Get text bounds for normalization
        Bounds bounds = textMesh.textBounds;
        float textMinX = bounds.min.x;
        float textMaxX = bounds.max.x;
        float textWidth = textMaxX - textMinX;

        if (textWidth <= 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            // Get character center X position, normalized to 0-1
            float charCenterX = (charInfo.bottomLeft.x + charInfo.topRight.x) / 2f;
            float normalizedX = (charCenterX - textMinX) / textWidth;

            // Calculate shimmer intensity based on distance from sweep position
            float dist = Mathf.Abs(normalizedX - sweepPos);
            float intensity = Mathf.Clamp01(1f - (dist / shimmerWidth));

            // Smooth the falloff
            intensity = intensity * intensity * (3f - 2f * intensity); // smoothstep

            Color32 charColor = Color.Lerp(baseColor, shimmerColor, intensity);

            colors[vertexIndex + 0] = charColor;
            colors[vertexIndex + 1] = charColor;
            colors[vertexIndex + 2] = charColor;
            colors[vertexIndex + 3] = charColor;
        }

        // Apply the color changes
        for (int i = 0; i < textInfo.materialCount; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
