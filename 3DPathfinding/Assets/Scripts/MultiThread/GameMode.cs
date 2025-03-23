//#define Await
#define Direct

using UnityEngine;
using System.Collections.Generic;
using System;
using SingleThread;
using System.Collections.Concurrent;

namespace MultiThread
{
    public struct PathfindingResultData
    {
        int requestIndex;
        public int RequestIndex { get => requestIndex; }


        private List<Vector3> _path;
        public List<Vector3> Path { get => _path; }

        public PathfindingResultData(int requestIndex, List<Vector3> path)
        {
            this.requestIndex = requestIndex;
            _path = path;
        }
    }

    public struct PathfindingComplete
    {
        PathfindingResultData result;
        public Action<PathfindingResultData> InjectResult { get; set; }

        public PathfindingResultData Result { get => result; }

        public PathfindingComplete(PathfindingResultData result, Action<PathfindingResultData> InjectPathResult)
        {
            this.result = result;
            this.InjectResult = InjectPathResult;
        }
    }

    public struct PathfindingRequestData
    {
        private Vector3 startPoint;
        private Vector3 endPoint;
        private int requestIndex;

        public PathfindingRequestData(Vector3 startPoint, Vector3 endPoint, int requestIndex)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            this.requestIndex = requestIndex;
        }

        public int RequestIndex { get => requestIndex; }
        public Vector3 StartPoint { get => startPoint;}
        public Vector3 EndPoint { get => endPoint; }
    }

    public struct PathfindingStart
    {
        private PathfindingRequestData pathfindingRequestData;

        public PathfindingStart(PathfindingRequestData pathfindingRequestData, Action<PathfindingComplete> OnCompleted, Action<PathfindingResultData> InjectResult)
        {
            this.pathfindingRequestData = pathfindingRequestData;
            this.OnCompleted = OnCompleted;
            this.InjectResult = InjectResult;
        }

        public PathfindingRequestData Data { get => pathfindingRequestData; }
        public Action<PathfindingComplete> OnCompleted { get; set; }
        public Action<PathfindingResultData> InjectResult { get; set; }
    }


    public class GameMode : MonoBehaviour
    {
        GroundPathfinder _groundPathfinder;
        GridComponent _gridComponent;

        [SerializeField] Transform _endPointParent;

        List<Transform> _endPoints;

        [SerializeField] Agent[] _agents;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _endPoints = new List<Transform>();

            for (int i = 0; i < _endPointParent.childCount; i++)
            {
                _endPoints.Add(_endPointParent.GetChild(i));
            }

            _completeQueue = new Queue<PathfindingComplete>();
            _gridComponent = GetComponent<GridComponent>();
            _gridComponent.Initialize();

            _groundPathfinder = GetComponent<GroundPathfinder>();
            _groundPathfinder.Initialize(_gridComponent);

            for (int i = 0; i < _agents.Length; i++)
            {
                _agents[i].Initialize(_endPoints, RequestPath);
            }

            //InvokeRepeating("RepeatFunction", 1.0f, 0.01f); // 2초 후 시작, 1초마다 실행
        }

        //List<List<Vector3>> _results = new List<List<Vector3>>();

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

        Queue<PathfindingComplete> _completeQueue;

        // Update is called once per frame
        private void Update()
        {
            if(_completeQueue.Count > 0)
            {
                PathfindingComplete complete;

                _completeQueue.TryDequeue(out complete);
                complete.InjectResult?.Invoke(complete.Result);
                requestCount--;
            }

        }
#endif

        [SerializeField] int requestCount = 0;
        [SerializeField] int maxRequestCount = 13;

        void RequestPath(PathfindingRequestData requestData, Action<PathfindingResultData> InjectResult)
        {
            if (requestCount >= maxRequestCount) return;

            requestCount++;
            _groundPathfinder.FindPath(new PathfindingStart(requestData, AddResult, InjectResult));
        }

        object _resultLock = new object();

        void AddResult(PathfindingComplete result)
        {
            lock (_resultLock)
            {
                _completeQueue.Enqueue(result);
            }
        }

        //private void OnDrawGizmos()
        //{
        //    if (_results.Count == 0) return;

        //    for (int i = 0; i < _results.Count; i++)
        //    {
        //        for (int j = 1; j < _results[i].Count; j++)
        //        {
        //            Gizmos.color = Color.magenta;
        //            if (j == 0)
        //            {
        //                Gizmos.DrawCube(_results[i][j], Vector3.one / 2);
        //            }
        //            else if(j == _results[i].Count - 1)
        //            {
        //                Gizmos.DrawCube(_results[i][j], Vector3.one / 2);
        //            }

        //            Gizmos.color = Color.red;
        //            Gizmos.DrawLine(_results[i][j - 1], _results[i][j]);
        //        }
        //    }
        //}
    }
}