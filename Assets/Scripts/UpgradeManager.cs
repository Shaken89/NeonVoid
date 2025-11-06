using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public PlayerController player;

    public void ApplyRandomUpgrade()
    {
        int random = Random.Range(0, 3);
        switch (random)
        {
            case 0:
                player.moveSpeed += 1f;
                break;
            case 1:
                player.bulletSpeed += 2f;
                break;
            case 2:
                player.bulletPrefab.transform.localScale *= 1.2f;
                break;
        }
    }
}
