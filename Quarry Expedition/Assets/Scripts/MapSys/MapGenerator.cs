using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum RoomType // Лист типів кімнат для генерації
    {
        Enemy,
        Camp,
        Base,
        Unknown
    }

    [System.Serializable] // Клас для представлення кімнати та її зв'язків
    public class RoomNode // Вузол кімнати
    {
        public RoomType type;
        public Vector2 position; // X - поверх (вперед), Y - зміщення (вбік)
        public List<RoomNode> nextRooms = new List<RoomNode>(); // Виходи
        public List<RoomNode> prevRooms = new List<RoomNode>(); // Входи
    }

    [Header("Map Settings")]
    public int mapLength = 8; // Кількість поверхів
    public int minWidth = 3; // Мінімальна кількість кімнат на одному поверсі
    public int maxWidth = 5; // Максимальна кількість кімнат на одному поверсі
    
    [Header("Spacing")]
    public float xSpacing = 3; // Відстань між поверхами (по осі X)
    public float ySpacing = 3f; // Відстань між кімнатами на одному поверсі (по осі Y)
    public float yRandomOffset = 1f; // Відхилення по Y для більш органічного вигляду

    [Header("Room Probabilities")]
    public int campChance = 20; // Відсоток шансів для кімнати з кемпом
    public int unknownChance = 20; // Відсоток шансів для невідомої кімнати
    public int baseChance = 20; // Відсоток шансів для кімнати з ворожою базою

    [Header("3D Prefabs")]
    public GameObject enemyPrefab;
    public GameObject campPrefab;
    public GameObject basePrefab;
    public GameObject unknownPrefab;
    public GameObject lineRendererPrefab;

    // СТРУКТУРА КАРТИ (Потрібна для генерації та Gizmos)
    private List<List<RoomNode>> mapLayers = new List<List<RoomNode>>();  
    
    // СПИСОК СПАВНЕНИХ ОБ'ЄКТІВ (Потрібен для гри)
    private List<MapRoom> spawnedRooms = new List<MapRoom>();  

    void Start()
    {
        GenerateMap();
        SpawnMapObjects(); // Спавнимо 3D об'єкти при старті гри
    }

    public void GenerateMap()
    {
        mapLayers.Clear();
        
        // 1. Генерація кімнат для кожного поверху
        for (int x = 0; x < mapLength; x++)
        {
            List<RoomNode> layer = new List<RoomNode>();
            int roomsInLayer = (x == 0) ? minWidth : Random.Range(minWidth, maxWidth + 1);
            
            // Розміщення кімнат на поверсі з випадковим зміщенням по Y
            for (int y = 0; y < roomsInLayer; y++)
            {
                RoomNode node = new RoomNode();
                float layerHeight = (roomsInLayer - 1) * ySpacing;
                float yPos = (y * ySpacing) - (layerHeight / 2f) + Random.Range(-yRandomOffset, yRandomOffset);
                node.position = new Vector2(x * xSpacing, yPos);
                node.type = AssignRoomType(x);
                layer.Add(node);
            }
            mapLayers.Add(layer);
        }

        // 2. З'єднання кімнат між поверхами
        for (int x = 0; x < mapLength - 1; x++)
        {
            List<RoomNode> currentLayer = mapLayers[x];
            List<RoomNode> nextLayer = mapLayers[x + 1];
            
            // Кожна кімната з поточного поверху з'єднується з однією або двома кімнатами наступного поверху
            for (int i = 0; i < currentLayer.Count; i++)
            {
                int targetIndex = Mathf.Clamp(Mathf.RoundToInt((float)i / currentLayer.Count * nextLayer.Count), 0, nextLayer.Count - 1);
                ConnectRooms(currentLayer[i], nextLayer[targetIndex]);
                
                if (Random.value < 0.4f && targetIndex + 1 < nextLayer.Count)
                {
                    ConnectRooms(currentLayer[i], nextLayer[targetIndex + 1]);
                }
                if (Random.value < 0.4f && targetIndex - 1 >= 0)
                {
                    ConnectRooms(currentLayer[i], nextLayer[targetIndex - 1]);
                }
            }
            
            // Додатково забезпечуємо, що всі кімнати наступного поверху мають принаймні одне з'єднання з поточним поверхом
            for (int j = 0; j < nextLayer.Count; j++)
            {
                if (nextLayer[j].prevRooms.Count == 0)
                {
                    int sourceIndex = Mathf.Clamp(Mathf.RoundToInt((float)j / nextLayer.Count * currentLayer.Count), 0, currentLayer.Count - 1);
                    ConnectRooms(currentLayer[sourceIndex], nextLayer[j]);
                }
            }
        }
    }

    private void ConnectRooms(RoomNode from, RoomNode to)
    {
        // Додаємо зв'язок між кімнатами, уникаючи дублікатів
        if (!from.nextRooms.Contains(to)) from.nextRooms.Add(to);
        if (!to.prevRooms.Contains(from)) to.prevRooms.Add(from);
    }

    private RoomType AssignRoomType(int layerIndex)
    {
        if (layerIndex == 0) return RoomType.Enemy;
        float rand = Random.value;
        if (rand < campChance / 100f) return RoomType.Camp;
        if (rand < (campChance + unknownChance) / 100f) return RoomType.Unknown;
        if (rand < (campChance + unknownChance + baseChance) / 100f) return RoomType.Base;
        return RoomType.Enemy;
    }

    private void SpawnMapObjects()
    {
        foreach (var room in spawnedRooms)
        {
            if (room != null) Destroy(room.gameObject);
        }
        spawnedRooms.Clear();

        foreach (var layer in mapLayers)
        {
            foreach (var node in layer)
            {
                GameObject prefabToSpawn = GetPrefabByType(node.type);
                if (prefabToSpawn == null) continue;

                Vector3 spawnPos = new Vector3(node.position.x, 0, node.position.y);
                GameObject roomObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, this.transform);
                
                MapRoom mapRoom = roomObj.GetComponent<MapRoom>();
                if (mapRoom == null) mapRoom = roomObj.AddComponent<MapRoom>();
                
                mapRoom.SetupRoom(node);
                spawnedRooms.Add(mapRoom);

                SpawnLinesForRoom(node, spawnPos);
            }
        }

        SetFirstLayerSelectable();
    }

    private GameObject GetPrefabByType(RoomType type)
    {
        switch (type)
        {
            case RoomType.Enemy: return enemyPrefab;
            case RoomType.Camp: return campPrefab;
            case RoomType.Base: return basePrefab;
            case RoomType.Unknown: return unknownPrefab;
            default: return enemyPrefab;
        }
    }

    private void SpawnLinesForRoom(RoomNode node, Vector3 startPos)
    {
        if (lineRendererPrefab == null) return;

        foreach (var nextNode in node.nextRooms)
        {
            Vector3 endPos = new Vector3(nextNode.position.x, 0, nextNode.position.y);
            GameObject lineObj = Instantiate(lineRendererPrefab, this.transform);
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, startPos);
                lr.SetPosition(1, endPos);
            }
        }
    }

    private void SetFirstLayerSelectable()
    {
        foreach (var room in spawnedRooms)
        {
            if (room.nodeData.position.x == 0)
            {
                room.SetSelectable(true);
            }
            else
            {
                room.SetSelectable(false);
            }
        }
    }

    public void OnRoomSelected(MapRoom selectedRoom)
    {
        // 1. Спочатку блокуємо абсолютно всі кімнати на карті
        foreach (var room in spawnedRooms)
        {
            room.SetSelectable(false);
        }

        // 2. Тепер відкриваємо лише ті, які зв'язані з поточною
        foreach (var nextNode in selectedRoom.nodeData.nextRooms)
        {
            MapRoom nextRoomObject = spawnedRooms.Find(r => r.nodeData == nextNode);
            if (nextRoomObject != null)
            {
                nextRoomObject.SetSelectable(true);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (mapLayers == null || mapLayers.Count == 0) return;

        foreach (var layer in mapLayers)
        {
            foreach (var node in layer)
            {
                switch (node.type)
                {
                    case RoomType.Enemy: Gizmos.color = Color.red; break;
                    case RoomType.Camp: Gizmos.color = Color.green; break;
                    case RoomType.Base: Gizmos.color = Color.magenta; break;
                    case RoomType.Unknown: Gizmos.color = Color.gray; break;
                }

                Vector3 roomPos = new Vector3(node.position.x, 0, node.position.y);
                Gizmos.DrawSphere(roomPos, 0.4f);

                Gizmos.color = Color.white;
                foreach (var nextNode in node.nextRooms)
                {
                    Vector3 nextRoomPos = new Vector3(nextNode.position.x, 0, nextNode.position.y);
                    Gizmos.DrawLine(roomPos, nextRoomPos);
                }
            }
        }
    }
}