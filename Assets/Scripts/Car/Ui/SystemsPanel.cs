﻿using System;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class SystemsPanel : Panel
    {
        public enum Systems
        {
            Engine,
            Brakes,
            Suspension,
            FrontArmor,
            LeftArmor,
            RightArmor,
            BackArmor,
            FrontChassis,
            LeftChassis,
            RightChassis,
            BackChassis,
            TireFL,
            TireFR,
            TireBL,
            TireBR
        };
        
        public SystemsPanel(Transform firstPersonTransform) : base(firstPersonTransform, "SYS", "zsy_.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }

            Array values = Enum.GetValues(typeof(Systems));
            foreach (Systems system in values)
            {
                SetSystemHealthGroup(system, 0, false);
            }

            ReferenceImage.UploadToGpu();
        }

        public Systems GetSystemForDamage(DamageType damageType, float angle)
        {
            switch (damageType)
            {
                case DamageType.Force:
                    if (angle < 90f)
                    {
                        return Systems.FrontChassis;
                    }
                    else if (angle < 180)
                    {
                        return Systems.RightChassis;
                    }
                    else if (angle < 270)
                    {
                        return Systems.BackChassis;
                    }
                    else
                    {
                        return Systems.LeftChassis;
                    }

                case DamageType.Projectile:
                    if (angle < 90f)
                    {
                        return Systems.FrontArmor;
                    }
                    else if (angle < 180)
                    {
                        return Systems.RightArmor;
                    }
                    else if (angle < 270)
                    {
                        return Systems.BackArmor;
                    }
                    else
                    {
                        return Systems.LeftArmor;
                    }

                default:
                    Debug.LogWarning("Unhandled damage type.");
                    throw new NotSupportedException();
            }
        }

        public void SetSystemHealthGroup(Systems system, int healthGroup, bool uploadToGpu)
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

            switch (system)
            {
                case Systems.Brakes:
                    spriteName = "brakes";
                    break;
                case Systems.Engine:
                    spriteName = "engine";
                    break;
                case Systems.Suspension:
                    spriteName = "suspen";
                    break;
                case Systems.FrontArmor:
                    spriteName = "farm";
                    break;
                case Systems.LeftArmor:
                    spriteName = "larm";
                    break;
                case Systems.RightArmor:
                    spriteName = "rarm";
                    break;
                case Systems.BackArmor:
                    spriteName = "barm";
                    break;
                case Systems.FrontChassis:
                    spriteName = "fchas";
                    break;
                case Systems.LeftChassis:
                    spriteName = "lchas";
                    break;
                case Systems.RightChassis:
                    spriteName = "rchas";
                    break;
                case Systems.BackChassis:
                    spriteName = "bchas";
                    break;
                case Systems.TireFL:
                    spriteName = "tire";
                    referenceName = "fltire";
                    break;
                case Systems.TireFR:
                    spriteName = "tire";
                    referenceName = "frtire";
                    break;
                case Systems.TireBL:
                    spriteName = "tire";
                    referenceName = "rltire";
                    break;
                case Systems.TireBR:
                    spriteName = "tire";
                    referenceName = "rrtire";
                    break;
                default:
                    Debug.LogWarning("Unknown system in SystemsPanel.");
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
