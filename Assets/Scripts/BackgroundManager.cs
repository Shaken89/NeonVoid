using UnityEngine;

/// <summary>
/// Manages background visuals and transitions between levels/waves.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    [Header("Backgrounds by Level")]
    public SpriteRenderer backgroundRenderer;
    public Sprite[] backgrounds; // Assign different backgrounds for each level in Inspector

    /// <summary>
    /// Sets background by current level (wave).
    /// </summary>
    public void SetBackground(int level)
    {
        if (backgrounds == null || backgrounds.Length == 0 || backgroundRenderer == null)
            return;
        int index = Mathf.Clamp(level - 1, 0, backgrounds.Length - 1);
        backgroundRenderer.sprite = backgrounds[index];
    }
}
