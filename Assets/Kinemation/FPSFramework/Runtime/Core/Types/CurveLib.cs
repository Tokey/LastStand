﻿// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.Attributes;

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Core.Types
{
    public struct AnimCurveValue
    {
        public float value;
        public float cache;
        public float target;
    }
    
    [Serializable]
    public struct AnimCurve
    {
        [AnimCurveName] public string name;
        public AnimationCurve curve;
    }
    
    [Serializable]
    public struct VectorCurve
    {
        public AnimationCurve x;
        public AnimationCurve y;
        public AnimationCurve z;

        public float GetLastTime()
        {
            float maxTime = -1f;

            float curveTime = GetMaxTime(x);
            maxTime = curveTime > maxTime ? curveTime : maxTime;
        
            curveTime = GetMaxTime(y);
            maxTime = curveTime > maxTime ? curveTime : maxTime;
        
            curveTime = GetMaxTime(z);
            maxTime = curveTime > maxTime ? curveTime : maxTime;

            return maxTime;
        }
        
        public static float GetMaxTime(AnimationCurve curve)
        {
            return curve[curve.length - 1].time;
        }
        
        public Vector3 Evaluate(float time)
        {
            return new Vector3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time));
        }

        public bool IsValid()
        {
            return x != null && y != null && z != null;
        }

        public VectorCurve(Keyframe[] keyFrame)
        {
            x = new AnimationCurve(keyFrame);
            y = new AnimationCurve(keyFrame);
            z = new AnimationCurve(keyFrame);
        }
    }
    
    [Serializable]
    public enum EEaseFunc
    {
        Linear,
        Sine,
        Cubic,
        Custom
    }
    
    [Serializable]
    public struct EaseMode
    {
        public EEaseFunc easeFunc;
        public AnimationCurve curve;

        public EaseMode(EEaseFunc func)
        {
            easeFunc = func;
            curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        }
    }

    public static class CurveLib
    {
        public static string Curve_MaskLeftHand = "MaskLeftHand";
        public static string Curve_MaskLookLayer = "MaskLookLayer";
        public static string Curve_WeaponBone = "WeaponBone";
        public static string Curve_Overlay = "Overlay";

        public static string Curve_Camera_Pitch = "Camera_Pitch";
        public static string Curve_Camera_Yaw = "Camera_Yaw";
        public static string Curve_Camera_Roll = "Camera_Roll";
        
        public static string Curve_IK_X = "IK_X";
        public static string Curve_IK_Y = "IK_Y";
        public static string Curve_IK_Z = "IK_Z";
        
        public static string Curve_IK_LeftHand_X = "IK_LeftHand_X";
        public static string Curve_IK_LeftHand_Y = "IK_LeftHand_Y";
        public static string Curve_IK_LeftHand_Z = "IK_LeftHand_Z";
        
        public static string Curve_IK_RightHand_X = "IK_RightHand_X";
        public static string Curve_IK_RightHand_Y = "IK_RightHand_Y";
        public static string Curve_IK_RightHand_Z = "IK_RightHand_Z";

        public static readonly List<string> AnimCurveNames = new()
        {
            Curve_MaskLeftHand,
            Curve_MaskLookLayer,
            Curve_WeaponBone,
            Curve_Overlay,
            Curve_Camera_Pitch,
            Curve_Camera_Yaw,
            Curve_Camera_Roll,
            Curve_IK_LeftHand_X,
            Curve_IK_LeftHand_Y,
            Curve_IK_LeftHand_Z,
            Curve_IK_RightHand_X,
            Curve_IK_RightHand_Y,
            Curve_IK_RightHand_Z,
            Curve_IK_X,
            Curve_IK_Y,
            Curve_IK_Z,
        };

        public static float Ease(float a, float b, float alpha, EaseMode ease)
        {
            alpha = Mathf.Clamp01(alpha);
            
            switch (ease.easeFunc)
            {
                case EEaseFunc.Sine:
                    alpha = -(Mathf.Cos(Mathf.PI * alpha) - 1) / 2;
                    break;
                case EEaseFunc.Cubic:
                    alpha = alpha < 0.5 ? 4 * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 3) / 2;
                    break;
                case EEaseFunc.Custom:
                    alpha = ease.curve?.Evaluate(alpha) ?? alpha;
                    break;
            }
            
            return Mathf.Lerp(a, b, alpha);
        }
    }
}