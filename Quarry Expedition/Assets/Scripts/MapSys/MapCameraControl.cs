using UnityEngine;
// ВИПРАВЛЕННЯ ПОМИЛКИ 1: Додаємо простір імен для роботи зі сценами
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
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        
        if (mapGenerator != null)
        {
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

        // Шукаємо генератор на сцені карти та вмикаємо назад камеру
        MapGenerator generator = FindFirstObjectByType<MapGenerator>();
        if (generator != null)
        {
            // Шукаємо камеру спочатку на самому генераторі чи його дочірніх об'єктах
            Camera mapCam = generator.GetComponentInChildren<Camera>(true);
            
            // ВИПРАВЛЕННЯ ПОМИЛКИ 2: Якщо там немає, шукаємо об'єкт типу MapCameraControl на сцені
            if (mapCam == null)
            {
                MapCameraControl cameraControl = FindFirstObjectByType<MapCameraControl>(FindObjectsInactive.Include);
                if (cameraControl != null) mapCam = cameraControl.GetComponent<Camera>();
            }
            
            // Якщо все одно не знайшли — беремо головну камеру через статичне посилання
            if (mapCam == null) mapCam = Camera.main;
            
            // Вмикаємо її назад
            if (mapCam != null) mapCam.gameObject.SetActive(true);
        }
    }
}