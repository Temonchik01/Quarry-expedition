using UnityEngine;

public class MapCameraControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public float scrollSpeed = 15f;      // Швидкість руху камери
    public float edgeThreshold = 30f;    // Зона біля краю екрана в пікселях (наприклад, 30px від краю)

    [Header("Limits")]
    public float minX = 0f;              // Ліва межа (старт карти)
    public float maxX = 25f;             // Права межа (кінець карти, підлаштуй під довжину)

    private MapGenerator mapGenerator;

    void Start()
    {
        // Автоматично шукаємо генератор, щоб за бажанням витягнути ліміти динамічно
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        
        if (mapGenerator != null)
        {
            // Розрахунок приблизного ліміту на основі довжини карти та відстані між поверхами
            // Залишаємо невеликий запас (+2), щоб камера не зупинялася впритул до останнього куба
            maxX = (mapGenerator.mapLength - 1) * mapGenerator.xSpacing + 2f;
        }
    }

    void Update()
    {
        MoveCameraWithMouse();
    }

    private void MoveCameraWithMouse()
    {
        Vector3 pos = transform.position;

        // 1. Рух ВПРАВО (Мишка біля правого краю екрана)
        // Screen.width - edgeThreshold означає, наприклад, праві 30 пікселів монітора
        if (Input.mousePosition.x >= Screen.width - edgeThreshold)
        {
            pos.x += scrollSpeed * Time.deltaTime;
        }
        
        // 2. Рух ВЛІВО (Мишка біля лівого краю екрана)
        // 0 + edgeThreshold означає ліві 30 пікселів монітора
        else if (Input.mousePosition.x <= edgeThreshold)
        {
            pos.x -= scrollSpeed * Time.deltaTime;
        }

        // 3. Обмеження руху (Clamp), щоб камера не улітала в безкінечність
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        // Застосовуємо нову позицію до камери
        transform.position = pos;
    }
}