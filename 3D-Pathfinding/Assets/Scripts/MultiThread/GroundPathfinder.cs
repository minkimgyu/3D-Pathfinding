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

            ThreadPool.SetMinThreads(1, 1); //�ּ� ������ ����
            ThreadPool.SetMaxThreads(13, 13); //�ִ� ������ ����
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
                // Profiler.BeginThreadProfiling("ThreadPool", "pathfinding"); // �۾� ť ��� ����

                lock (pathFindLock)
                {
                    List<Vector3> path = FindPathInternal(request.startPoint, request.endPoint);
                    request.OnCompleted?.Invoke(new PathfindingResult(path));
                }

                // Profiler.EndThreadProfiling(); // �۾� ���� ����
            });
        }


        public List<Vector3> FindPathInternal(Vector3 startPos, Vector3 targetPos)
        {
            Heap<Node> openList = new Heap<Node>(maxSize);
            HashSet<Node> closedList = new HashSet<Node>();

            //// ����Ʈ �ʱ�ȭ
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
                if (targetNode == endNode) // �������� Ÿ���� ������ ��
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

                openList.DeleteMin(); // �ش� �׸��� ������
                closedList.Add(targetNode); // �ش� �׸��� �߰�����
                //_closedListPoints.Add(targetNode.SurfacePos);

                AddNearGridInList(openList, closedList, targetNode, endNode.SurfacePos);
            }

            // �� ���� ��θ� ã�� ���� ��Ȳ��
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

        private const float D = 1f;        // ����/���� �̵� ���
        private const float D2 = 1.414f;   // 2D �밢�� �̵� ��� (��2)
        private const float D3 = 1.732f;   // 3D �밢�� �̵� ��� (��3)

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
                if (nearNode.CanStep == false || closedList.Contains(nearNode)) continue; // �������� �ʰų� ���� ����Ʈ�� �ִ� ��� ���� �׸��� Ž�� --> Ground�� ��� �����־�� Ž�� ������

                // ���߿� �ִ� ���� Pos, ���� �ִ� ���� SurfacePos�� ó���Ѵ�.
                float moveCost = GetHeuristic(targetNode.SurfacePos, nearNode.SurfacePos);
                // �� �κ� �߿�! --> �Ÿ��� �����ؼ� ������Ʈ ���� �ʰ� ��� �����ִ� ������� �����ؾ���
                moveCost += targetNode.G;

                bool isOpenListContainNearGrid = openList.Contain(nearNode);

                // ���� ����Ʈ�� �ִ��� G ���� ����ȴٸ� �ٽ� �������ֱ�
                if (isOpenListContainNearGrid == false || moveCost < nearNode.G)
                {
                    // ���⼭ grid �� �Ҵ� �ʿ�
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