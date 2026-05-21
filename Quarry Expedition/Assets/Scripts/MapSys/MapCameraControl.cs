using UnityEngine;
// Додаємо простір імен для роботи зі сценами
using UnityEngine.SceneManagement; 

public class MapCameraControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public float scrollSpeed = 10f;      // Швидкість руху камери
    public float edgeThreshold = 20f;    // Зона біля краю екрана в пікселях

    [Header("Limits")]
    public float minX = 0f;              // Ліва межа (старт карти)
    public float maxX = 25f;             // Права межа (кінець карти)

    private MapGenerator mapGenerator;

    void Start()
    {
        // Автоматично шукаємо генератор, щоб за бажанням витягнути ліміти динамічно
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        
        if (mapGenerator != null)
        {
            // Розрахунок приблизного ліміту на основі довжини карти та відстані між поверхами
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
        float currentSpeed = 0f;

        // --- 1. РУХ ВПРАВО ---
        if (Input.mousePosition.x >= Screen.width - edgeThreshold)
        {
            currentSpeed = scrollSpeed;
        }
        else if (Input.mousePosition.x >= Screen.width - edgeThreshold * 2f)
        {
            currentSpeed = scrollSpeed * 0.5f;
        }
        else if (Input.mousePosition.x >= Screen.width - edgeThreshold * 3f)
        {
            currentSpeed = scrollSpeed * 0.25f;
        }

        if (currentSpeed > 0f)
        {
            pos.x += currentSpeed * Time.deltaTime;
        }

        currentSpeed = 0f; 

        // --- 2. РУХ ВЛІВО ---
        if (Input.mousePosition.x <= edgeThreshold)
        {
            currentSpeed = scrollSpeed;
        }
        else if (Input.mousePosition.x <= edgeThreshold * 2f)
        {
            currentSpeed = scrollSpeed * 0.5f;
        }
        else if (Input.mousePosition.x <= edgeThreshold * 3f)
        {
            currentSpeed = scrollSpeed * 0.25f;
        }

        if (currentSpeed > 0f)
        {
            pos.x -= currentSpeed * Time.deltaTime;
        }

        // 3. Обмеження руху (Clamp)
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        // Застосовуємо зміни
        transform.position = pos;
    }

    // Виклич цей метод з BattleManager, коли бій закінчиться: MapCameraControl.ReturnToMap("BattleScene");
    public static void ReturnToMap(string activeSceneName)
    {
        // Вивантажуємо бойову сцену з пам'яті
        SceneManager.UnloadSceneAsync(activeSceneName);

        // Шукаємо генератор на сцені карти
        MapGenerator generator = FindFirstObjectByType<MapGenerator>();
        if (generator != null)
        {
            // Знаходимо наш контейнер і вмикаємо його назад (колайдери та візуал оживають)
            Transform containerTransform = generator.transform.Find("MapVisualContainer");
            if (containerTransform != null)
            {
                containerTransform.gameObject.SetActive(true);
            }

            // Вмикаємо камеру назад
            Camera mapCam = generator.GetComponentInChildren<Camera>(true);
            if (mapCam == null)
            {
                MapCameraControl cameraControl = FindFirstObjectByType<MapCameraControl>(FindObjectsInactive.Include);
                if (cameraControl != null) mapCam = cameraControl.GetComponent<Camera>();
            }
            if (mapCam == null) mapCam = Camera.main;
            if (mapCam != null) mapCam.gameObject.SetActive(true);
        }
    }
}