using MultiThreadWithPool;
using SingleThread;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiThread
{
    public class Agent : MonoBehaviour
    {
        [SerializeField] Rigidbody _rigidbody;

        List<Transform> _endPoints;
        Vector3 _end;
        [SerializeField] float _speed = 5f;

        Action<PathfindingRequestData, Action<PathfindingResultData>> RequestPath;

        public void Initialize(List<Transform> endPoints, Action<PathfindingRequestData, Action<PathfindingResultData>> RequestPath)
        {
            _delayTimer = new Timer();
            _endPoints = endPoints;
            this.RequestPath = RequestPath;
            ResetEndPoint();
            // 초기 경로 요청
            RequestNewPath();
        }

        void ResetEndPoint()
        {
            int index = UnityEngine.Random.Range(0, _endPoints.Count);
            Transform endPoint = _endPoints[index];
            _end = endPoint.position;
            _storedTargetPos = _end; // 초기화 시점 변경
        }

        List<Vector3> _path = new List<Vector3>();
        int _pathIndex = 0;
        const float _delayDuration = 0.5f;
        const float _reachDistance = 0.5f;

        Timer _delayTimer;

        //private void OnDrawGizmos()
        //{
        //    if (_path == null) return;

        //    for (int i = 1; i < _path.Count; i++)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawLine(_path[i - 1], _path[i]);
        //    }
        //}

        public bool NowFinish()
        {
            return _path != null && _pathIndex == _path.Count - 1;
        }

        int _pathRequestIndex = 0;
        int getIndex = 0;

        void GetPath(PathfindingResultData result)
        {
            //Debug.Log($"{transform.name} - {result.RequestIndex}");

            if (result.RequestIndex < getIndex)
            {
                //Debug.Log($"{transform.name} - {result.RequestIndex}");
                //Debug.Log("순서가 보장되지 않은 경우임");
                return;
            }
            getIndex = result.RequestIndex;

            _path = result.Path;
            _pathIndex = 0;

            _storedTargetPos = _end;
            _delayTimer.Reset();
            _delayTimer.Start(_delayDuration);
        }

        Vector3 _storedTargetPos;

        void RequestNewPath()
        {
            _pathRequestIndex++;
            RequestPath?.Invoke(new PathfindingRequestData(transform.position, _end, _pathRequestIndex), GetPath);
        }

        // Update is called once per frame
        void Update()
        {
            if (NowFinish() == true)
            {
                ResetEndPoint();
                RequestNewPath(); // 목표 지점에 도달하면 새 경로 요청
            }

            if(_delayTimer.CurrentState != Timer.State.Running)
            {
                RequestNewPath(); // 타이머가 종료되면 새 경로 요청
                _delayTimer.Reset();
                _delayTimer.Start(_delayDuration);
            }

            // 경로가 없는 경우 정지
            if (_path == null || _path.Count == 0)
            {
                _rigidbody.velocity = Vector3.zero;
                return;
            }

            // 현재 목표 지점과의 거리가 가까워지면 새 경로 요청 (움직이는 중이 아닐 때)
            if (_rigidbody.velocity.magnitude <= 0.1f && Vector3.Distance(transform.position, _storedTargetPos) <= _reachDistance)
            {
                RequestNewPath();
            }

            // 경로 따라 이동
            if (_pathIndex < _path.Count)
            {
                float distance = Vector3.Distance(transform.position, _path[_pathIndex]);
                bool closeEnough = distance <= _reachDistance;

                if (closeEnough == true && _pathIndex < _path.Count - 1)
                {
                    _pathIndex++;
                }

                if (_pathIndex < _path.Count)
                {
                    _rigidbody.velocity = (_path[_pathIndex] - transform.position).normalized * _speed;
                }
                else
                {
                    _rigidbody.velocity = Vector3.zero; // 경로의 마지막 지점에 도달하면 정지
                }
            }
            else
            {
                _rigidbody.velocity = Vector3.zero; // 경로가 끝났는데 _pathIndex가 범위를 벗어난 경우 (예외 처리)
            }
        }
    }
}