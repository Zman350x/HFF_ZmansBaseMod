using System;

namespace HffArchipelagoClient
{
    using UnityEngine;
    using HumanAPI;
    using UnityEngine.Events;

    public static class HubWorld
    {

        private static GameObject hubLevelObject;
        private static Level hubLevel;

        private static WorkshopItemSource previousLevelType;
        private static int previousLevelNumber;
        private static Vector3? portalSpawnpoint;

        public static readonly LevelSource hubLevelSource = new LevelSource(
            new WorkshopLevelMetadata {
                itemType = WorkshopItemType.Level,
                workshopId = ulong.MaxValue - 1,
                title = "Archipelago Hub",
                levelType = WorkshopItemSource.NotSpecified,
                cachedThumbnail = ResourceManager.HubWorldThumbnail,
                folder = "none"
            }, true);

        public static void LoadHubWorld()
        {
            previousLevelType = Game.instance.currentLevelType;
            previousLevelNumber = Game.instance.currentLevelNumber;

            LoadingTools.LoadLevel(hubLevelSource.levelData);
        }

        public static void OnHubWorldLoaded()
        {
            // Create and deactivate a new root game object
            hubLevelObject = new GameObject("Level");
            hubLevelObject.SetActive(false);

            // Add level component; must be done while the game object is disabled
            hubLevel = hubLevelObject.AddComponent<Level>();

            // Add things to the scene
            AddLighting();
            AddGeometry();
            AddPortals();
            AddRequiredLevelObjects(); // Must add after portals

            // Activate the level game object only after everything is setup
            hubLevelObject.SetActive(true);
        }

        private static void AddRequiredLevelObjects()
        {
            GameObject spawnpoint = new GameObject("Spawnpoint");
            spawnpoint.transform.SetParent(hubLevelObject.transform);
            if (portalSpawnpoint.HasValue)
                spawnpoint.transform.position = portalSpawnpoint.Value;
            else
                spawnpoint.transform.localPosition = Vector3.zero;
            spawnpoint.transform.localRotation = Quaternion.identity;
            spawnpoint.transform.localScale = Vector3.one;
            spawnpoint.AddComponent<Checkpoint>().number = 0;
            hubLevel.spawnPoint = spawnpoint.transform;

            GameObject checkpoint = new GameObject("Checkpoint", typeof(BoxCollider));
            checkpoint.transform.SetParent(hubLevelObject.transform);
            checkpoint.transform.localPosition = Vector3.zero;
            checkpoint.transform.localRotation = Quaternion.identity;
            checkpoint.transform.localScale = Vector3.one;
            checkpoint.GetComponent<BoxCollider>().center = new Vector3(0.0f, 1.0f, 0.0f);
            checkpoint.GetComponent<BoxCollider>().size = new Vector3(70.0f, 2.0f, 70.0f);
            checkpoint.GetComponent<BoxCollider>().isTrigger = true;
            checkpoint.AddComponent<Checkpoint>().number = 1;

            GameObject fallTrigger = new GameObject("FallTrigger", typeof(BoxCollider));
            fallTrigger.transform.SetParent(hubLevelObject.transform);
            fallTrigger.transform.localPosition = new Vector3(0.0f, -30.0f, 0.0f);
            fallTrigger.transform.localRotation = Quaternion.identity;
            fallTrigger.transform.localScale = Vector3.one;
            fallTrigger.GetComponent<BoxCollider>().center = new Vector3(0.0f, -20.0f, 0.0f);
            fallTrigger.GetComponent<BoxCollider>().size = new Vector3(400.0f, 50.0f, 400.0f);
            fallTrigger.GetComponent<BoxCollider>().isTrigger = true;
            fallTrigger.AddComponent<FallTrigger>().OnFall = new UnityEvent();
        }

        private static void AddLighting()
        {
            GameObject directionalLight = new GameObject("DirectionalLight", typeof(Light));
            directionalLight.transform.SetParent(hubLevelObject.transform);
            directionalLight.transform.localPosition = new Vector3(0.0f, 3.0f, 0.0f);
            directionalLight.transform.localEulerAngles = new Vector3(50.0f, 330.0f, 0.0f);
            directionalLight.transform.localScale = Vector3.one;
            directionalLight.GetComponent<Light>().shadows = LightShadows.Soft;
            directionalLight.GetComponent<Light>().type = LightType.Directional;
        }

        private static void AddGeometry()
        {
            GameObject tempFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            tempFloor.name = "TempFloor";
            tempFloor.transform.SetParent(hubLevelObject.transform);
            Renderer tempFloorRenderer = tempFloor.GetComponent<Renderer>();
            StandardShaderUtils.ChangeRenderMode(tempFloorRenderer.material, StandardShaderUtils.BlendMode.Opaque);
            tempFloorRenderer.material.SetColor("_Color", new Color(0.0f, 0.4f, 0.0f, 1.0f));
            tempFloor.transform.localPosition = Vector3.zero;
            tempFloor.transform.localRotation = Quaternion.identity;
            tempFloor.transform.localScale = Vector3.one * 7;
            tempFloor.GetComponent<Collider>().enabled = true;
        }

        private static void AddPortals()
        {
            portalSpawnpoint = null;

            GameObject portalParent = new GameObject("Portals");
            portalParent.transform.SetParent(hubLevelObject.transform);
            portalParent.transform.localPosition = Vector3.zero;
            portalParent.transform.rotation = Quaternion.identity;
            portalParent.transform.localScale = Vector3.one;

            int portals = LevelSource.EnabledLevels.Count;

            for (int i = 0; i < portals; ++i)
            {
                double angle = (2 * Math.PI * i) / portals;
                Vector3 position = new Vector3((float) Math.Sin(angle), 0.0f, (float) Math.Cos(angle));
                Vector3 rotation = new Vector3(0.0f, (float) (Mathf.Rad2Deg * angle), 0.0f);
                GameObject portal = Portal.CreatePortal(portalParent.transform, position * 30, rotation, LevelSource.EnabledLevels[i]);

                if (LevelSource.EnabledLevels[i].levelData.levelType == previousLevelType &&
                    (LevelSource.EnabledLevels[i].levelData.workshopId == (ulong) previousLevelNumber ||
                    (LevelSource.EnabledLevels[i].levelData.workshopId == 11 && previousLevelNumber == 12 &&
                    previousLevelType == WorkshopItemSource.BuiltIn))) // Exception for Ice/Reprise
                {
                    portalSpawnpoint = portal.transform.Find("PortalSpawnpoint").position;
                }
            }
        }
    }
}
