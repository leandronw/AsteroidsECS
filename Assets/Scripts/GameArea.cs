using System.Collections;
using System.Collections.Generic;
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

        float worldWidth = worldTopRight.x - worldBottomLeft.x;
        float worldHeight = worldTopRight.y - worldBottomLeft.y;

        BottomEdge = -worldHeight / 2;
        TopEdge = worldHeight / 2;
        LeftEdge = -worldWidth / 2;
        RightEdge = worldWidth / 2;
    }
}