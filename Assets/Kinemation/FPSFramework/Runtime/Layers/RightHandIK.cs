// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class RightHandIK : AnimLayer
    {
        public override void OnAnimUpdate()
        {
            if (GetGunAsset() == null) return;

            GetRightHandIK().Move(GetMasterPivot(), GetGunAsset().rightHandOffset.position, smoothLayerAlpha);
            GetRightHandIK().Rotate(GetMasterPivot(), GetGunAsset().rightHandOffset.rotation, smoothLayerAlpha);
        }
    }
}
