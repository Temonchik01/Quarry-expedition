using UnityEngine;

public class MapGen : MonoBehaviour
{
    
    [SerializeField, Range(2,999)] private int width=15;
    [SerializeField, Range(1,999)] private int height=5;
    [SerializeField, Range(0,1)] private float enemyProb=0.4f;
    [SerializeField, Range(0,1)] private float campProb=0.2f;
    [SerializeField, Range(0,1)] private float eventProb=0.2f;
    [SerializeField, Range(0,1)] private float baseProb=0.2f;
    public GameObject startPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
