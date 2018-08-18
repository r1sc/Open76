using System;
using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car
{
    public class SystemsPanel : MonoBehaviour
    {
        private ReferenceImage _referenceImage;
        private SpriteManager _spriteManager;

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
        
        public void InitSystems()
        {
            _spriteManager = SpriteManager.Instance;

            _referenceImage = _spriteManager.LoadReferenceImage("zsy_.map");
            if (_referenceImage == null)
            {
                return;
            }

            Transform transformObj = transform;
            bool foundPanel = false;
            foreach (Transform child in transformObj)
            {
                if (child.name.Contains("SYS"))
                {
                    MeshRenderer panelRenderer = child.GetComponent<MeshRenderer>();
                    panelRenderer.material.mainTexture = _referenceImage.MainTexture;
                    foundPanel = true;
                    break;
                }
            }

            if (!foundPanel)
            {
                Debug.LogWarning("Failed to find system panel in vehicle's FirstPerson hierarchy.");
            }

            Array values = Enum.GetValues(typeof(Systems));
            foreach (Systems system in values)
            {
                SetSystemHealthGroup(system, 0, false);
            }

            _referenceImage.UploadToGpu();
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

            I76Sprite sprite = _spriteManager.GetSprite("zsye.map", spriteName + healthSuffix);
            if (sprite != null)
            {
                _referenceImage.ApplySprite(referenceName, sprite, uploadToGpu);
            }
        }
    }
}
