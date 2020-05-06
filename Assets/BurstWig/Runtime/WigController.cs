﻿using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using VisualEffect = UnityEngine.VFX.VisualEffect;

namespace BurstWig
{
    public class WigController : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] MeshRenderer _source = null;
        [SerializeField] VisualEffect _target = null;
        [SerializeField, Range(8, 256)] int _segmentCount = 64;
        [SerializeField] WigProfile _profile = null;
        [SerializeField] uint _randomSeed = 0;

        #endregion

        #region Internal buffers

        NativeArray<RootPoint> _rootPoints;
        NativeArray<float4> _positionBuffer;
        NativeArray<float3> _velocityBuffer;
        Texture2D _positionMap;

        #endregion

        #region MonoBehaviour implementation

        void Start()
        {
            var mesh = _source.GetComponent<MeshFilter>().sharedMesh;
            var vcount = mesh.vertexCount;

            _rootPoints = Utility.NewBuffer<RootPoint>(vcount, 1);
            _positionBuffer = Utility.NewBuffer<float4>(vcount, _segmentCount);
            _velocityBuffer = Utility.NewBuffer<float3>(vcount, _segmentCount);

            var vertices = mesh.vertices;
            var normals = mesh.normals;

            for (var vi = 0; vi < vcount; vi++)
                _rootPoints[vi] = new RootPoint
                  { position = vertices[vi], normal = normals[vi] };

            _positionMap = new Texture2D
              (_segmentCount, vcount, TextureFormat.RGBAFloat, false);

            _target.SetTexture("PositionMap", _positionMap);
            _target.SetUInt("VertexCount", (uint)vcount);
            _target.SetUInt("SegmentCount", (uint)_segmentCount);
        }

        void OnDestroy()
        {
            if (_rootPoints.IsCreated) _rootPoints.Dispose();
            if (_positionBuffer.IsCreated) _positionBuffer.Dispose();
            if (_velocityBuffer.IsCreated) _velocityBuffer.Dispose();
            if (_positionMap != null) Destroy(_positionMap);
        }

        void Update()
        {
            var job = new WigUpdateJob
            {
                R = _rootPoints,
                P = _positionBuffer,
                V = _velocityBuffer,

                time = Time.time,
                dt = Time.deltaTime,

                tf = (float4x4)_source.transform.localToWorldMatrix,
                randomSeed = _randomSeed,

                length = _profile.length,
                lengthRandomness = _profile.lengthRandomness,
                damping = _profile.damping,
                spring = _profile.spring,
                gravity = _profile.gravity,
                noiseFrequency = _profile.noiseFrequency,
                noiseAmplitude = _profile.noiseAmplitude,
                noiseSpeed = _profile.noiseSpeed
            };

            job.Run();

            _positionMap.LoadRawTextureData(_positionBuffer);
            _positionMap.Apply();
        }

        #endregion
    }
}
