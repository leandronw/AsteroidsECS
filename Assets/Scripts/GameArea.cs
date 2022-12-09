using Unity.Mathematics;
using UnityEngine;

/*
 * Singleton that provides the game area edges for anyone that needs them
 */
public class GameArea : MonoBehaviour
{
    public static GameArea Instance { get; private set;} // singleton instance

    public float BottomEdge { get; private set; }
    public float TopEdge { get; private set; }
    public float LeftEdge { get; private set; }
    public float RightEdge { get; private set; }

    public float Width { get; private set; }
    public float Height { get; private set; }

    private Vector2 screenResolution;


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        screenResolution = new Vector2(Screen.width, Screen.height);
        CalculateEdges();        
    }

    private void Update()
    {
        if (screenResolution.x != Screen.width || screenResolution.y != Screen.height)
        {
            screenResolution = new Vector2(Screen.width, Screen.height);
            CalculateEdges();
        }
    }

    private void CalculateEdges()
    {
        Camera camera = Camera.main;

        Vector3 worldBottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 worldTopRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        Width = worldTopRight.x - worldBottomLeft.x;
        Height = worldTopRight.y - worldBottomLeft.y;

        BottomEdge = -Height / 2;
        TopEdge = Height / 2;
        LeftEdge = -Width / 2;
        RightEdge = Width / 2;

        Debug.Log($"Game Area - Width: {Width} - Height: {Height}");
    }

    static public float2 GetRandomPosition()
    {
        float2 randomPosition = new float2(
            UnityEngine.Random.Range(Instance.LeftEdge, Instance.RightEdge),
            UnityEngine.Random.Range(Instance.BottomEdge, Instance.TopEdge));

        return randomPosition;
    }

    static public float2 GetRandomPositionFarFromPlayer(float2 playerPosition, float threshold)
    {
        float2 randomPosition = GetRandomPosition();

        if (math.distance(playerPosition, randomPosition) < threshold)
        {
            randomPosition.x += Instance.Width * 0.5f;
        }

        return randomPosition;
    }
}