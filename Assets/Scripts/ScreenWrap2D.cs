using UnityEngine;

public class ScreenWrap2D : MonoBehaviour
{
    private Camera mainCam;
    private float halfWidth, halfHeight;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) return;

        Vector2 screenSize = mainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        halfWidth = screenSize.x;
        halfHeight = screenSize.y;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        // Проверка по X
        if (pos.x > halfWidth)
            pos.x = -halfWidth;
        else if (pos.x < -halfWidth)
            pos.x = halfWidth;

        // Проверка по Y
        if (pos.y > halfHeight)
            pos.y = -halfHeight;
        else if (pos.y < -halfHeight)
            pos.y = halfHeight;

        transform.position = pos;
    }
}
