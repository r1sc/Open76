using System;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public enum SystemType
    {
        Vehicle,
        FrontArmor,
        LeftArmor,
        RightArmor,
        BackArmor,
        FrontChassis,
        LeftChassis,
        RightChassis,
        BackChassis,
        Engine,
        Brakes,
        Suspension,
        TireFL,
        TireFR,
        TireBL,
        TireBR
    };

    public class SystemsPanel : Panel
    {
        public SystemsPanel(Transform firstPersonTransform) : base(firstPersonTransform, "SYS", "zsy_.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }

            Array values = Enum.GetValues(typeof(SystemType));
            foreach (SystemType system in values)
            {
                SetSystemHealthGroup(system, 0, false);
            }

            ReferenceImage.UploadToGpu();
        }

        public void SetSystemHealthGroup(SystemType systemType, int healthGroup, bool uploadToGpu)
        {
            string spriteName;
            string referenceName = null;

            string healthSuffix;
            switch (healthGroup)
            {
                case 0:
                    healthSuffix = "_off";
                    break;
                case 1:
                    healthSuffix = "_grn";
                    break;
                case 2:
                    healthSuffix = "_ylw";
                    break;
                case 3:
                    healthSuffix = "_red";
                    break;
                case 4:
                    healthSuffix = "_drk";
                    break;
                default:
                    Debug.LogWarning("Health group out of range in SystemsPanel.");
                    return;
            }

            switch (systemType)
            {
                case SystemType.Brakes:
                    spriteName = "brakes";
                    break;
                case SystemType.Engine:
                    spriteName = "engine";
                    break;
                case SystemType.Suspension:
                    spriteName = "suspen";
                    break;
                case SystemType.FrontArmor:
                    spriteName = "farm";
                    break;
                case SystemType.LeftArmor:
                    spriteName = "larm";
                    break;
                case SystemType.RightArmor:
                    spriteName = "rarm";
                    break;
                case SystemType.BackArmor:
                    spriteName = "barm";
                    break;
                case SystemType.FrontChassis:
                    spriteName = "fchas";
                    break;
                case SystemType.LeftChassis:
                    spriteName = "lchas";
                    break;
                case SystemType.RightChassis:
                    spriteName = "rchas";
                    break;
                case SystemType.BackChassis:
                    spriteName = "bchas";
                    break;
                case SystemType.TireFL:
                    spriteName = "tire";
                    referenceName = "fltire";
                    break;
                case SystemType.TireFR:
                    spriteName = "tire";
                    referenceName = "frtire";
                    break;
                case SystemType.TireBL:
                    spriteName = "tire";
                    referenceName = "rltire";
                    break;
                case SystemType.TireBR:
                    spriteName = "tire";
                    referenceName = "rrtire";
                    break;
                default:
                    Debug.LogWarning("Unknown systemType in SystemsPanel.");
                    return;
            }

            if (referenceName == null)
            {
                referenceName = spriteName;
            }

            I76Sprite sprite = SpriteManager.GetSprite("zsye.map", spriteName + healthSuffix);
            if (sprite != null)
            {
                ReferenceImage.ApplySprite(referenceName, sprite, uploadToGpu);
            }
        }
    }
}
