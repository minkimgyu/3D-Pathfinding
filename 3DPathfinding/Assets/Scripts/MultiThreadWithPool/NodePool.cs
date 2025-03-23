using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiThreadWithPool
{
    public class NodePool
    {
        private readonly Queue<Node> _pool;
        private readonly int _maxSize;
        private readonly object _lock = new object();

        public int TotalCount { get; set; }

        public NodePool(int initialSize, int maxSize)
        {
            _pool = new Queue<Node>(initialSize);
            _maxSize = maxSize;
            InitializePool(initialSize);
        }

        private void InitializePool(int size)
        {
            for (int i = 0; i < size; i++)
            {
                TotalCount++;
                _pool.Enqueue(new Node()); // 기본 생성자 사용 - 필요에 따라 초기화 로직 추가
            }
        }

        public Node Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    return _pool.Dequeue();
                }
                else if (_pool.Count < _maxSize || _maxSize == 0) // maxSize가 0이면 제한 없음
                {
                    TotalCount++;
                    return new Node(); // 풀에 없으면 새로 생성 (최대 크기 제한 고려)
                }
                else
                {
                    TotalCount++;
                    // 풀이 가득 찼을 경우, 필요에 따라 예외 처리 또는 다른 전략 구현
                    Debug.LogWarning("NodePool is full. Consider increasing the max size.");
                    return new Node(); // 임시로 새로 생성
                }
            }
        }

        public void Release(Node node)
        {
            lock (_lock)
            {
                // 노드 상태를 초기화하여 재사용 준비 (필요에 따라)
                node.Reset();
                _pool.Enqueue(node);
            }
        }

        public int Size => _pool.Count;
        public int MaxSize => _maxSize;
    }
}