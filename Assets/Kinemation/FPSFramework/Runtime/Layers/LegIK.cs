// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;

using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class LegIK : AnimLayer
    {
        [Header("Basic Settings")]
        [SerializeField] protected float traceRadius;
        [SerializeField] protected float footTraceLength;
        [SerializeField] protected float heightOffset;
        [SerializeField] protected float footInterpSpeed;
        [SerializeField] protected float pelvisInterpSpeed;
        [SerializeField] protected LayerMask layerName;
        
        protected LocRot smoothRfIK;
        protected LocRot smoothLfIK;
        protected LocRot targetRfIK;
        protected LocRot targetLfIK;

        protected (Vector3, Vector3) rightLeftStart;
        protected (Vector3, Vector3) rightLeftEnd;
        
        protected float smoothPelvis;

        private void OnDrawGizmos()
        {
            if (!drawDebugInfo) return;
            
            var color = Gizmos.color;
            Gizmos.color = Color.red;

            Gizmos.DrawLine(rightLeftStart.Item1, rightLeftEnd.Item1);
            Gizmos.DrawLine(rightLeftStart.Item2, rightLeftEnd.Item2);
            
            Gizmos.color = color;
        }

        private LocRot TraceFoot(Transform footTransform)
        {
            Vector3 origin = footTransform.position;
            origin.y = GetRootBone().position.y + heightOffset;
            
            LocRot target = new LocRot(footTransform.position, footTransform.rotation);
            Quaternion finalRotation = footTransform.rotation;

            float animOffset = GetRootBone().InverseTransformPoint(footTransform.position).y;
            
            if(Physics.Raycast(origin, -GetRootBone().up, out RaycastHit hit, footTraceLength,layerName))
            {
                var rotation = footTransform.rotation;
                finalRotation = Quaternion.FromToRotation(GetRootBone().up, hit.normal) * rotation;
                finalRotation.Normalize();
                target.position = hit.point;
                
                target.position = new Vector3(target.position.x, target.position.y + animOffset, target.position.z);
            }
            
            target.position -= footTransform.position;
            target.rotation = Quaternion.Inverse(footTransform.rotation) * finalRotation;
            
            return target;
        }

        protected void TraceFeet()
        {
            targetRfIK = TraceFoot(GetRightFoot());
            targetLfIK = TraceFoot(GetLeftFoot());
        }

        public override void OnAnimUpdate()
        {
            if (Mathf.Approximately(smoothLayerAlpha, 0f))
            {
                return;
            }
            
            rightLeftStart.Item1 = GetRightFoot().position;
            rightLeftStart.Item1.y = GetRootBone().position.y + heightOffset;
            
            rightLeftStart.Item2 = GetLeftFoot().position;
            rightLeftStart.Item2.y = GetRootBone().position.y + heightOffset;

            rightLeftEnd.Item1 = rightLeftStart.Item1;
            rightLeftEnd.Item2 = rightLeftStart.Item2;

            rightLeftEnd.Item1 -= GetRootBone().up * footTraceLength;
            rightLeftEnd.Item2 -= GetRootBone().up * footTraceLength;

            TraceFeet();
            
            var rightFoot = GetRightFoot();
            var leftFoot = GetLeftFoot();
            
            smoothRfIK = CoreToolkitLib.Glerp(smoothRfIK, targetRfIK, footInterpSpeed);
            smoothLfIK = CoreToolkitLib.Glerp(smoothLfIK, targetLfIK, footInterpSpeed);
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), rightFoot, smoothRfIK.position, smoothLayerAlpha);
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), leftFoot, smoothLfIK.position, smoothLayerAlpha);

            rightFoot.rotation *= Quaternion.Slerp(Quaternion.identity, smoothRfIK.rotation, smoothLayerAlpha);
            leftFoot.rotation *= Quaternion.Slerp(Quaternion.identity, smoothLfIK.rotation, smoothLayerAlpha);

            float rightFootHeight = targetRfIK.position.y;
            float leftFootHeight = targetLfIK.position.y;
            float pelvisTarget = Mathf.Min(rightFootHeight, leftFootHeight);
            
            smoothPelvis = CoreToolkitLib.Glerp(smoothPelvis, pelvisTarget, pelvisInterpSpeed);
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), GetPelvis(), 
                new Vector3(0f, smoothPelvis, 0f), 1f);
        }
    }
}