using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pathfinding
{
    #region Initialization
    public class node
    {
        public int[] nodePos;
        public node parent;
        public int g;
        public int h;
        public int f;
        public bool obstacle;
        public List<int> occupied;
        public node(int[] _nodePos, bool _obstacle, List<int> _occupied)
        {
            g = 0;
            h = 0;
            f = g + h;
            obstacle = _obstacle;
            occupied = _occupied;
            nodePos = _nodePos;
            parent = null;
        }
    }

    public class grid
    {
        public int width;
        public int height;
        public node[,] cells;
        public grid(int _width, int _height, int _xbotSize)
        {
            // Note: the width and height are reduced by _xbotSize as before.
            width = _width - _xbotSize;
            height = _height - _xbotSize;
            cells = new node[width, height];
            InitializeNodes(_xbotSize);
        }

        public void InitializeNodes(int _xbotSize)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = new node(new int[] { x, y }, false, new());
                }
            }

            double[,] zone2 = {
                { (width + _xbotSize) * (2.0 / 4) - _xbotSize / 2, (height + _xbotSize) * (1.0 / 3) - _xbotSize / 2 },
                { (width) * (3.0 / 4) + _xbotSize / 2, (height) * (2.0 / 3) + _xbotSize / 2 }
            };

            // Convert to integers
            int xStart = (int)Math.Round(zone2[0, 0]);
            int yStart = (int)Math.Round(zone2[0, 1]);
            int xEnd = (int)Math.Round(zone2[1, 0]);
            int yEnd = (int)Math.Round(zone2[1, 1]);

            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    cells[x, y].obstacle = true;
                }
            }
        }

        public List<node> getNeighbors(node _node)
        {
            List<(int, int)> directions = new()
            {
                (1, 0),  // Right
                (-1, 0), // Left
                (0, 1),  // Down
                (0, -1), // Up
                (1, 1),  // Down-Right
                (1, -1), // Up-Right
                (-1, 1), // Down-Left
                (-1, -1) // Up-Left
            };

            // Optionally, you could allow a “waiting” neighbor: (0,0)
            // directions.Add((0,0));

            List<node> neighbors = new List<node>();

            foreach (var direction in directions)
            {
                int[] newPos = { _node.nodePos[0] + direction.Item1, _node.nodePos[1] + direction.Item2 };

                if (newPos[0] >= 0 && newPos[0] < width && newPos[1] >= 0 && newPos[1] < height)
                {
                    neighbors.Add(cells[newPos[0], newPos[1]]);
                }
            }

            return neighbors;
        }

        public bool isWalkable(node _node, int _xbotID)
        {
            int x = _node.nodePos[0];
            int y = _node.nodePos[1];
            if (x >= 0 && y >= 0 && x < width && y < height && !_node.obstacle && !_node.occupied.Contains(_xbotID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void removeOccupied()
        {
            foreach (node cell in cells)
            {
                cell.occupied.Clear();
            }
        }

        public void makeUnWalkable(node _node, int _xbotID, bool _obstacle) // Set node as unwalkable
        {
            if (_obstacle)
            {
                _node.obstacle = true;
            }
            else
            {
                if (!_node.occupied.Contains(_xbotID))
                    _node.occupied.Add(_xbotID);
            }
        }

        // Added clone method for simulating changes without affecting the original grid.
        public grid Clone(int _xbotSize)
        {
            grid newGrid = new grid(width + _xbotSize, height + _xbotSize, _xbotSize);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newGrid.cells[x, y].obstacle = cells[x, y].obstacle;
                    newGrid.cells[x, y].occupied = new List<int>(cells[x, y].occupied);
                }
            }
            return newGrid;
        }
    }

    #endregion

    #region ASTAR Pathfinding
    public (List<node>, int) aStar(int[] _from, int[] _to, grid _grid, int _xbotID)
    {
        node startNode = _grid.cells[_from[0], _from[1]];
        node endNode = _grid.cells[_to[0], _to[1]];

        List<node> openList = new();
        HashSet<node> closedList = new();

        openList.Add(startNode); // Add the start node to the open list

        while (openList.Count > 0)
        {
            node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].f < currentNode.f || openList[i].f == currentNode.f && openList[i].h < currentNode.h)
                {
                    currentNode = openList[i];
                }
            }
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (currentNode == endNode)
            {
                List<node> path = new();
                int fCost = 0;
                while (currentNode != startNode)
                {
                    fCost += currentNode.f;
                    path.Add(currentNode);
                    currentNode = currentNode.parent;
                }
                path.Reverse();
                return (path, fCost);
            }

            foreach (node neighbor in _grid.getNeighbors(currentNode))
            {
                if (!_grid.isWalkable(neighbor, _xbotID) || closedList.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.g + Convert.ToInt32(getDistance(currentNode, neighbor, false));
                if (newMovementCostToNeighbor < neighbor.g || !openList.Contains(neighbor))
                {
                    neighbor.g = newMovementCostToNeighbor;
                    neighbor.h = Convert.ToInt32(getDistance(neighbor, endNode, false));
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = currentNode;
                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }

        }
        //Debug.LogError("No A* path found");
        return (null, 0);
    }
    #endregion

    #region Priority Planner
    public List<(int, List<node>, int)> priorityPlanner(grid _grid, List<(int, int[], int[])> _xBotID_From_To, int _xbotSize)
    {
        int[,] priorityMatrix = getPriorityMatrix(_xBotID_From_To);
        List<(int, List<node>, int)> oldPathList = null;

        Debug.Log("Entries in priorityMatrix: " + priorityMatrix.GetLength(0));
        for (int i = 0; i < priorityMatrix.GetLength(0); i++)
        {
            SortedList<int, int> priorityList = new SortedList<int, int>();
            for (int j = 0; j < priorityMatrix.GetLength(1); j++)
            {
                int botID = _xBotID_From_To.Select(x => x.Item1).ToList()[j];
                int priority = priorityMatrix[i, j];
                priorityList.Add(priority, botID);
            }

            List<(int, List<node>, int)> newPathList = conflictHandler(_xBotID_From_To, _xbotSize, _grid, priorityList);

            if (i == 0)
            {
                oldPathList = newPathList;
            }
            if (newPathList.Select(x => (long)x.Item3).Sum() < oldPathList.Select(x => (long)x.Item3).Sum())
            {
                oldPathList = newPathList;
            }
        }
        Debug.Log("Tried: " + priorityMatrix.GetLength(0) + " different priorities.");
        return oldPathList;
    }
    #endregion

    #region PathPlan Runner
    public List<(int, List<(int, int)>)> pathPlanRunner(grid _grid, List<(int, int[], int[])> _xBotID_From_To, int _xbotSize)
    {
        List<(int, List<node>, int)> pathList = priorityPlanner(_grid, _xBotID_From_To, _xbotSize);
        List<(int, List<(int, int)>)> output = new();

        foreach (var path in pathList)
        {
            output.Add((path.Item1, path.Item2.Select(x => (x.nodePos[0], x.nodePos[1])).ToList()));
        }
        return output;
    }
    #endregion

    #region Conflict Searcher
    public List<(int, node, int, node)> conflictSearcher(List<(int, List<node>, int)> _pathList, int _xbotSize)
    {
        List<(int, node, int, node)> conflicts = new();
        List<int> xBotIDs = _pathList.Select(x => x.Item1).ToList();
        foreach (int xbotID in xBotIDs)
        {
            foreach (int otherXbotID in xBotIDs)
            {
                if (xbotID == otherXbotID || xbotID > otherXbotID) // Skip if the same bot or if the pair has already been checked
                {
                    continue;
                }
                List<node> xbotPath = new List<node>(_pathList.FirstOrDefault(x => x.Item1 == xbotID).Item2);
                List<node> otherXbotPath = new List<node>(_pathList.FirstOrDefault(x => x.Item1 == otherXbotID).Item2);
                int minLength = Math.Min(xbotPath.Count, otherXbotPath.Count);
                for (int timeStep = 0; timeStep < minLength; timeStep++)
                {
                    double distance = getDistance(xbotPath[timeStep], otherXbotPath[timeStep], true);
                    if (distance <= Math.Sqrt(_xbotSize * _xbotSize + _xbotSize * _xbotSize) + 0.1 * _xbotSize && !conflicts.Any(c => (c.Item1 == xbotID && c.Item3 == otherXbotID)))
                    {
                        conflicts.Add((xbotID, xbotPath[timeStep], otherXbotID, otherXbotPath[timeStep]));
                    }
                }
            }
        }
        return conflicts;
    }
    #endregion

    #region Enhanced Conflict Handler
    // This conflict handler now simulates two possible resolutions for a conflict:
    //  1) Waiting: The waiting cost is now computed using a nonlinear function (square-root)
    //     to encourage waiting longer before opting for a reroute.
    //  2) Walking around: It forces a detour by marking the conflict node as unwalkable.
    // It then chooses the option with the lower simulated total cost.
    public List<(int, List<node>, int)> conflictHandler(
        List<(int, int[], int[])> _xBotID_From_To,
        int _xbotSize,
        grid _grid,
        SortedList<int, int> priorityList)
    {
        int iteration = 0;
        long iterationMax = Convert.ToInt64(Math.Pow(10, 10));
        const int MAX_WAIT = 50;
        const int WAIT_PENALTY = 0;

        Dictionary<int, int> botWaitTime = new();
        Dictionary<int, List<node>> botWaitingPaths = new(); // Tracks waiting steps for each bot

        while (iteration < iterationMax)
        {
            List<(int, List<node>, int)> pathList = new();
            Dictionary<int, List<node>> botPaths = new();

            // Compute paths for each bot, including any waiting steps
            foreach (var botTuple in _xBotID_From_To)
            {
                int botID = botTuple.Item1;
                int[] from = { botTuple.Item2[0], botTuple.Item2[1] };
                int[] to = { botTuple.Item3[0], botTuple.Item3[1] };

                List<node> waitingPath = botWaitingPaths.ContainsKey(botID) ? botWaitingPaths[botID] : new List<node>();

                (List<node> newPath, int cost) = aStar(from, to, _grid, botID);
                if (newPath != null && cost != 0)
                {
                    List<node> fullPath = waitingPath.Concat(newPath).ToList();
                    // Use the modified cost function: add WAIT_PENALTY * sqrt(waitTime+1)
                    int currentWaitTime = botWaitTime.ContainsKey(botID) ? botWaitTime[botID] : 0;
                    int totalCost = cost + (WAIT_PENALTY);
                    botPaths[botID] = fullPath;
                    pathList.Add((botID, fullPath, totalCost));
                }
                else
                {
                    Debug.Log("No path found for bot " + botID);
                }
            }

            List<(int, node, int, node)> conflicts = conflictSearcher(pathList, _xbotSize);
            if (conflicts.Count == 0)
            {
                _grid.removeOccupied();
                return pathList;
            }

            // Process each conflict found.
            foreach (var conflictTuple in conflicts)
            {
                int botID1 = conflictTuple.Item1;
                int botID2 = conflictTuple.Item3;
                int priority1 = priorityList.FirstOrDefault(x => x.Value == botID1).Key;
                int priority2 = priorityList.FirstOrDefault(x => x.Value == botID2).Key;
                int lowerPriorityBot = (priority1 > priority2) ? botID1 : botID2;
                node conflictNode = (lowerPriorityBot == botID1) ? conflictTuple.Item2 : conflictTuple.Item4;

                // Retrieve starting and target positions for the lower priority bot.
                var botInfo = _xBotID_From_To.First(x => x.Item1 == lowerPriorityBot);
                int[] from = { botInfo.Item2[0], botInfo.Item2[1] };
                int[] to = { botInfo.Item3[0], botInfo.Item3[1] };

                // Get current computed cost for the lower priority bot.
                var currentPathEntry = pathList.First(x => x.Item1 == lowerPriorityBot);
                int currentCost = currentPathEntry.Item3;

                // --- Simulate walk-around option ---
                grid walkClone = _grid.Clone(_xbotSize);
                walkClone.makeUnWalkable(conflictNode, lowerPriorityBot, false);
                (List<node> simWalkPath, int simWalkCost) = aStar(from, to, walkClone, lowerPriorityBot);
                if (simWalkPath == null || simWalkCost == 0)
                {
                    simWalkCost = int.MaxValue;
                }

                // --- Compute waiting option cost using the modified cost function ---
                int currentWaitTimeForBot = botWaitTime.ContainsKey(lowerPriorityBot) ? botWaitTime[lowerPriorityBot] : 0;
                int waitOptionCost = currentCost + (int)(WAIT_PENALTY * Math.Sqrt(currentWaitTimeForBot + 1));

                // Choose the option with the lower extra cost.
                if (currentWaitTimeForBot < MAX_WAIT && waitOptionCost <= simWalkCost)
                {
                    // Choose waiting.
                    if (!botWaitTime.ContainsKey(lowerPriorityBot))
                        botWaitTime[lowerPriorityBot] = 0;
                    botWaitTime[lowerPriorityBot]++;

                    // Append a waiting step (using the starting node of the current path)
                    node startNode = currentPathEntry.Item2.First();
                    if (!botWaitingPaths.ContainsKey(lowerPriorityBot))
                        botWaitingPaths[lowerPriorityBot] = new List<node>();
                    botWaitingPaths[lowerPriorityBot].Add(startNode);

                    Debug.Log($"Bot {lowerPriorityBot} chooses to wait. New wait time: {botWaitTime[lowerPriorityBot]}.");
                }
                else
                {
                    // Choose walk-around option.
                    _grid.makeUnWalkable(conflictNode, lowerPriorityBot, false);
                    Debug.Log($"Bot {lowerPriorityBot} chooses to walk around the conflict at ({conflictNode.nodePos[0]}, {conflictNode.nodePos[1]}).");
                }
            }

            iteration++;
        }

        Debug.LogError("No conflict solution found");
        return null;
    }
    #endregion


    #region Distance Calculator
    public double getDistance(node _nodeA, node _nodeB, bool euclidean)
    {
        if (euclidean)
        {
            double dstX = Math.Abs(_nodeA.nodePos[0] - _nodeB.nodePos[0]);
            double dstY = Math.Abs(_nodeA.nodePos[1] - _nodeB.nodePos[1]);
            return Math.Sqrt(dstX * dstX + dstY * dstY);
        }
        else
        {
            int dstX = Math.Abs(_nodeA.nodePos[0] - _nodeB.nodePos[0]);
            int dstY = Math.Abs(_nodeA.nodePos[1] - _nodeB.nodePos[1]);
            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
    #endregion

    #region Priority Matrix
    public int[,] getPriorityMatrix(List<(int, int[], int[])> _xBotID_From_To)
    {
        var xBots = _xBotID_From_To.Select(x => x.Item1).ToList();
        int botCount = xBots.Count;

        // Generate unique priority values (1 to botCount)
        List<int> priorities = Enumerable.Range(1, botCount).ToList();

        // Get all permutations of unique priority assignments
        var permutations = GetPermutations(priorities.ToArray()).ToArray();

        int[,] priorityMatrix = new int[permutations.Length, botCount];

        for (int i = 0; i < permutations.Length; i++)
        {
            for (int j = 0; j < botCount; j++)
            {
                priorityMatrix[i, j] = permutations[i].ElementAt(j);
            }
        }

        return priorityMatrix;
    }

    #region Get Permutations
    static IEnumerable<IEnumerable<T>> GetPermutations<T>(T[] list) =>
        list.Length == 1 ? new[] { list } :
        list.SelectMany((item, index) =>
            GetPermutations(list.Where((_, i) => i != index).ToArray())
            .Select(perm => new[] { item }.Concat(perm)));
    #endregion
    #endregion
}
