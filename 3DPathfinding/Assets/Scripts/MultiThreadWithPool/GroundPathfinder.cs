using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Node = MultiThread.Node;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Profiling;
using UnityEditor.Experimental.GraphView;

namespace MultiThreadWithPool
{
    public class GroundPathfinder : MonoBehaviour
    {
        private GridComponent _gridComponent;
        private const int maxSize = 1000;

        public enum HeuristicType { Euclidean, Manhattan, Chebyshev, Octile }

        [SerializeField] private int _awaitDuration;
        [SerializeField] private HeuristicType _heuristic;
        [SerializeField] private int _initialPoolSize = 1000;
        [SerializeField] private int _maxPoolSize = 1500;


        [SerializeField] int _currentRunningThread = 0;

        private NodePool _nodePool;
        //private readonly object _pathFindLock = new object();

        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawCube(_startNodePos, Vector3.one * 0.5f);
            //Gizmos.DrawCube(_endNodePos, Vector3.one * 0.5f);

            for (int i = 1; i < _openListPoints.Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(_openListPoints[i], Vector3.one * 0.7f);
            }

            for (int i = 1; i < _closedListPoints.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(_closedListPoints[i], Vector3.one * 0.7f);
            }
        }

        List<Vector3> _openListPoints = new List<Vector3>();
        List<Vector3> _closedListPoints = new List<Vector3>();


        public void Initialize(GridComponent gridComponent)
        {
            _gridComponent = gridComponent;
            _nodePool = new NodePool(_initialPoolSize, _maxPoolSize);

            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(13, 13);
        }

        List<PathfindingRequest> pathfindingRequests = new List<PathfindingRequest>();

        public void FindPath(PathfindingRequest request)
        {
            pathfindingRequests.Add(request);

            ThreadPool.QueueUserWorkItem(state =>
            {
                if(_currentRunningThread >= 1)
                {
                    Debug.Log("Not Finished!");
                }

                _currentRunningThread++;

                List<Vector3> path = FindPathInternal(request.startPoint, request.endPoint);
                request.OnCompleted?.Invoke(new PathfindingResult(path));

                Debug.Log(_nodePool.TotalCount);

                _currentRunningThread--;
            });
        }

        const int _maxLoop = 1500;

        private List<Vector3> FindPathInternal(Vector3 startPos, Vector3 targetPos)
        {
            _openListPoints.Clear();
            _closedListPoints.Clear();

            Heap<Node> openList = new Heap<Node>(maxSize);
            HashSet<Vector3Int> openListHash = new HashSet<Vector3Int>();
            HashSet<Vector3Int> closedListHash = new HashSet<Vector3Int>();

            List<Node> tmpPoolNodes = new List<Node>();

            Vector3Int startIndex = _gridComponent.ReturnNodeIndex(startPos);
            Vector3Int endIndex = _gridComponent.ReturnNodeIndex(targetPos);

            Node startNode = _gridComponent.GetNode(startIndex);
            Node endNode = _gridComponent.GetNode(endIndex);

            if (startNode == null || endNode == null) return null;

            Node pooledStartNode = _nodePool.Get();
            Node pooledEndNode = _nodePool.Get();

            tmpPoolNodes.Add(pooledStartNode);
            tmpPoolNodes.Add(pooledEndNode);

            pooledStartNode.Copy(startNode);
            pooledEndNode.Copy(endNode);

            openList.Insert(pooledStartNode);
            _openListPoints.Add(pooledStartNode.Pos);

            int maxIterations = _maxLoop; // 예시: 최대 반복 횟수 설정
            int iterationCount = 0;
            while (openList.Count > 0)
            {
                if (iterationCount > maxIterations) break;

                iterationCount++;
                Node currentNode = openList.ReturnMin();
                openList.DeleteMin();
                openListHash.Remove(currentNode.Index);

                if (currentNode.Index == pooledEndNode.Index)
                {
                    List<Vector3> path = ConvertNodeToV3(currentNode, pooledStartNode);
                    ReleaseNodes(tmpPoolNodes);

                    return path;
                }

                closedListHash.Add(currentNode.Index);
                _closedListPoints.Add(currentNode.Pos);

                tmpPoolNodes.Add(currentNode);
                AddNearGridInList(openList, closedListHash, openListHash, tmpPoolNodes, currentNode, pooledEndNode.SurfacePos);
            }

            if (iterationCount >= maxIterations)
            {
                Debug.LogError("Potential infinite loop detected in pathfinding.");
                ReleaseNodes(tmpPoolNodes);
                return null;
            }
            else
            {
                ReleaseNodes(tmpPoolNodes);
                return null;
            }
        }

        private void ReleaseNodes(List<Node> tmpPoolNodes)
        {
            for (int i = 0; i < tmpPoolNodes.Count; i++)
            {
                _nodePool.Release(tmpPoolNodes[i]);
            }
        }

        private List<Vector3> ConvertNodeToV3(Node targetNode, Node startNode)
        {
            int maxIterations = _maxLoop; // 예시: 최대 반복 횟수 설정
            int iterationCount = 0;

            List<Vector3> path = new List<Vector3>();
            while (targetNode.Index != startNode.Index)
            {
                if (iterationCount > maxIterations) break;

                path.Add(targetNode.SurfacePos);
                targetNode = targetNode.ParentNode;
            }
            path.Reverse();


            if (iterationCount >= maxIterations)
            {
                Debug.LogError("Potential infinite loop detected in pathfinding.");
                return null;
            }
            else
            {
                return path;
            }
        }

        private void AddNearGridInList(Heap<Node> openList, HashSet<Vector3Int> closedListHash, HashSet<Vector3Int> openListHash, List<Node> tmpPoolNodes, Node currentNode, Vector3 endPos)
        {
            foreach (Vector3Int nearNodeIndex in currentNode.NearNodeIndexes)
            {
                //if (openListHash.Contains(nearNodeIndex)) continue;

                Node neighborNode = _gridComponent.GetNode(nearNodeIndex);
                if (neighborNode.CanStep == false) continue;

                if (closedListHash.Contains(neighborNode.Index)) continue;

                Node pooledNeighborNode = _nodePool.Get();
                if (pooledNeighborNode == null) continue;

                pooledNeighborNode.Copy(neighborNode);

                float moveCost = currentNode.G + GetHeuristic(currentNode.SurfacePos, pooledNeighborNode.SurfacePos);
                if (moveCost < pooledNeighborNode.G || !openListHash.Contains(nearNodeIndex))
                {
                    pooledNeighborNode.G = moveCost;
                    pooledNeighborNode.H = GetHeuristic(pooledNeighborNode.SurfacePos, endPos);
                    pooledNeighborNode.ParentNode = currentNode;

                    openList.Insert(pooledNeighborNode);
                    openListHash.Add(nearNodeIndex);
                    _openListPoints.Add(pooledNeighborNode.Pos);
                }
                else
                {
                    _nodePool.Release(pooledNeighborNode); // 조건에 맞지 않으면 즉시 릴리즈
                }
            }
        }

        private float GetHeuristic(Vector3 a, Vector3 b)
        {
            return _heuristic switch
            {
                HeuristicType.Euclidean => Vector3.Distance(a, b),
                HeuristicType.Manhattan => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z),
                HeuristicType.Chebyshev => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z)),
                HeuristicType.Octile => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z)) + (1.414f - 1) * Mathf.Min(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)),
                _ => 0f
            };
        }
    }
}