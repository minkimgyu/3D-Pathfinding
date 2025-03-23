using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SingleThread
{
    public class PathSeeker : MonoBehaviour, IInjectPathfind
    {
        Func<Vector3, Vector3, List<Vector3>> FindPath;

        List<Vector3> _path = new List<Vector3>();
        int _pathIndex = 0;
        const float _delayDuration = 0.5f;
        const float _reachDistance = 0.5f;
        const float _updateThreshold = 1.0f; // 경로 갱신 최소 거리

        Timer _delayTimer;
        Vector3 _storedTargetPos;

        public void AddPathfind(Func<Vector3, Vector3, List<Vector3>> FindPath)
        {
            this.FindPath = FindPath;
        }

        public void Initialize()
        {
            _storedTargetPos = Vector3.positiveInfinity;
            _delayTimer = new Timer();
        }

        //private void OnDrawGizmos()
        //{
        //    if (_path == null || _path.Count < 2) return;

        //    Gizmos.color = Color.red;
        //    for (int i = 1; i < _path.Count; i++)
        //    {
        //        Gizmos.DrawLine(_path[i - 1], _path[i]);
        //    }
        //}

        public bool NowFinish()
        {
            if (_path == null || _path.Count == 0) return true;

            // 마지막 지점에 도착했는지 확인
            return _pathIndex >= _path.Count - 1 &&
                   (transform.position - _path[_pathIndex]).sqrMagnitude <= _reachDistance * _reachDistance;
        }

        public Vector3 ReturnDirection(Vector3 targetPos)
        {
            bool shouldUpdatePath = _delayTimer.CurrentState != Timer.State.Running ||
                                    (_storedTargetPos - targetPos).sqrMagnitude > _updateThreshold * _updateThreshold;

            if (shouldUpdatePath && FindPath != null)
            {
                var newPath = FindPath(transform.position, targetPos);

                if (newPath == null || newPath.Count == 0)
                {
                    _path.Clear(); // 기존 경로 삭제
                    return Vector3.zero;
                }

                _path.Clear();
                _path.AddRange(newPath);
                _pathIndex = 0;

                _storedTargetPos = targetPos;
                _delayTimer.Reset();
                _delayTimer.Start(_delayDuration);
            }

            if (_path.Count == 0) return Vector3.zero;

            // 현재 위치가 목표 위치에 가까워지면 다음 경로로 이동
            float distanceSqr = (transform.position - _path[_pathIndex]).sqrMagnitude;
            if (distanceSqr <= _reachDistance * _reachDistance && _pathIndex < _path.Count - 1)
            {
                _pathIndex++;
            }

            return (_path[_pathIndex] - transform.position).normalized;
        }
    }
}
