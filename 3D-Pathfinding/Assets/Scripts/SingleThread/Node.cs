using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace SingleThread
{
    [System.Serializable]
    public struct SerializableVector3
    {
        [SerializeField] private float x, y, z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;  // 소수점 두 번째 자리까지 반올림
            this.y = y;
            this.z = z;
        }

        public float X { get => x; }
        public float Y { get => y; }
        public float Z { get => z; }

        // ✅ 암시적 변환 (Vector3 <-> SerializableVector3)
        public static implicit operator Vector3(SerializableVector3 v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v.x, v.y, v.z);
    }

    [System.Serializable]
    public struct SerializableVector3Int
    {
        [SerializeField] private int x, y, z;

        public SerializableVector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public int X { get => x; }
        public int Y { get => y; }
        public int Z { get => z; }

        // ✅ 암시적 변환 (Vector3Int <-> SerializableVector3)
        public static implicit operator Vector3Int(SerializableVector3Int v) => new Vector3Int(v.x, v.y, v.z);
        public static implicit operator SerializableVector3Int(Vector3Int v) => new SerializableVector3Int(v.x, v.y, v.z);
    }

    [System.Serializable]
    public class Node : INode<Node>
    {
        [System.Serializable]
        public enum State
        {
            Empty,
            Block,
            NonPass,
        }

        public Node(Vector3 pos, State state)
        {
            _pos = pos;
            _state = state;

            _haveSurface = false;
            _surfacePos = Vector3.zero;
        }

        [SerializeField] State _state;
        public State CurrentState { get { return _state; } }

        [SerializeField] SerializableVector3 _pos; // 그리드 실제 위치
        public Vector3 Pos { get { return _pos; } }

        [SerializeField] SerializableVector3Int _index; // 그리드 실제 위치
        public Vector3Int Index { get { return _index; } set { _index = value; } }

        [SerializeField] bool _haveSurface;
        public bool HaveSurface { set { _haveSurface = value; } }

        [SerializeField] SerializableVector3 _surfacePos; // 발을 딛을 수 있는 표면 위치
        public Vector3 SurfacePos { set { _surfacePos = value; } get { return _surfacePos; } }

        public bool CanStep { get { return _state == State.Block && _haveSurface == true; } }


        // 만약 해당 위치로 이동할 때 노드가 Block인 경우 이 노드 사용
        // 만약 이 노드도 사용 불가능하다면 새로운 노드를 찾아야 한다.
        //public Node AlternativeNode { get; set; } // 대체할 노드

        [SerializeField] List<SerializableVector3Int> _nearNodeIndexesInGround;
        public List<SerializableVector3Int> NearNodeIndexesInGround { get => _nearNodeIndexesInGround; set => _nearNodeIndexesInGround = value; }

        [SerializeField] List<SerializableVector3Int> _nearNodeIndexes;
        public List<SerializableVector3Int> NearNodeIndexexInAir { get => _nearNodeIndexes; set => _nearNodeIndexes = value; }

        //public List<Node> NearNodesInGround { get; set; }
        //public List<Node> NearNodes { get; set; }



        // g는 시작 노드부터의 거리
        // h는 끝 노드부터의 거리
        // f는 이 둘을 합친 값
        [System.NonSerialized] float g, h = 0;
        public float G { get { return g; } set { g = value; } }
        public float H { get { return h; } set { h = value; } }
        public float F { get { return g + h; } }

        public Node ParentNode { get; set; }
        public int StoredIndex { get; set; }

        public void Dispose()
        {
            StoredIndex = -1;
            ParentNode = null;
        }

        public int CompareTo(Node other)
        {
            int compareValue = F.CompareTo(other.F);
            if (compareValue == 0) compareValue = H.CompareTo(other.H);
            return compareValue;
        }
    }
}