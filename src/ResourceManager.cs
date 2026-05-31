using System.Linq;

namespace ZmanBase
{
    using UnityEngine;
    using TMPro;

    public static class ResourceManager
    {
        public class HffFont
        {
            public TMP_FontAsset asset;
            public Material material;
        }

        public static HffFont menuFont = new HffFont();
        public static HffFont goodDogFont = new HffFont();
        public static HffFont bloggerSansBoldFont = new HffFont();
        public static HffFont arialFont = new HffFont();
        public static HffFont liberationSansFont = new HffFont();
        public static HffFont xb1Ps4ControllerSymbolsFont = new HffFont();
        public static HffFont nintendoControllerSymbolsFont = new HffFont();

        static ResourceManager()
        {
            // Fonts
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            Material[] fontMaterials = Resources.FindObjectsOfTypeAll<Material>();

            menuFont.asset = fonts.Where(font => font.name == "Menu SDF").First();
            menuFont.material = fontMaterials.Where(material => material.name == "Menu SDF Material" && material.mainTexture.name == "Menu SDF Atlas").First(); // There are multiple "Menu SDF Material" resources

            goodDogFont.asset = fonts.Where(font => font.name == "GoodDog SDF").First();
            goodDogFont.material = fontMaterials.Where(material => material.name == "GoodDog Material").First();

            bloggerSansBoldFont.asset = fonts.Where(font => font.name == "Blogger_Sans-Bold SDF").First();
            bloggerSansBoldFont.material = fontMaterials.Where(material => material.name == "Blogger_Sans-Bold SDF Instruction").First();

            arialFont.asset = fonts.Where(font => font.name == "ARIALUNI SDF").First();
            arialFont.material = fontMaterials.Where(material => material.name == "ARIALUNI SDF Material").First();

            liberationSansFont.asset = fonts.Where(font => font.name == "LiberationSans SDF").First();
            liberationSansFont.material = fontMaterials.Where(material => material.name == "LiberationSans SDF Material").First();

            xb1Ps4ControllerSymbolsFont.asset = fonts.Where(font => font.name == "XB1PS4JoypadsSDF").First();
            xb1Ps4ControllerSymbolsFont.material = fontMaterials.Where(material => material.name == "XB1PS4JoypadsSDF Material").First();

            nintendoControllerSymbolsFont.asset = fonts.Where(font => font.name == "nintendo_ext_LE_003 SDF").First();
            nintendoControllerSymbolsFont.material = fontMaterials.Where(material => material.name == "nintendo_ext_LE_003 SDF Material").First();
        }
    }
}
