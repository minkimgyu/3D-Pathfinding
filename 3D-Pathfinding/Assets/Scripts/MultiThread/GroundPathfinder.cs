using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Node = MultiThread.Node;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Profiling;

namespace MultiThread
{
    public class GroundPathfinder : MonoBehaviour
    {
        GridComponent _gridComponent;
        const int maxSize = 1000;

        public enum HeuristicType
        {
            Euclidean,
            Manhattan,
            Chebyshev,
            Octile
        }

        [SerializeField] int _awaitDuration;
        [SerializeField] HeuristicType _heuristic;

        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawCube(_startNodePos, Vector3.one * 0.5f);
        //    Gizmos.DrawCube(_endNodePos, Vector3.one * 0.5f);

        //    for (int i = 1; i < _openListPoints.Count; i++)
        //    {
        //        Gizmos.color = Color.cyan;
        //        Gizmos.DrawCube(_openListPoints[i], Vector3.one * 0.7f);
        //    }

        //    for (int i = 1; i < _closedListPoints.Count; i++)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawCube(_closedListPoints[i], Vector3.one * 0.7f);
        //    }
        //}

        public void Initialize(GridComponent gridComponent)
        {
            _gridComponent = gridComponent;

            ThreadPool.SetMinThreads(1, 1); //최소 스레드 개수
            ThreadPool.SetMaxThreads(13, 13); //최대 스레드 개수
        }

        List<Vector3> ConvertNodeToV3(Stack<Node> stackNode)
        {
            List<Vector3> points = new List<Vector3>();
            while (stackNode.Count > 0)
            {
                Node node = stackNode.Peek();
                points.Add(node.SurfacePos);
                stackNode.Pop();
            }

            return points;
        }

        //List<Vector3> _openListPoints = new List<Vector3>();
        //List<Vector3> _closedListPoints = new List<Vector3>();

        object pathFindLock = new object();

        public void FindPath(PathfindingRequest request)
        {

            ThreadPool.QueueUserWorkItem(state =>
            {
                // Profiler.BeginThreadProfiling("ThreadPool", "pathfinding"); // 작업 큐 대기 시작

                lock (pathFindLock)
                {
                    List<Vector3> path = FindPathInternal(request.startPoint, request.endPoint);
                    request.OnCompleted?.Invoke(new PathfindingResult(path));
                }

                // Profiler.EndThreadProfiling(); // 작업 실행 종료
            });
        }


        public List<Vector3> FindPathInternal(Vector3 startPos, Vector3 targetPos)
        {
            Heap<Node> openList = new Heap<Node>(maxSize);
            HashSet<Node> closedList = new HashSet<Node>();

            //// 리스트 초기화
            openList.Clear();
            closedList.Clear();

            //_openListPoints.Clear();
            //_closedListPoints.Clear();

            Vector3Int startIndex = _gridComponent.ReturnNodeIndex(startPos);
            Vector3Int endIndex = _gridComponent.ReturnNodeIndex(targetPos);

            Node startNode = _gridComponent.ReturnNode(startIndex);
            Node endNode = _gridComponent.ReturnNode(endIndex);

            if (startNode == null || endNode == null) { return null; }

            Vector3 startNodePos = startNode.SurfacePos;
            Vector3 endNodePos = endNode.SurfacePos;

            openList.Insert(startNode);
            //_openListPoints.Add(startNode.SurfacePos);

            while (openList.Count > 0)
            {
                Node targetNode = openList.ReturnMin();
                if (targetNode == endNode) // 목적지와 타겟이 같으면 끝
                {
                    Stack<Node> finalList = new Stack<Node>();

                    Node TargetCurNode = targetNode;
                    while (TargetCurNode != startNode)
                    {
                        finalList.Push(TargetCurNode);
                        TargetCurNode = TargetCurNode.ParentNode;
                    }
                    //finalList.Push(startNode);

                    return ConvertNodeToV3(finalList);
                }

                openList.DeleteMin(); // 해당 그리드 지워줌
                closedList.Add(targetNode); // 해당 그리드 추가해줌
                //_closedListPoints.Add(targetNode.SurfacePos);

                AddNearGridInList(openList, closedList, targetNode, endNode.SurfacePos);
            }

            // 이 경우는 경로를 찾지 못한 상황임
            return null;
        }

        float GetHeuristic(Vector3 a, Vector3 b)
        {
            switch (_heuristic)
            {
                case HeuristicType.Euclidean:
                    return EuclideanHeuristic(a, b);
                case HeuristicType.Manhattan:
                    return ManhattanHeuristic(a, b);
                case HeuristicType.Chebyshev:
                    return ChebyshevHeuristic(a, b);
                case HeuristicType.Octile:
                    return OctileHeuristic(a, b);
            }

            return 0;
        }

        float EuclideanHeuristic(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        //static readonly float SQRT_2_MINUS_1 = Mathf.Sqrt(2) - 1.0f;

        private const float D = 1f;        // 수직/수평 이동 비용
        private const float D2 = 1.414f;   // 2D 대각선 이동 비용 (√2)
        private const float D3 = 1.732f;   // 3D 대각선 이동 비용 (√3)

        float OctileHeuristic(Vector3 a, Vector3 b)
        {
            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);
            float dz = Mathf.Abs(a.z - b.z);

            float minXYZ = Mathf.Min(dx, dy, dz);
            float maxXYZ = Mathf.Max(dx, dy, dz);
            float midXYZ = dx + dy + dz - minXYZ - maxXYZ;

            return D * (dx + dy + dz) + (D2 - 2 * D) * minXYZ + (D3 - 3 * D) * midXYZ;
        }

        float ChebyshevHeuristic(Vector3 a, Vector3 b)
        {
            float x_dist = Mathf.Abs(a.x - b.x);
            float y_dist = Mathf.Abs(a.y - b.y);
            float z_dist = Mathf.Abs(a.z - b.z);

            return Mathf.Max(x_dist, y_dist, z_dist);
        }

        float ManhattanHeuristic(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }

        void AddNearGridInList(Heap<Node> openList, HashSet<Node> closedList, Node targetNode, Vector3 endNodePos)
        {
            Vector3Int startIndex = targetNode.Index;
            List<Node> nearNodes = targetNode.NearNodesInGround;// _gridComponent.ReturnNearNodesInGround(startIndex);

            for (int i = 0; i < targetNode.NearNodesInGround.Count; i++)
            {
                Node nearNode = targetNode.NearNodesInGround[i];
                if (nearNode.CanStep == false || closedList.Contains(nearNode)) continue; // 막혀있지 않거나 닫힌 리스트에 있는 경우 다음 그리드 탐색 --> Ground의 경우 막혀있어야 탐색 가능함

                // 공중에 있는 경우는 Pos, 땅에 있는 경우는 SurfacePos로 처리한다.
                float moveCost = GetHeuristic(targetNode.SurfacePos, nearNode.SurfacePos);
                // 이 부분 중요! --> 거리를 측정해서 업데이트 하지 않고 계속 더해주는 방식으로 진행해야함
                moveCost += targetNode.G;

                bool isOpenListContainNearGrid = openList.Contain(nearNode);

                // 오픈 리스트에 있더라도 G 값이 변경된다면 다시 리셋해주기
                if (isOpenListContainNearGrid == false || moveCost < nearNode.G)
                {
                    // 여기서 grid 값 할당 필요
                    nearNode.G = moveCost;
                    nearNode.H = GetHeuristic(nearNode.SurfacePos, endNodePos);
                    nearNode.ParentNode = targetNode;
                }

                if (isOpenListContainNearGrid == false)
                {
                    openList.Insert(nearNode);
                    //_openListPoints.Add(nearNode.Pos);
                }
            }
        }
    }
}