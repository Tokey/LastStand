// Designed by KINEMATION, 2023

using System;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.FPSAnimator
{
    [System.Serializable, CreateAssetMenu(fileName = "NewIKAnimation", menuName = "FPS Animator/IKAnimation")]
    public class IKAnimation : ScriptableObject
    {
        public VectorCurve rot = new VectorCurve(new Keyframe[]
        {
            new (0, 0), 
            new (1, 0)
        });
        
        public VectorCurve loc = new VectorCurve(new Keyframe[]
        {
            new (0, 0), 
            new (1, 0)
        });
        
        [Tooltip("Blend time in seconds")]
        public float blendSpeed = 0.1f;
        [Tooltip("Play rate multiplier")]
        public float playRate = 1f;
        [Tooltip("Scale will be a random value between X and Y")]
        public Vector2 scale = Vector3.one;

        public float GetLength()
        {
            if (!rot.IsValid() || !loc.IsValid())
            {
                return 0f;
            }

            return Mathf.Max(rot.GetLastTime(), loc.GetLastTime());
        }
    }
}