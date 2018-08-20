using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class Panel
    {
        public ReferenceImage ReferenceImage { get; private set; }

        protected SpriteManager SpriteManager;
        private static Material _panelMaterialPrefab;

        public Panel(Transform firstPersonTransform, string firstPersonObjectName, string referenceMap)
        {
            SpriteManager = SpriteManager.Instance;
            ReferenceImage = SpriteManager.LoadReferenceImage(referenceMap);

            Transform transformObj = firstPersonTransform;
            bool foundPanel = false;
            foreach (Transform child in transformObj)
            {
                if (child.name.Contains(firstPersonObjectName))
                {
                    if (_panelMaterialPrefab == null)
                    {
                        _panelMaterialPrefab = Resources.Load<Material>("Materials/PanelMaterial");
                    }

                    MeshRenderer panelRenderer = child.GetComponent<MeshRenderer>();
                    Material newMaterial = Object.Instantiate(_panelMaterialPrefab);
                    newMaterial.mainTexture = ReferenceImage.MainTexture;
                    panelRenderer.material = newMaterial;
                    foundPanel = true;
                    break;
                }
            }

            if (!foundPanel)
            {
                Debug.LogWarningFormat("Failed to find panel with name '{0}' in vehicle's FirstPerson hierarchy.", firstPersonObjectName);
            }
        }
    }
}
