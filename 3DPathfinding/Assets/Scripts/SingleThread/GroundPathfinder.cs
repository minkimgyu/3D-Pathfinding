using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Node = SingleThread.Node;
using System.Threading.Tasks;

namespace SingleThread
{
    public class GroundPathfinder : MonoBehaviour
    {
        GridComponent _gridComponent;
        const int maxSize = 1000;

        Heap<Node> _openList = new Heap<Node>(maxSize);
        // List<Node> _openList = new List<Node>(maxSize);
        HashSet<Node> _closedList = new HashSet<Node>();

        Vector3 _startNodePos;
        Vector3 _endNodePos;

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
        //    //Gizmos.DrawCube(_startNodePos, Vector3.one * 0.5f);
        //    //Gizmos.DrawCube(_endNodePos, Vector3.one * 0.5f);

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

        //Node GetMinFNode()
        //{
        //    float minF = float.MaxValue;
        //    float minH = float.MaxValue;
        //    int index = 0;

        //    for (int i = 0; i < _openList.Count; i++)
        //    {
        //        if(_openList[i].F == minF)
        //        {
        //            if(_openList[i].H < minH)
        //            {
        //                index = i;
        //                minF = _openList[i].F;
        //                minH = _openList[i].H;
        //            }

        //        }
        //        else if (_openList[i].F < minF)
        //        {
        //            index = i;
        //            minF = _openList[i].F;
        //            minH = _openList[i].H;
        //        }
        //    }

        //    return _openList[index];
        //}

        // ���� ���� �ݿø��� ���� ���� ����� ��带 ã�´�.
        //public async Task<List<Vector3>> FindPathAwait(Vector3 startPos, Vector3 targetPos)
        //{
        //    //// ����Ʈ �ʱ�ȭ
        //    _openList.Clear();
        //    _closedList.Clear();

        //    _openListPoints.Clear();
        //    _closedListPoints.Clear();

        //    Vector3Int startIndex = _gridComponent.ReturnNodeIndex(startPos);
        //    Vector3Int endIndex = _gridComponent.ReturnNodeIndex(targetPos);

        //    Node startNode = _gridComponent.GetNode(startIndex);
        //    Node endNode = _gridComponent.GetNode(endIndex);

        //    if (startNode == null || endNode == null) { return null; }

        //    _startNodePos = startNode.SurfacePos;
        //    _endNodePos = endNode.SurfacePos;

        //    _openList.Insert(startNode);
        //    _openListPoints.Add(startNode.SurfacePos);

        //    while (_openList.Count > 0)
        //    {
        //        await Task.Delay(_awaitDuration);

        //        Node targetNode = _openList.ReturnMin();
        //        if (targetNode == endNode) // �������� Ÿ���� ������ ��
        //        {
        //            Stack<Node> finalList = new Stack<Node>();

        //            Node TargetCurNode = targetNode;
        //            while (TargetCurNode != startNode)
        //            {
        //                finalList.Push(TargetCurNode);
        //                TargetCurNode = TargetCurNode.ParentNode;
        //            }
        //            //finalList.Push(startNode);

        //            return ConvertNodeToV3(finalList);
        //        }

        //        _openList.DeleteMin(); // �ش� �׸��� ������
        //        _closedList.Add(targetNode); // �ش� �׸��� �߰�����
        //        _closedListPoints.Add(targetNode.SurfacePos);

        //        AddNearGridInList(targetNode, endNode.SurfacePos);
        //    }

        //    // �� ���� ��θ� ã�� ���� ��Ȳ��
        //    return null;
        //}

        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            //// ����Ʈ �ʱ�ȭ
            _openList.Clear();
            _closedList.Clear();

            //_openListPoints.Clear();
            //_closedListPoints.Clear();

            Vector3Int startIndex = _gridComponent.ReturnNodeIndex(startPos);
            Vector3Int endIndex = _gridComponent.ReturnNodeIndex(targetPos);

            Node startNode = _gridComponent.GetNode(startIndex);
            Node endNode = _gridComponent.GetNode(endIndex);

            if (startNode == null || endNode == null) { return null; }

            _startNodePos = startNode.SurfacePos;
            _endNodePos = endNode.SurfacePos;

            _openList.Insert(startNode);
            //_openListPoints.Add(startNode.SurfacePos);

            while (_openList.Count > 0)
            {
                Node targetNode = _openList.ReturnMin();
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

                _openList.DeleteMin(); // �ش� �׸��� ������
                _closedList.Add(targetNode); // �ش� �׸��� �߰�����
                //_closedListPoints.Add(targetNode.SurfacePos);

                AddNearGridInList(targetNode, endNode.SurfacePos);
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

        void AddNearGridInList(Node targetNode, Vector3 endNodePos)
        {
            Vector3Int startIndex = targetNode.Index;
            List<SerializableVector3Int> nearNodes = targetNode.NearNodeIndexesInGround;// _gridComponent.ReturnNearNodesInGround(startIndex);

            for (int i = 0; i < nearNodes.Count; i++)
            {
                Node nearNode = _gridComponent.GetNode(nearNodes[i]);
                if (nearNode.CanStep == false || _closedList.Contains(nearNode)) continue; // �������� �ʰų� ���� ����Ʈ�� �ִ� ��� ���� �׸��� Ž�� --> Ground�� ��� �����־�� Ž�� ������

                // ���߿� �ִ� ���� Pos, ���� �ִ� ���� SurfacePos�� ó���Ѵ�.
                float moveCost = GetHeuristic(targetNode.SurfacePos, nearNode.SurfacePos);
                // �� �κ� �߿�! --> �Ÿ��� �����ؼ� ������Ʈ ���� �ʰ� ��� �����ִ� ������� �����ؾ���
                moveCost += targetNode.G;

                bool isOpenListContainNearGrid = _openList.Contain(nearNode);

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
                    _openList.Insert(nearNode);
                    //_openListPoints.Add(nearNode.Pos);
                }
            }
        }
    }
}