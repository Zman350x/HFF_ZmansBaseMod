using System;
using System.Collections;

namespace HffArchipelagoClient
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using HumanAPI;
    using TMPro;

    public class Portal : LevelObject
    {
        public LevelSource destination;
        public bool hasTriggered = false;

        private Renderer portalRenderer;

        private const int THREAD_GROUP_SIZE = 64;
        private ComputeShader boundsCompute;
        private ComputeBuffer vertexBuffer;
        private ComputeBuffer groupResultsBuffer;
        private ComputeBuffer finalBoundsBuffer;
        private int findKernel;
        private int reduceKernel;
        private int groupCount;

        public void LateUpdate()
        {
            Matrix4x4 worldToViewport = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
            boundsCompute.SetMatrix("_WorldToViewport", worldToViewport);
            boundsCompute.Dispatch(findKernel, groupCount, 1, 1);
            boundsCompute.Dispatch(reduceKernel, 1, 1, 1);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.tag != "Player")
                return;

            if (destination.IsUnlocked() && !hasTriggered)
            {
                if (destination.levelData.levelType == WorkshopItemSource.NotSpecified &&
                    destination.levelData.workshopId == ulong.MaxValue - 1)
                    HubWorld.LoadHubWorld();
                else
                    LoadingTools.LoadLevel(destination.levelData);

                hasTriggered = true;
            }
        }

        public void OnUnlock(bool IsUnlocked)
        {
            portalRenderer.material.SetFloat("_IsUnlocked", IsUnlocked ? 1.0f : 0.0f);
        }

        public void OnDestroy()
        {
            destination.UnregisterCallback(OnUnlock);
            vertexBuffer?.Release();
            groupResultsBuffer?.Release();
            finalBoundsBuffer?.Release();
        }

        public static GameObject CreatePortal(Transform parent, Vector3 position, Vector3 rotation, LevelSource destination)
        {
            GameObject portalParent = Instantiate(ResourceManager.PortalPrefab);
            portalParent.name = $"Portal to {destination.levelData.title}";
            portalParent.transform.SetParent(parent);
            portalParent.transform.localPosition = position;
            portalParent.transform.localEulerAngles = rotation;
            portalParent.transform.localScale = Vector3.one;

            GameObject portalBody = portalParent.GetComponentInChildren<BoxCollider>().gameObject;
            Portal portalComponent = portalBody.AddComponent<Portal>();
            portalComponent.destination = destination;
            destination.RegisterCallback(portalComponent.OnUnlock);

            portalComponent.portalRenderer = portalBody.GetComponent<Renderer>();
            if (destination.levelData.thumbnailTexture != null)
            {
                portalComponent.portalRenderer.material.SetTexture(Shader.PropertyToID("_MainTex"), destination.levelData.thumbnailTexture);
                portalComponent.portalRenderer.material.SetFloat("_MainTextureAspect", (float) destination.levelData.thumbnailTexture.width / (float) destination.levelData.thumbnailTexture.height);
            }
            if (ResourceManager.LockTexture != null)
            {
                portalComponent.portalRenderer.material.SetTexture(Shader.PropertyToID("_LockTex"), ResourceManager.LockTexture);
                portalComponent.portalRenderer.material.SetFloat("_LockTextureAspect", (float) ResourceManager.LockTexture.width / (float) ResourceManager.LockTexture.height);
            }
            portalComponent.portalRenderer.material.SetFloat("_IsUnlocked", destination.IsUnlocked() ? 1.0f : 0.0f);
            portalComponent.portalRenderer.material.color = Color.white;
            portalComponent.portalRenderer.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            portalComponent.portalRenderer.receiveShadows = false;
            portalComponent.portalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            portalComponent.boundsCompute = Instantiate(ResourceManager.BoundsCompute);
            Vector3[] vertices = portalBody.GetComponent<MeshFilter>().mesh.vertices;
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = portalBody.transform.TransformPoint(vertices[i]);
            }
            portalComponent.groupCount = Mathf.CeilToInt(vertices.Length / (float) THREAD_GROUP_SIZE);
            portalComponent.vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            portalComponent.vertexBuffer.SetData(vertices);

            portalComponent.groupResultsBuffer = new ComputeBuffer(portalComponent.groupCount, sizeof(float) * 4);
            portalComponent.finalBoundsBuffer = new ComputeBuffer(1, sizeof(float) * 4);

            portalComponent.findKernel = portalComponent.boundsCompute.FindKernel("FindBounds");
            portalComponent.boundsCompute.SetBuffer(portalComponent.findKernel, "_Vertices", portalComponent.vertexBuffer);
            portalComponent.boundsCompute.SetBuffer(portalComponent.findKernel, "_GroupResults", portalComponent.groupResultsBuffer);

            portalComponent.reduceKernel = portalComponent.boundsCompute.FindKernel("ReduceBounds");
            portalComponent.boundsCompute.SetBuffer(portalComponent.reduceKernel, "_GroupResults", portalComponent.groupResultsBuffer);
            portalComponent.boundsCompute.SetBuffer(portalComponent.reduceKernel, "_FinalBounds", portalComponent.finalBoundsBuffer);
            portalComponent.boundsCompute.SetInt("_GroupCount", portalComponent.groupCount);

            portalComponent.portalRenderer.material.SetBuffer("_BoundsBuffer", portalComponent.finalBoundsBuffer);

            GameObject portalText = portalParent.GetComponentInChildren<RectTransform>().gameObject;
            TextMeshPro textContent = portalText.AddComponent<TextMeshPro>();
            ArchipelagoClient.Instance.StartCoroutine(SetPortalText(textContent, destination.levelData.title));

            return portalParent;
        }

        private static IEnumerator SetPortalText(TextMeshPro textContent, string text)
        {
            yield return null;

            textContent.color = new Color(0.4549f, 0.4549f, 0.4549f, 1.0f);
            textContent.alignment = TextAlignmentOptions.Center;
            textContent.fontSizeMin = 2;
            textContent.fontSizeMax = 4;
            textContent.font = ResourceManager.goodDogFont;
            textContent.fontMaterial = ResourceManager.goodDogFontMaterial;
            textContent.fontMaterial.renderQueue = 3001;
            textContent.enableWordWrapping = true;
            textContent.enableAutoSizing = true;
            textContent.enableKerning = false;
            textContent.text = text;

            textContent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1.1f, 0.47f);
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Vector3? position = null;
            Vector3? rotation = null;

            Action<GameObject> onCreation = null;

            switch (scene.path)
            {
                // Main levels
                case "Assets/Scenes/Levels/Intro.unity":
                    position = new Vector3(-15.0f, 0.0f, -4.5f);
                    rotation = new Vector3(0.0f, 270.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Push.unity":
                    position = new Vector3(-11.25f, -0.3f, -8.5f);
                    rotation = new Vector3(0.0f, 0.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Carry.unity":
                    position = new Vector3(-12.0f, 0.0f, -4.0f);
                    rotation = new Vector3(0.0f, 270.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Climb.unity":
                    position = new Vector3(-12.0f, 0.0f, -15.0f);
                    rotation = new Vector3(0.0f, 240.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Break.unity":
                    position = new Vector3(9.0f, 3.0f, -4.5f);
                    rotation = new Vector3(0.0f, 180.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Siege.unity":
                    position = new Vector3(94.9f, -10.0f, -42.0f);
                    rotation = new Vector3(0.0f, 0.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/River.unity":
                    position = new Vector3(-97.9f, 0.95f, 320.45f);
                    rotation = new Vector3(5.0f, 230.0f, 354.0f);
					break;
                case "Assets/Scenes/Levels/Power.unity":
                    position = new Vector3(-57.0f, 0.0f, -72.8f);
                    rotation = new Vector3(0.0f, 180.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Aztec.unity":
                    position = new Vector3(66.1f, -0.9f, -60.3f);
                    rotation = new Vector3(0.0f, 278.0f, 0.0f);
					break;
                case "Assets/Scenes/Levels/Halloween.unity":
                    position = new Vector3(-0.8f, 14.04f, 40.0f);
                    rotation = new Vector3(0.0f, 0.0f, 0.0f);
					break;
                case "Assets/Scenes/SteamExperimental/Steam_merged.unity":
                    position = new Vector3(-35.3064f, 0.28f, -42.5f);
                    rotation = new Vector3(0.0f, 0.0f, 357.2f);
					break;
                case "Assets/Scenes/Experiments/IceExperimental/Ice_merged.unity":
                    position = new Vector3(58.0f, 24.61f, -98.0f);
                    rotation = new Vector3(0.4f, 283.0f, 355.5f);
					break;

                // Extra Dreams
                case "Assets/ContestLevels/ThermalAssets/Thermal.unity":
                    position = new Vector3(-7.1f, 1.21f, -3.3f);
                    rotation = new Vector3(358.0f, 240.0f, 0.0f);
                    break;
                case "Assets/ContestLevels/FactoryAssets/Factory.unity":
                    position = new Vector3(11.07f, 6.238f, -4.3f);
                    rotation = new Vector3(0.0f, 90.0f, 359.3f);
                    break;
                case "Assets/ContestLevels/GolfAssets/Golf.unity":
                    position = new Vector3(-19.0f, 10.52f, -16.2f);
                    rotation = new Vector3(0.0f, 245.0f, 0.0f);
                    break;
                case "Assets/ContestLevels/CityAssets/City.unity":
                    position = new Vector3(-128.0f, 56.95f, -26.25f);
                    rotation = new Vector3(0.0f, 270.0f, 0.0f);
					break;
                case "Assets/ContestLevels/ForestAssets/Forest.unity":
                    position = new Vector3(26.8f, 5.3f, -34.4f);
                    rotation = new Vector3(0.0f, 170.0f, 0.0f);
					break;
                case "Assets/ContestLevels/LabAssets/Lab.unity":
                    position = new Vector3(-192.54f, 1.5f, -88.7f);
                    rotation = new Vector3(0.0f, 180.0f, 0.0f);
					break;
                case "Assets/ContestLevels/LumberAssets/Lumber.unity":
                    position = new Vector3(6.0f, 0.0f, 7.5f);
                    rotation = new Vector3(0.0f, 90.0f, 0.0f);
					break;
                case "Assets/ContestLevels/RedRockAssets/RedRock.unity":
                    position = new Vector3(-40.35f, -2.795f, -38.43f);
                    rotation = new Vector3(0.0f, 270.0f, 0.0f);
					break;
                case "Assets/ContestLevels/TowerAssets/Tower.unity":
                    position = new Vector3(80.16f, 43.5194f, 53.45f);
                    rotation = new Vector3(0.0f, 167.0f, 0.0f);
					break;
                case "Assets/ContestLevels/MiniatureAssets/Miniature.unity":
                    position = new Vector3(-8.5f, 0.0f, -9.2f);
                    rotation = new Vector3(0.0f, 180.0f, 0.0f);
					break;
                case "Assets/ContestLevels/CopperWorldAssets/CopperWorld.unity":
                    position = new Vector3(-5.95f, 0.0f, -6.75f);
                    rotation = new Vector3(0.0f, 180.0f, 0.0f);
                    break;
                case "Assets/ContestLevels/NavalAssests/Naval_Ben.unity":
                    position = new Vector3(65.7f, 2.6427f, -5.6f);
                    rotation = new Vector3(0.0f, 259.0f, 0.0f);
					break;
                case "Assets/ContestLevels/UnderwaterAssets/OceanAdventure.unity":
                    position = new Vector3(-4.87f, 15.23f, 28.47f);
                    rotation = new Vector3(351.0f, 69.6124f, 350.681f);
					break;
                case "Assets/ContestLevels/DockyardAssets/Dockyard.unity":
                    position = new Vector3(-63.545f, -4.2234f, -43.23f);
                    rotation = new Vector3(0.0f, 180.0f, 0.0f);
                    onCreation = (GameObject portalObj) =>
                    {
                        portalObj.GetComponentInChildren<MeshCollider>().gameObject.GetComponent<Renderer>().material.renderQueue = 3000;
                    };
					break;
                case "Assets/ContestLevels/MuseumAssets/Museum.unity":
                    position = new Vector3(38.3f, 0.85f, 27.5f);
                    rotation = new Vector3(0.0f, 90.0f, 0.0f);
					break;
                case "Assets/ContestLevels/HikeAssets/Scenes/Hike.unity":
                    position = new Vector3(-111.0f, -35.025f, -129.0f);
                    rotation = new Vector3(0.0f, 331.4221f, 0.0f);
					break;
                case "Assets/ContestLevels/CandylandAssets/Candyland.unity":
                    position = new Vector3(-279.5f, 52.96f, -158.92f);
                    rotation = new Vector3(359.0f, 353.0f, 6.0f);
					break;
                case "Assets/ContestLevels/FacilityAssets/Facility.unity":
                    position = new Vector3(-62.75f, 31.0f, 67.25f);
                    rotation = new Vector3(0.0f, 235.0f, 0.0f);
					break;
                case "Assets/ContestLevels/Punk/SteamPunk.unity":
                    position = new Vector3(-62.78f, -4.585f, -42.0f);
                    rotation = new Vector3(0.0f, 315.0f, 0.0f);
					break;
                case "Assets/ContestLevels/VikingAssets/Viking.unity":
                    position = new Vector3(-75.66f, 0.53f, -32.46f);
                    rotation = new Vector3(0.0f, 215.0f, 2.0f);
                    break;

                // Lobbies
                case "Assets/WorkShop/Scenes/Levels/WorkshopLobby.unity":
                    position = new Vector3(-17.0f, -4.425f, -13.0f);
                    rotation = new Vector3(0.0f, 345.0f, 0.0f);
					break;
                case "Assets/Scenes/Lobby.unity":
                    position = new Vector3(14.5f, 3.0f, 49.5f);
                    rotation = new Vector3(0.0f, 0.0f, 0.0f);
					break;
                case "Assets/Scenes/Special/Xmas.unity":
                    position = new Vector3(2.3f, 0.08f, 7.45f);
                    rotation = new Vector3(0.0f, 328.0f, 0.0f);
					break;
                case "Assets/Scenes/Lobbies/Zodiac.unity":
                    position = new Vector3(-1.8f, -36.0f, 16.8f);
                    rotation = new Vector3(0.0f, 270.0f, 0.0f);
					break;
                default:
                    break;
            }

            if (position.HasValue && rotation.HasValue)
            {
                GameObject portalObj = CreatePortal(FindObjectOfType<Level>().gameObject.transform,
                                                    position.Value,
                                                    rotation.Value,
                                                    HubWorld.hubLevelSource);

                onCreation?.Invoke(portalObj);
            }
        }
    }
}
