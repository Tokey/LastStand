// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using Kinemation.FPSFramework.Runtime.FPSAnimator;

using UnityEngine;

using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class SlotLayer : AnimLayer
    {
        private IKAnimation _asset;
        
        private LocRot _out;
        private LocRot _cache;
        
        private float _length;
        private float _playback;
        private float _blendAlpha;
        private float _scale;
        
        private void Reset()
        {
            _out = _cache = LocRot.identity;
        }

        private void UpdateMotion()
        {
            if (_asset == null || Mathf.Approximately(_playback, _length))
            {
                _out = new LocRot(Vector3.zero, Quaternion.identity);
                return;
            }
            
            _playback += Time.deltaTime * _asset.playRate;
            _playback = Mathf.Clamp(_playback, 0f, _length);

            LocRot activeMotion = new LocRot()
            {
                position = _asset.loc.Evaluate(_playback) * _scale,
                rotation = Quaternion.Euler(_asset.rot.Evaluate(_playback) * _scale)
            };

            _blendAlpha += Time.deltaTime * _asset.blendSpeed;
            _blendAlpha = Mathf.Clamp01(_blendAlpha);
            _out = CoreToolkitLib.Lerp(_cache, activeMotion, _blendAlpha);
        }
        
        public void PlayMotion(IKAnimation animationAsset)
        {
            _asset = animationAsset;
            if (_asset == null) return;
            
            _cache = _out;
            _scale = Random.Range(_asset.scale.x, _asset.scale.y);
            
            _playback = 0f;
            _blendAlpha = 0f;
            _length = _asset.GetLength();
        }

        public override void OnAnimStart()
        {
            Reset();
        }

        public override void OnAnimUpdate()
        {
            UpdateMotion();
            GetMasterIK().Move(GetRootBone(), _out.position, smoothLayerAlpha);
            GetMasterIK().Rotate(GetRootBone().rotation, _out.rotation, smoothLayerAlpha);
        }
    }
}