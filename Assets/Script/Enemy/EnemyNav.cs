using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class EnemyNav : MonoBehaviour
{
    [SerializeField] float repathInterval = 0.5f; // 路径更新间隔
    [SerializeField] float arrivalThreshold = 0.1f; // 到达判定阈值

    private List<Vector2> currentPath = new List<Vector2>();
    private Coroutine moveCoroutine;
    private Coroutine pathCoroutine;
    private int currentPathIndex;
    private float lastRepathTime;

    [SerializeField] LayerMask obstacleLayer; // 在Inspector中设置障碍物所在图层
    [SerializeField] float obstacleCheckRadius = 1f; // 障碍物检测半径
    public GameObject target;
    // 将 map_points 从 List 改为 HashSet 提升 Contains 检查效率
    [SerializeField] HashSet<Vector2> map_points = new HashSet<Vector2>();
    List<Vector2> all_points = new List<Vector2>();
    [SerializeField] float max_nav_distance = 20f;
    Vector2 lastUpdatePosition;
    const float UPDATE_THRESHOLD = 1f;
    const int GRID_SIZE = 1;

    Enemy self_controller;
    Dictionary<Vector2Int, List<Vector2>> spatialGrid = new Dictionary<Vector2Int, List<Vector2>>();

    public bool is_activing { get; private set; }
    bool initialized;
    public void SetNavActive(bool value) => is_activing = value;
    public void SetTarget(GameObject t) => target = t;

    // 分批加载的参数
    [SerializeField] int initBatchSize = 1000; // 每批初始化的点数量
    [SerializeField] int gridBatchSize = 500; // 每批处理的网格点数量
    bool pointsInitialized;
    bool gridInitialized;

    void Start()
    {
        self_controller = GetComponent<Enemy>();
        lastUpdatePosition = transform.position;
        is_activing = false;
        pointsInitialized = false;
        gridInitialized = false;
        initialized = false;
        StartCoroutine(InitializeNavigationAsync());
    }

    // 分批异步初始化导航系统
    IEnumerator InitializeNavigationAsync()
    {
        // 等待ChunkGenerator准备就绪
        while (ChunkGenerator.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 分批初始化导航点
        yield return StartCoroutine(InitializePointsAsync());
        
        // 分批初始化空间网格
        yield return StartCoroutine(InitializeSpatialGridAsync());
        
        // 初始化导航范围
        UpdateNavigationRange();
        
        // 标记初始化完成
        initialized = true;
        Debug.Log("Enemy navigation initialized");
    }

    // 分批异步加载导航点
    IEnumerator InitializePointsAsync()
    {
        ChunkGenerator generator = ChunkGenerator.Instance;
        if (generator != null)
        {
            var tileDict = generator.GetTileDictionary();
            
            // 先获取字典键总数
            var keys = tileDict.Keys.ToList();
            int totalPoints = keys.Count;
            
            // 分批处理
            all_points = new List<Vector2>(totalPoints);
            for (int i = 0; i < totalPoints; i += initBatchSize)
            {
                int batchEnd = Mathf.Min(i + initBatchSize, totalPoints);
                for (int j = i; j < batchEnd; j++)
                {
                    var v = keys[j];
                    all_points.Add(new Vector2(v.x, v.y));
                }
                
                // 每批处理后等待一帧
                yield return null;
            }
            
            pointsInitialized = true;
        }
        else
        {
            Debug.LogWarning("ChunkGenerator is null, waiting for it to be available");
            pointsInitialized = false;
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(InitializePointsAsync());
        }
    }

// 分批异步构建空间网格
    IEnumerator InitializeSpatialGridAsync()
    {
        // 等待导航点初始化完成
        while (!pointsInitialized)
        {
            yield return null;
        }
        
        spatialGrid.Clear();
        int count = 0;
        
        foreach (var point in all_points)
        {
            Vector2Int gridKey = new Vector2Int(
                Mathf.FloorToInt(point.x / GRID_SIZE),
                Mathf.FloorToInt(point.y / GRID_SIZE)
            );
            
            if (!spatialGrid.ContainsKey(gridKey))
                spatialGrid[gridKey] = new List<Vector2>();
                
            spatialGrid[gridKey].Add(point);
            
            // 每处理一定数量的点就让出主线程
            if (++count % gridBatchSize == 0)
            {
                yield return null;
            }
        }
        
        gridInitialized = true;
    }   
    void Update()
    {
        if (!initialized)
        {
            return;
        }
        else
        {
            if (Vector2.Distance(lastUpdatePosition, transform.position) > UPDATE_THRESHOLD)
            {
                UpdateNavigationRange();
                lastUpdatePosition = transform.position;
            }

            if (target != null && is_activing)
            {
                // 加入随机偏移，避免所有敌人同一帧执行寻路
                float randomDelay = UnityEngine.Random.Range(0f, 0.2f);
                if (Time.time - lastRepathTime >= repathInterval + randomDelay)
                {
                    lastRepathTime = Time.time;
                    Vector2 startPos = transform.position;
                    Vector2 targetPos = target.transform.position;

                    // 停止之前正在进行的路径计算和移动协程
                    if (pathCoroutine != null)
                    {
                        StopCoroutine(pathCoroutine);
                        pathCoroutine = null;
                    }
                    if (moveCoroutine != null)
                    {
                        StopCoroutine(moveCoroutine);
                        moveCoroutine = null;
                    }

                    // 开始寻路协程
                    pathCoroutine = StartCoroutine(FindPathCoroutine(startPos, targetPos, (path) =>
                    {
                        currentPath = path;
                        currentPathIndex = 0;
                        if (currentPath != null && currentPath.Count > 0)
                        {
                            moveCoroutine = StartCoroutine(FollowPath());
                        }
                    }));
                }
            }
            else
            {
                if (moveCoroutine != null)
                {
                    StopCoroutine(moveCoroutine);
                    moveCoroutine = null;
                }
            }
        }

    }

    IEnumerator FollowPath()
    {
        while (currentPathIndex < currentPath.Count)
        {
            Vector2 targetPoint = currentPath[currentPathIndex];
            while (Vector2.Distance(transform.position, targetPoint) > arrivalThreshold)
            {
                // 更新X和Y坐标
                Vector2 newPosition = Vector2.MoveTowards(
                    transform.position,
                    targetPoint,
                    self_controller.speed * Time.deltaTime
                );

                // 保持Z轴不变
                transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);

                yield return null;
            }
            currentPathIndex++;
        }
    }

    void UpdateNavigationRange()
    {
        if (!gridInitialized) return;
        
        Vector2 currentPos = transform.position;
        int searchRadius = Mathf.CeilToInt(max_nav_distance / GRID_SIZE);
        Vector2Int centerGrid = GetGridKey(currentPos);

        map_points.Clear();
        
        // 只检查实际需要的网格
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                // 跳过明显超出范围的网格
                if (x*x + y*y > searchRadius*searchRadius)
                    continue;
                    
                CheckGrid(centerGrid + new Vector2Int(x, y), currentPos);
            }
        }
    }

    Vector2Int GetGridKey(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / GRID_SIZE),
            Mathf.FloorToInt(position.y / GRID_SIZE)
        );
    }

    void CheckGrid(Vector2Int gridKey, Vector2 currentPos)
    {
        if (spatialGrid.TryGetValue(gridKey, out var points))
        {
            foreach (var pt in points)
            {
                if (pt.IsInRange(currentPos, max_nav_distance))
                {
                    map_points.Add(pt);
                }
            }
        }
    }

    // 异步分帧 A* 路径查找协程
    IEnumerator FindPathCoroutine(Vector2 start, Vector2 originalTarget, Action<List<Vector2>> callback)
    {
        Vector2 startSnapped = SnapToGrid(start);
        Vector2 targetSnapped = SnapToGrid(originalTarget);
        Vector2? actualTarget = GetNearestValidTarget(targetSnapped);
        if (!actualTarget.HasValue)
        {
            callback(null);
            yield break;
        }

        Dictionary<Vector2, Vector2> parentDict = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, float> gCostDict = new Dictionary<Vector2, float>();
        Dictionary<Vector2, float> hCostDict = new Dictionary<Vector2, float>();

        BinaryHeapPriorityQueue<Vector2> openList = new BinaryHeapPriorityQueue<Vector2>();
        HashSet<Vector2> closedSet = new HashSet<Vector2>();

        gCostDict[startSnapped] = 0;
        hCostDict[startSnapped] = Vector2.Distance(startSnapped, actualTarget.Value);
        openList.Enqueue(startSnapped, gCostDict[startSnapped] + hCostDict[startSnapped]);

        int iterations = 0;
        while (openList.Count > 0)
        {
            Vector2 currentPos = openList.Dequeue();

            if (Vector2.Distance(currentPos, actualTarget.Value) < 0.1f)
            {
                List<Vector2> path = RetracePath(parentDict, currentPos);
                callback(path);
                yield break;
            }

            closedSet.Add(currentPos);

            foreach (var neighbor in GetNeighbors(currentPos))
            {
                Vector2 neighborSnapped = SnapToGrid(neighbor);

                // 检查邻居节点是否在导航网格内
                if (!map_points.Contains(neighborSnapped))
                    continue;

                // 检查邻居节点是否已在关闭列表中
                if (closedSet.Contains(neighborSnapped))
                    continue;

                // 检查邻居节点是否有障碍
                if (HasObstacle(neighborSnapped))
                {
                    closedSet.Add(neighborSnapped);
                    continue;
                }

                float tentativeGCost = gCostDict[currentPos] + Vector2.Distance(currentPos, neighborSnapped);
                if (tentativeGCost < gCostDict.GetValueOrDefault(neighborSnapped, float.MaxValue))
                {
                    parentDict[neighborSnapped] = currentPos;
                    gCostDict[neighborSnapped] = tentativeGCost;
                    hCostDict[neighborSnapped] = Vector2.Distance(neighborSnapped, actualTarget.Value);
                    float fCost = tentativeGCost + hCostDict[neighborSnapped];

                    if (openList.Contains(neighborSnapped))
                        openList.UpdatePriority(neighborSnapped, fCost);
                    else
                        openList.Enqueue(neighborSnapped, fCost);
                }
            }

            iterations++;
            // 每 100 次迭代后 yield 一帧，避免长时间占用主线程
            if (iterations % 100 == 0)
                yield return null;
        }
        // 若未找到路径
        callback(null);
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        return new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
    }

    private bool HasObstacle(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, obstacleCheckRadius, obstacleLayer);
        if (hit != null && !IsSelfOrChildCollider(hit))
        {
            return true;
        }
        return false;
    }

    private bool IsSelfOrChildCollider(Collider2D collider)
    {
        GameObject obj = collider.gameObject;
        return obj == this.gameObject || obj.transform.IsChildOf(this.transform);
    }

    // 查找离目标点最近且无障碍的有效导航点
    Vector2? GetNearestValidTarget(Vector2 originalTarget)
    {
        // 先判断原点是否有效
        foreach (var pt in map_points)
        {
            if (Vector2.Distance(pt, originalTarget) < 0.1f && !HasObstacle(pt))
            {
                return originalTarget;
            }
        }
        Vector2? nearest = null;
        float minDist = float.MaxValue;
        foreach (var pt in map_points)
        {
            if (HasObstacle(pt)) continue;
            float dist = Vector2.Distance(pt, originalTarget);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = pt;
            }
        }
        return nearest;
    }

    List<Vector2> RetracePath(Dictionary<Vector2, Vector2> parentDict, Vector2 endPos)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 current = endPos;
        while (parentDict.ContainsKey(current))
        {
            path.Add(current);
            current = parentDict[current];
        }
        path.Reverse();
        return path;
    }

    // 支持8方向（包括斜向）的邻居节点
    List<Vector2> GetNeighbors(Vector2 pos)
    {
        return new List<Vector2>
        {
            pos + Vector2.right,
            pos + Vector2.left,
            pos + Vector2.up,
            pos + Vector2.down,
            pos + new Vector2(1, 1),
            pos + new Vector2(-1, 1),
            pos + new Vector2(1, -1),
            pos + new Vector2(-1, -1)
        };
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (currentPath == null || currentPath.Count < 2)
            return;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 start = new Vector3(currentPath[i].x, currentPath[i].y, 0);
            Vector3 end = new Vector3(currentPath[i + 1].x, currentPath[i + 1].y, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}

public static class NavigationExtensions
{
    public static bool IsInRange(this Vector2 point, Vector2 center, float radius)
    {
        return (point - center).sqrMagnitude <= radius * radius;
    }
}

// 使用二叉堆实现的优先队列（适用于大数据量 A* 寻路）
public class BinaryHeapPriorityQueue<T>
{
    private List<KeyValuePair<T, float>> heap = new List<KeyValuePair<T, float>>();
    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add(new KeyValuePair<T, float>(item, priority));
        HeapifyUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        T bestItem = heap[0].Key;
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        HeapifyDown(0);
        return bestItem;
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < heap.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(heap[i].Key, item))
                return true;
        }
        return false;
    }

    public void UpdatePriority(T item, float newPriority)
    {
        for (int i = 0; i < heap.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(heap[i].Key, item))
            {
                float oldPriority = heap[i].Value;
                heap[i] = new KeyValuePair<T, float>(item, newPriority);
                if (newPriority < oldPriority)
                    HeapifyUp(i);
                else
                    HeapifyDown(i);
                break;
            }
        }
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[index].Value < heap[parent].Value)
            {
                var temp = heap[index];
                heap[index] = heap[parent];
                heap[parent] = temp;
                index = parent;
            }
            else break;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = heap.Count - 1;
        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;

            if (left <= lastIndex && heap[left].Value < heap[smallest].Value)
                smallest = left;
            if (right <= lastIndex && heap[right].Value < heap[smallest].Value)
                smallest = right;

            if (smallest != index)
            {
                var temp = heap[index];
                heap[index] = heap[smallest];
                heap[smallest] = temp;
                index = smallest;
            }
            else break;
        }
    }
}
