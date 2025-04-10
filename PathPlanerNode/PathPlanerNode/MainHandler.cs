//using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
/*
public class MainHandler : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject pathPrefab;
    public GameObject shuttlePrefab;
    public GameObject goalPrefab;
    public GameObject obstaclePrefab;

    public Pathfinding.grid gridGlobal;
    public Pathfinding pathfinder;

    private Dictionary<int, List<Vector3>> shuttlePaths = new();
    private Dictionary<int, GameObject> shuttles = new();
    private bool isMoving = false;
    private float moveSpeed = 200f;
    private float stepTime = 0.075f;
    
    void Start()
    {
        int xbotSize = 12;
        int width = 96;
        int height = 72;
        gridGlobal = new Pathfinding.grid(width, height, xbotSize);
        pathfinder = new Pathfinding();

        int w = width - xbotSize - 1;
        int h = height - xbotSize - 1;
        int r = xbotSize / 2;
        int k = 20;
        int minD = Convert.ToInt32(Math.Round(Math.Sqrt(xbotSize * xbotSize + xbotSize * xbotSize)));
        List<(int, int[], int[])> xBotID_From_To = new()
        {
            //(1, new int[] { r, r}, new int[] { r, h - r }),
            //(2, new int[] { r, h - r }, new int[] { r, r }), // 1 og 2 går mod i hinanden head on.
            (3, new int[] { r, r }, new int[] { r, 30 }),
            (4, new int[] { r + minD, r }, new int[] { r, r }), // 4 tager 3's plads
            /*(5, new int[] { 0, 30 }, new int[] { 83, 30 }),
            (6, new int[] { 83, 30 }, new int[] { 0, 30 }),
            (7, new int[] { 42, 0 }, new int[] { 42, 59 }),
            (8, new int[] { 42, 59 }, new int[] { 42, 0 }),
        };

        GeneratePrefabVisual();
        RunPathfinder(xBotID_From_To, xbotSize);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving)
        {
            StartCoroutine(MoveShuttles());
        }
    }

    void GeneratePrefabVisual()
    {
        if (nodePrefab == null)
        {
            Debug.LogError("Node prefab not assigned in MainHandler!");
            return;
        }

        for (int x = 0; x < gridGlobal.width; x++)
        {
            for (int y = 0; y < gridGlobal.height; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject nodeInstance = Instantiate(nodePrefab, position, Quaternion.identity);
                nodeInstance.transform.SetParent(transform);
                if (gridGlobal.cells[x, y].obstacle)
                {
                    GameObject obstacleInstance = Instantiate(obstaclePrefab, position, Quaternion.identity);
                    obstacleInstance.transform.SetParent(transform);
                }
            }
        }

        Debug.Log("Grid visualization instantiated.");
    }

    void RunPathfinder(List<(int, int[], int[])> _xBotID_From_To, int _xbotSize)
    {       
        // Clear dictionaries to prevent duplicate keys from previous runs
        shuttles.Clear();
        shuttlePaths.Clear();

        var paths = pathfinder.pathPlanRunner(gridGlobal, _xBotID_From_To, _xbotSize);

        foreach (var path in paths)
        {
            int botID = path.Item1;
            List<(int, int)> nodePath = path.Item2;

            if (nodePath.Count == 0)
            {
                Debug.Log($"No valid path found for Bot {botID}.");
                continue;
            }

            Vector3 startPosition = new Vector3(nodePath[0].Item1, nodePath[0].Item2, 0);
            Vector3 goalPosition = new Vector3(nodePath[^1].Item1, nodePath[^1].Item2, 0);

            if (!shuttles.ContainsKey(botID))
            {
                GameObject shuttleInstance = Instantiate(shuttlePrefab, startPosition, Quaternion.identity);
                shuttleInstance.name = $"Shuttle_{botID}";
                shuttles[botID] = shuttleInstance;
            }

            if (!shuttlePaths.ContainsKey(botID))
            {
                List<Vector3> pathPositions = nodePath.Select(node => new Vector3(node.Item1, node.Item2, 0)).ToList();
                shuttlePaths[botID] = pathPositions;

                GameObject goalInstance = Instantiate(goalPrefab, goalPosition, Quaternion.identity);
                goalInstance.name = $"Goal_{botID}";

                foreach (var node in pathPositions)
                {
                    Instantiate(pathPrefab, node, Quaternion.identity);
                }

                Debug.Log($"Bot {botID} path instantiated.");
            }
            else
            {
                Debug.LogWarning($"Duplicate bot ID detected: {botID}. Skipping.");
            }
        }
    }


    IEnumerator MoveShuttles()
    {
        isMoving = true;
        bool anyShuttleMoving = true;

        int stepIndex = 1;
        while (anyShuttleMoving)
        {
            anyShuttleMoving = false;
            foreach (var botID in shuttlePaths.Keys)
            {
                if (stepIndex < shuttlePaths[botID].Count)
                {
                    shuttles[botID].transform.position = Vector3.MoveTowards(
                        shuttles[botID].transform.position,
                        shuttlePaths[botID][stepIndex],
                        moveSpeed * Time.deltaTime
                    );
                    anyShuttleMoving = true;
                }
            }
            stepIndex++;
            yield return new WaitForSeconds(stepTime);
        }
        isMoving = false;
    }


}*/

