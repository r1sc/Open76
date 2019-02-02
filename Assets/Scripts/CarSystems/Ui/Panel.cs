using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.CarSystems.Ui
{
    public class Panel
    {
        public ReferenceImage ReferenceImage { get; protected set; }

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
                    if (ReferenceImage.MainTexture != null)
                    {
                        newMaterial.mainTexture = ReferenceImage.MainTexture;
                    }
                    else
                    {
                        newMaterial.mainTexture = panelRenderer.material.mainTexture;
                        ReferenceImage.MainTexture = (Texture2D)newMaterial.mainTexture;
                    }

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
