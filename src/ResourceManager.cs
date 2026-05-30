using System.Linq;
using System.Reflection;
using System.IO;

namespace HffArchipelagoClient
{
    using UnityEngine;
    using TMPro;

    public static class ResourceManager
    {
        public static GameObject PortalPrefab { get; private set; }
        public static ComputeShader BoundsCompute { get; private set; }

        public static TMP_FontAsset menuFont;
        public static Material menuFontMaterial;

        public static TMP_FontAsset goodDogFont;
        public static Material goodDogFontMaterial;

        public static Texture2D HubWorldThumbnail;
        public static Texture2D LockTexture;

        static ResourceManager()
        {
            // Load AssetBundle
            string bundleName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("archipelago"));
            Stream bundleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(bundleName);
            AssetBundle bundle = AssetBundle.LoadFromStream(bundleStream);

            // AssetBundle Assets
            bundle.LoadAsset<Shader>("assets/textmesh pro/required/shaders/tmp_sdf-surface.shader");
            bundle.LoadAsset<Shader>("assets/shaders/screenspacecover.shader");
            BoundsCompute = bundle.LoadAsset<ComputeShader>("assets/shaders/computebounds.compute");
            goodDogFontMaterial = bundle.LoadAsset<Material>("assets/fonts/gooddog sdf.mat");
            PortalPrefab = bundle.LoadAsset<GameObject>("assets/portal.prefab");

            // Fonts
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            Material[] fontMaterials = Resources.FindObjectsOfTypeAll<Material>();

            menuFont = fonts.Where(font => font.name == "Menu SDF").First();
            menuFontMaterial = fontMaterials.Where(material => material.name == "Menu SDF Material").First();
            goodDogFont = fonts.Where(font => font.name == "GoodDog SDF").First();

            // Load Hub World Thumbnail
            string hubWorldThumbnailName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("hub_world_thumbnail.png"));
            Stream hubWorldThumbnailStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(hubWorldThumbnailName);
            byte[] hubWorldThumbnailBytes = new byte[hubWorldThumbnailStream.Length];
            hubWorldThumbnailStream.Read(hubWorldThumbnailBytes, 0, hubWorldThumbnailBytes.Length);
            HubWorldThumbnail = new Texture2D(1, 1);
            HubWorldThumbnail.LoadImage(hubWorldThumbnailBytes);
            HubWorldThumbnail.name = "HubWorldThumbnail";

            // Load Hub Lock Texture
            string lockTextureName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("lock.png"));
            Stream lockTextureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(lockTextureName);
            byte[] lockTextureBytes = new byte[lockTextureStream.Length];
            lockTextureStream.Read(lockTextureBytes, 0, lockTextureBytes.Length);
            LockTexture = new Texture2D(1, 1);
            LockTexture.LoadImage(lockTextureBytes);
            LockTexture.name = "LockTexture";
        }
    }
}
