using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace SingleThread
{
    public class Agent : MonoBehaviour
    {
        [SerializeField] PathSeeker _pathSeeker;
        [SerializeField] Rigidbody _rigidbody;

        List<Transform> _endPoints;
        Vector3 _end;
        [SerializeField] float _speed = 5f;

        public void Initialize(List<Transform> endPoints, Func<Vector3, Vector3, List<Vector3>> FindPath)
        {
            _endPoints = endPoints;
            _pathSeeker.Initialize();
            _pathSeeker.AddPathfind(FindPath);
            ResetEndPoint();
        }

        void ResetEndPoint()
        {
            int index = UnityEngine.Random.Range(0, _endPoints.Count);
            Transform endPoint = _endPoints[index];
            _end = endPoint.position;
        }

        // Update is called once per frame
        void Update()
        {
            if(_pathSeeker.NowFinish() == true)
            {
                ResetEndPoint();
            }

            _rigidbody.velocity = _pathSeeker.ReturnDirection(_end) * _speed;
        }
    }
}