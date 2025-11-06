using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;
    public HUDController hud; 

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Player took damage! HP: " + currentHealth);

        if (hud != null)
            hud.UpdateHealth(currentHealth, maxHealth);
            
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Player died!");

        // Отключаем InputSystem перед уничтожением игрока
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.Kill();
        }

        // Перезапускаем текущую сцену
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
