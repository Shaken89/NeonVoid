using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider healthBar;
    public Text scoreText;
    public Text waveText;

    private int score = 0;

    void Start()
    {
        if (healthBar != null)
            healthBar.value = 1f; 
        UpdateScore(0);
        UpdateWave(1);
    }

    
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;
    }

    
    public void UpdateScore(int points)
    {
        score += points;
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    
    public void UpdateWave(int waveNumber)
    {
        if (waveText != null)
            waveText.text = "Wave: " + waveNumber;
    }
}
