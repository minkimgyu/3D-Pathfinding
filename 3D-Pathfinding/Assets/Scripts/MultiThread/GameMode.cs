//#define Await
#define Direct

using UnityEngine;
using System.Collections.Generic;
using System;

namespace MultiThread
{
    public struct PathfindingResult
    {
        private List<Vector3> _path;
        public List<Vector3> Path { get => _path; }

        public PathfindingResult(List<Vector3> path)
        {
            _path = path;
        }
    }

    public struct PathfindingRequest
    {
        public Vector3 startPoint;
        public Vector3 endPoint;

        public PathfindingRequest(Vector3 startPoint, Vector3 endPoint, Action<PathfindingResult> onCompleted)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            OnCompleted = onCompleted;
        }

        public Action<PathfindingResult> OnCompleted { get; set; }
    }


    public class GameMode : MonoBehaviour
    {
        GroundPathfinder _groundPathfinder;
        GridComponent _gridComponent;

        [SerializeField] Transform _startPointParent;
        [SerializeField] Transform _endPointParent;

        List<Transform> _startPoints;
        List<Transform> _endPoints;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _startPoints = new List<Transform>();
            _endPoints = new List<Transform>();

            for (int i = 0; i < _startPointParent.childCount; i++)
            {
                _startPoints.Add(_startPointParent.GetChild(i));
            }

            for (int i = 0; i < _endPointParent.childCount; i++)
            {
                _endPoints.Add(_endPointParent.GetChild(i));
            }

            _requestQueue = new Queue<PathfindingResult>();
            _gridComponent = GetComponent<GridComponent>();
            _groundPathfinder = GetComponent<GroundPathfinder>();
            _groundPathfinder.Initialize(_gridComponent);


            InvokeRepeating("RepeatFunction", 1.0f, 0.01f); // 2초 후 시작, 1초마다 실행
        }

        List<List<Vector3>> _results = new List<List<Vector3>>();

#if Await

    // Update is called once per frame
    private async void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _points = await _groundPathfinder.FindPathAwait(_startPoint.transform.position, _endPoint.transform.position);
        }
    }

#elif Direct

        Queue<PathfindingResult> _requestQueue;

        // Update is called once per frame
        private void Update()
        {
            if(_requestQueue.Count > 0)
            {
                PathfindingResult result = _requestQueue.Dequeue();
                _results.Add(result.Path);

                if(_results.Count > 30)
                {
                    _results.RemoveAt(0);
                }
            }

        }
#endif
        void RepeatFunction()
        {
            Vector3 startPoint = _startPoints[UnityEngine.Random.Range(0, _startPoints.Count)].position;
            Vector3 endPoint = _endPoints[UnityEngine.Random.Range(0, _endPoints.Count)].position;

            _groundPathfinder.FindPath(new PathfindingRequest(startPoint, endPoint, AddResult));
        }

        object _resultLock = new object();

        void AddResult(PathfindingResult result)
        {
            lock (_resultLock)
            {
                _requestQueue.Enqueue(result);
            }
        }

        private void OnDrawGizmos()
        {
            if (_results.Count == 0) return;

            for (int i = 0; i < _results.Count; i++)
            {
                for (int j = 1; j < _results[i].Count; j++)
                {
                    Gizmos.color = Color.magenta;
                    if (j == 0)
                    {
                        Gizmos.DrawCube(_results[i][j], Vector3.one / 2);
                    }
                    else if(j == _results[i].Count - 1)
                    {
                        Gizmos.DrawCube(_results[i][j], Vector3.one / 2);
                    }

                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(_results[i][j - 1], _results[i][j]);
                }
            }
        }
    }
}