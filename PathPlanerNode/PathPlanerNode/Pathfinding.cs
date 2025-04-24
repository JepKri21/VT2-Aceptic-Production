using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public class Pathfinding
{
    #region Initialization
    public class node
    {
        public int x;
        public int y;
        public node parent;
        public int g;
        public int h;
        public int f;
        public bool obstacle;
        public List<int> occupied;
        public node(int _x, int _y, bool _obstacle, List<int> _occupied)
        {
            g = 0;
            h = 0;
            f = g + h;
            obstacle = _obstacle;
            occupied = _occupied;
            x = _x;
            y = _y;
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
            cells = new node[width+1, height+1];
            InitializeNodes(_xbotSize);
        }

        public void InitializeNodes(int _xbotSize)
        {
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    cells[x, y] = new node(x, y, false, new());
                }
            }
/*
            double[,] zone2 = {
                { (width + _xbotSize) * (1.0 / 3) - _xbotSize, (height + _xbotSize) * (2.0 / 4) - _xbotSize },
                { (width + _xbotSize) * (2.0 / 3), (height + _xbotSize) * (3.0 / 4) }
            };
*/
            double[,] zone2 = {
                { 12, 36},
                { 48, 72 }
            };

            // Convert to integers
            int xStart = (int)Math.Round(zone2[0, 0])+1;
            int yStart = (int)Math.Round(zone2[0, 1])+1;
            int xEnd = (int)Math.Round(zone2[1, 0])-1;
            int yEnd = (int)Math.Round(zone2[1, 1])-1;

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    if (x > 0 && x < width && y > 0 && y < height)
                    {
                        cells[x, y].obstacle = true;
                    }
                }
            }
        }
        public void SaveWalkablePointsToFile(int _xbotID, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Walkable Points:");
                for (int x = 0; x <= width; x++)
                {
                    for (int y = 0; y <= height; y++)
                    {
                        if (isWalkable(cells[x, y], _xbotID))
                        {
                            writer.WriteLine($"({x}, {y})");
                        }
                    }
                }
            }
            Console.WriteLine($"Walkable points saved to {filePath}");
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
                int[] newPos = { _node.x + direction.Item1, _node.y + direction.Item2 };

                if (newPos[0] >= 0 && newPos[0] <= width && newPos[1] >= 0 && newPos[1] <= height)
                {
                    neighbors.Add(cells[newPos[0], newPos[1]]);
                }
            }

            return neighbors;
        }

        public bool isWalkable(node _node, int _xbotID)
        {
            int x = _node.x;
            int y = _node.y;
            if (x >= 0 && y >= 0 && x <= width && y <= height && !_node.obstacle && !_node.occupied.Contains(_xbotID))
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

        public void makeUnWalkable(node _node, int _xbotID, bool _obstacle, bool _set) // Set node as unwalkable
        {
            if (_set)
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
            else // Set node as walkable
            {
                if (_obstacle)
                {
                    _node.obstacle = false;
                }
                else
                {
                    if (_node.occupied.Contains(_xbotID))
                        _node.occupied.Remove(_xbotID);
                }
            }
        }

        // Added clone method for simulating changes without affecting the original grid.
        public grid Clone(int _xbotSize)
        {
            grid newGrid = new grid(width + _xbotSize, height + _xbotSize, _xbotSize);
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
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
        foreach (var node in _grid.cells)
        {
            node.g = 0;
            node.h = 0;
            node.f = 0;
            node.parent = null;
        }
        
        node startNode = _grid.cells[_from[0], _from[1]];
        node endNode = _grid.cells[_to[0], _to[1]];

        if(startNode == endNode)
        {
            return (new List<node> { startNode }, 0);
        }

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
        //UnityEngine.Debug.LogError("No A* path found");
        return (null, 0);
    }
    #endregion
    #region Priority Planner
    public List<(int, List<node>, int)> priorityPlanner(grid _grid, List<(int, int[], int[])> _xBotID_From_To, int _xbotSize)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        int[,] priorityMatrix = getPriorityMatrix(_xBotID_From_To);
        
        //Console.WriteLine("Entries in priorityMatrix: " + priorityMatrix.GetLength(0));
        for (int i = 0; i < priorityMatrix.GetLength(0); i++)
        {
            //Console.WriteLine("Trying priority: " + (i + 1));
            SortedList<int, int> priorityList = new SortedList<int, int>();
            for (int j = 0; j < priorityMatrix.GetLength(1); j++)
            {
                int botID = _xBotID_From_To.Select(x => x.Item1).ToList()[j];
                int priority = priorityMatrix[i, j];
                priorityList.Add(priority, botID);
            }

            List<(int, List<node>, int)> newPathList = conflictHandler(_xBotID_From_To, _xbotSize, _grid, priorityList);

            if (newPathList != null && newPathList.Count > 0 && newPathList.All(path => path.Item2 != null && path.Item2.Count > 0))
            {
                // Check if every bot has a valid path
                bool allBotsHavePaths = _xBotID_From_To.All(bot => newPathList.Any(path => path.Item1 == bot.Item1 && path.Item2 != null && path.Item2.Count > 0));
                if (allBotsHavePaths)
                {
                    //Console.WriteLine("Priority " + (i + 1) + " took: " + stopwatch.ElapsedMilliseconds + " ms");
                    stopwatch.Stop();
                    //Console.WriteLine("Path found. Returning path list with priority: " + (i + 1));
                    // Print each XbotID's priority  
                    foreach (var priorityEntry in priorityList)
                    {
                        //Console.WriteLine($"XbotID: {priorityEntry.Value}, Priority: {priorityEntry.Key}");
                    }
                    //Console.WriteLine("Returning pathlist " + string.Join(", ", newPathList.Select(p => $"Bot {p.Item1}: {string.Join(" -> ", p.Item2.Select(n => $"({n.x}, {n.y})"))}")));
                    return newPathList;
                }
            }
            //Console.WriteLine("Priority " + (i + 1) + " took: " + stopwatch.ElapsedMilliseconds + " ms");
        }
        Console.WriteLine("Tried: " + priorityMatrix.GetLength(0) + " different priorities. Without Luck");
        return null;
    }
    #endregion
    #region PathPlanRunner
    public List<(int, List<double[]>)> pathPlanRunner(grid _grid, List<(int, double[], double[])> _xBotID_From_To, int _xbotSize)
    {
        Stopwatch stopwatche = new Stopwatch();
        stopwatche.Start();

        List<(int, int[], int[])> xBotID_From_To = unit_M_To_CM(_xBotID_From_To); // Convert to CM

        Console.WriteLine("Pathfinder received xBotID_From_To: " + string.Join(", ", xBotID_From_To.Select(p => $"Bot {p.Item1}: From ({p.Item2[0]}, {p.Item2[1]}) To ({p.Item3[0]}, {p.Item3[1]})")));
        List<(int, List<node>, int)> pathList = priorityPlanner(_grid, xBotID_From_To, _xbotSize);
        List<(int, List<(int, int)>)> _output = new();

        foreach (var path in pathList)
        {
            _output.Add((path.Item1, path.Item2.Select(x => (x.x, x.y)).ToList()));
        }
        //Console.WriteLine($"Pathfinding completed in: " + stopwatche.ElapsedMilliseconds + " ms");
        stopwatche.Stop();
        List<(int, List<double[]>)> output = unit_CM_To_M(_output); // Convert back to M
        return output;
    }

    public List<(int, int[], int[])> unit_M_To_CM(List<(int, double[], double[])> _xBotID_From_To)
    {

        List<(int, int[], int[])> xBotID_From_To = new();
        foreach (var botTuple in _xBotID_From_To)
        {
            /*int botID = botTuple.Item1;
            int[] from = { (int)Math.Floor(botTuple.Item2[0] * 100), (int)Math.Floor(botTuple.Item2[1] * 100) };
            int[] to = { (int)Math.Floor(botTuple.Item3[0] * 100), (int)Math.Floor(botTuple.Item3[1] * 100) };
            xBotID_From_To.Add((botID, from, to));
            */
            int botID = botTuple.Item1;
            int[] from = { (int)Math.Floor(botTuple.Item2[0] * 100-6), (int)Math.Floor(botTuple.Item2[1] * 100-6) };
            int[] to = { (int)Math.Floor(botTuple.Item3[0] * 100-6), (int)Math.Floor(botTuple.Item3[1] * 100-6) };
            xBotID_From_To.Add((botID, from, to));

        }
        return xBotID_From_To;
    }
    public List<(int, List<double[]>)> unit_CM_To_M(List<(int, List<(int, int)>)> output)
    {
        List<(int, List<double[]>)> xBotID_From_To = new();
        foreach (var botTuple in output)
        {
            int botID = botTuple.Item1;
            //List<double[]> path = botTuple.Item2.Select(x => new double[] { (double)x.Item1 / 100, (double)x.Item2 / 100 +0.06}).ToList();
            List<double[]> path = botTuple.Item2.Select(x => new double[] { ((double)x.Item1 / 100) + 0.06, ((double)x.Item2 / 100 + 0.06) }).ToList();
            xBotID_From_To.Add((botID, path));
        }
        return xBotID_From_To;
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
                if (xbotID == otherXbotID || xbotID > otherXbotID)
                    continue;

                List<node> xbotPath = new List<node>(_pathList.First(x => x.Item1 == xbotID).Item2);
                List<node> otherXbotPath = new List<node>(_pathList.First(x => x.Item1 == otherXbotID).Item2);

                int maxLength = Math.Max(xbotPath.Count, otherXbotPath.Count);

                // Pad the shorter path with the last node (simulate standing still)
                if (xbotPath.Count < maxLength)
                {
                    node lastNode = xbotPath.Last();
                    while (xbotPath.Count < maxLength)
                        xbotPath.Add(lastNode);
                }

                if (otherXbotPath.Count < maxLength)
                {
                    node lastNode = otherXbotPath.Last();
                    while (otherXbotPath.Count < maxLength)
                        otherXbotPath.Add(lastNode);
                }

                for (int timeStep = 0; timeStep < maxLength; timeStep++)
                {
                    if (Math.Abs(xbotPath[timeStep].x - otherXbotPath[timeStep].x) < _xbotSize &&
                        Math.Abs(xbotPath[timeStep].y - otherXbotPath[timeStep].y) < _xbotSize)
                    {
                        if (!conflicts.Any(c => (c.Item1 == xbotID && c.Item3 == otherXbotID)))
                        {
                            conflicts.Add((xbotID, xbotPath[timeStep], otherXbotID, otherXbotPath[timeStep]));
                        }
                    }
                }
            }
        }

        return conflicts;
    }

    #endregion
    #region Enhanced Conflict Handler
    public List<(int, List<node>, int)> conflictHandler(
        List<(int, int[], int[])> _xBotID_From_To,
        int _xbotSize,
        grid _grid,
        SortedList<int, int> priorityList)
    {
        int botCount = _xBotID_From_To.Count;
        int iteration = 0;
        long iterationMax = 150;
        const int MAX_WAIT = 20;
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
                if (newPath != null)
                {
                    List<node> fullPath = waitingPath.Concat(newPath).ToList();
                    int currentWaitTime = botWaitTime.ContainsKey(botID) ? botWaitTime[botID] : 0;
                    int totalCost = cost + (WAIT_PENALTY);
                    botPaths[botID] = fullPath;
                    pathList.Add((botID, fullPath, totalCost));
                }
                else
                {
                    Console.WriteLine("No path found for bot " + botID);
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

                var botInfo = _xBotID_From_To.First(x => x.Item1 == lowerPriorityBot);
                int[] from = { botInfo.Item2[0], botInfo.Item2[1] };
                int[] to = { botInfo.Item3[0], botInfo.Item3[1] };

                var currentPathEntry = pathList.First(x => x.Item1 == lowerPriorityBot);
                int currentCost = currentPathEntry.Item3;

                // --- Simulate walk-around ---
                grid walkClone = _grid.Clone(_xbotSize); // SHALLOW CLONE ALERT
                walkClone.makeUnWalkable(conflictNode, lowerPriorityBot, false, true); // Temp unwalkable
                (List<node> simWalkPath, int simWalkCost) = aStar(from, to, walkClone, lowerPriorityBot);
                walkClone.makeUnWalkable(conflictNode, lowerPriorityBot, false, false); // Restore walkable
                if (simWalkPath == null || simWalkCost == 0)
                {
                    simWalkCost = int.MaxValue;
                }

                int currentWaitTimeForBot = botWaitTime.ContainsKey(lowerPriorityBot) ? botWaitTime[lowerPriorityBot] : 0;
                int waitOptionCost = currentCost + (WAIT_PENALTY);

                bool choosesToWait = currentWaitTimeForBot < MAX_WAIT && waitOptionCost <= simWalkCost;

                if (choosesToWait)
                {
                    if (!botWaitTime.ContainsKey(lowerPriorityBot))
                        botWaitTime[lowerPriorityBot] = 0;
                    botWaitTime[lowerPriorityBot]++;

                    node startNode = currentPathEntry.Item2.First();
                    if (!botWaitingPaths.ContainsKey(lowerPriorityBot))
                        botWaitingPaths[lowerPriorityBot] = new List<node>();
                    botWaitingPaths[lowerPriorityBot].Add(startNode);

                    //Console.WriteLine($"🕒 Bot {lowerPriorityBot} chooses to **WAIT**. WaitTime: {botWaitTime[lowerPriorityBot]}, WaitCost: {waitOptionCost}, WalkCost: {simWalkCost}");
                    _grid.removeOccupied();
                }
                else
                {
                    // Replan on actual grid and only mark unwalkable if successful
                    (List<node> finalPath, int _) = aStar(from, to, _grid, lowerPriorityBot);
                    if (finalPath != null && finalPath.Count > 0)
                    {
                        botWaitingPaths[lowerPriorityBot] = new List<node>(); // Clear waiting steps
                        _grid.makeUnWalkable(conflictNode, lowerPriorityBot, false, true);
                        //Console.WriteLine($"🚶 Bot {lowerPriorityBot} chooses to **WALK AROUND** the conflict at ({conflictNode.x}, {conflictNode.y}). WalkCost: {simWalkCost}, WaitCost: {waitOptionCost}");
                    }
                    else
                    {
                        //Console.WriteLine($"⚠️ Bot {lowerPriorityBot} attempted walk-around but no valid path found in main grid.");
                    }
                }
            }


            iteration++;
        }

        //Console.WriteLine("Iteration cap hit. No conflict solution found");
        return null;
    }
    #endregion
    #region Distance Calculator
    public double getDistance(node _nodeA, node _nodeB, bool euclidean)
    {
        if (euclidean)
        {
            double dstX = Math.Abs(_nodeA.x - _nodeB.x);
            double dstY = Math.Abs(_nodeA.y - _nodeB.y);
            return Math.Sqrt(dstX * dstX + dstY * dstY);
        }
        else
        {
            int dstX = Math.Abs(_nodeA.x - _nodeB.x);
            int dstY = Math.Abs(_nodeA.y - _nodeB.y);
            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
    public int[,] getPriorityMatrix(List<(int, int[], int[])> _xBotID_From_To)
    {
        var xBots = _xBotID_From_To.Select(x => x.Item1).ToList();
        int botCount = xBots.Count;

        // Generate unique priority values (1 to botCount)
        List<int> priorities = Enumerable.Range(1, botCount).ToList();

        // Get all permutations of the priorities
        var permutations = GetPermutations(priorities.ToArray())
            .Select(p => p.ToList())
            .ToList();

        // Preserve the first permutation as-is
        var firstPermutation = permutations[0];
        var remainingPermutations = permutations.Skip(1).ToList();

        // Shuffle the remaining permutations
        var rng = new System.Random();
        remainingPermutations = remainingPermutations.OrderBy(_ => rng.Next()).ToList();

        // Combine the first permutation with the shuffled remaining permutations
        permutations = new List<List<int>> { firstPermutation }.Concat(remainingPermutations).ToList();

        // Convert the permutations to a 2D array (priority matrix)
        int[,] priorityMatrix = new int[permutations.Count, botCount];
        for (int i = 0; i < permutations.Count; i++)
        {
            for (int j = 0; j < botCount; j++)
            {
                priorityMatrix[i, j] = permutations[i][j];
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
