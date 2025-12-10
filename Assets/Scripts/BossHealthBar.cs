using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying boss health bar
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Colors")]
    [SerializeField] private Gradient healthGradient;

    private float currentFill = 1f;

    private void Start()
    {
        if (bossNameText != null)
            bossNameText.text = "BOSS";

        UpdateHealth(1f);
    }

    public void UpdateHealth(float fillAmount)
    {
        currentFill = Mathf.Clamp01(fillAmount);

        if (fillImage != null)
        {
            fillImage.fillAmount = currentFill;

            if (healthGradient != null && healthGradient.colorKeys.Length > 0)
                fillImage.color = healthGradient.Evaluate(currentFill);
        }

        if (healthText != null)
        {
            healthText.text = Mathf.RoundToInt(currentFill * 100f) + "%";
        }
    }

    public void SetBossName(string name)
    {
        if (bossNameText != null)
            bossNameText.text = name;
    }
}
