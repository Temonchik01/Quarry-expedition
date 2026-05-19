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

    // Метод для ініціалізації кімнати після спавну
    public void SetupRoom(MapGenerator.RoomNode data)
    {
        nodeData = data;
        // Тут можна налаштувати зовнішній вигляд (наприклад, увімкнути потрібну 3D модель)
        SetColor(normalColor);
    }

    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
        SetColor(selectable ? normalColor : lockedColor);
    }

    // Логіка взаємодії через мишку (3D Collider обов'язковий на префабі!)
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
        
        // ТУТ ЛОГІКА: 
        // 1. Передати менеджеру гри, що ми змінили кімнату
        // 2. Завантажити відповідну сцену або запустити івент/бій
    }

    private void SetColor(Color color)
    {
        if (roomRenderer != null)
        {
            roomRenderer.material.color = color;
        }
    }
}