using UnityEngine;

public class MapRoom : MonoBehaviour
{
    // Посилання на логічні дані цієї кімнати з генератора
    public MapGenerator.RoomNode nodeData;
    
    [Header("Visuals")]
    public MeshRenderer roomRenderer; // Компонент для зміни кольору/матеріалу
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color lockedColor = Color.gray;

    private bool isSelectable = false;
    private MapGenerator mapGenerator;

    void Awake()
    {
        // Якщо забув прилінкувати в інспекторі — шукаємо на цьому ж об'єкті
        if (roomRenderer == null) roomRenderer = GetComponent<MeshRenderer>();
        
        // Знаходимо генератор на сцені
        mapGenerator = FindFirstObjectByType<MapGenerator>();

        // Перевірка колайдера, бо без нього OnMouseEnter/OnMouseDown на 3D об'єктах не працюють
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"[MapRoom] На {gameObject.name} немає Collider! Кліки мишкою не працюватимуть.");
        }
    }

    // Метод для ініціалізації кімнати після спавну
    public void SetupRoom(MapGenerator.RoomNode data)
    {
        nodeData = data;
        SetColor(normalColor);
    }

    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
        SetColor(selectable ? normalColor : lockedColor);
    }

    // Логіка взаємодії через мишку
    void OnMouseEnter()
    {
        if (isSelectable) SetColor(hoverColor);
    }

    void OnMouseExit()
    {
        if (isSelectable) SetColor(normalColor);
    }

    void OnMouseDown()
    {
        if (isSelectable)
        {
            SelectRoom();
        }
    }

    private void SelectRoom()
    {
        Debug.Log($"Гравець перейшов у кімнату: {nodeData.type} на позиції {nodeData.position}");
        
        if (mapGenerator != null)
        {
            // Передаємо генератору інформацію, яку кімнату ми обрали
            mapGenerator.OnRoomSelected(this);
        }
    }

    private void SetColor(Color color)
    {
        if (roomRenderer != null && roomRenderer.material != null)
        {
            roomRenderer.material.color = color;
        }
    }
}