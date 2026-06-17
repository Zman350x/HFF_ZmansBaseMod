namespace ZmanBase
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using TMPro;

    public static class MenuTools
    {
        public const float buttonAnchorVerticalOffset = 0.1043f; // Found via trial and error in-game

        public static void AddButton(string menu, string name, string text, int order, UnityAction callback, bool isBuiltIn = true)
        {
            GameObject menuRoot = GetMenuObjectByName(menu, isBuiltIn);
            GameObject menuButtons = menuRoot.GetComponentInChildren<VerticalLayoutGroup>(true).gameObject;
            GameObject button = new GameObject(name, typeof(RectTransform),
                                                     typeof(CanvasRenderer),
                                                     typeof(Image),
                                                     typeof(Button),
                                                     typeof(LayoutElement));

            button.layer = LayerMask.NameToLayer("UI");
            button.transform.SetParent(menuButtons.transform);
            HelperFunctions.ResetRectTransform(button.transform as RectTransform);
            button.GetComponent<LayoutElement>().preferredHeight = 70;
            button.transform.SetSiblingIndex(order);

            button.GetComponent<Button>().colors = new ColorBlock
            {
                normalColor = new Color(1.0f, 1.0f, 1.0f, 0.2549f),
                highlightedColor = new Color(1.0f, 1.0f, 1.0f, 1.0f),
                pressedColor = new Color(0.7843f, 0.7843f, 0.7843f, 1.0f),
                disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
                colorMultiplier = 1.0f,
                fadeDuration = 0.1f
            };

            button.GetComponent<Button>().onClick.AddListener(callback);

            GameObject buttonText = new GameObject("TextMeshPro Text", typeof(RectTransform),
                                                                       typeof(CanvasRenderer),
                                                                       typeof(TextMeshProUGUI));

            buttonText.layer = LayerMask.NameToLayer("UI");

            RectTransform textRect = buttonText.transform as RectTransform;
            textRect.SetParent(button.transform);
            HelperFunctions.ResetRectTransform(textRect);
            textRect.offsetMax = new Vector2(-20.0f, 0.0f);
            textRect.offsetMin = new Vector2(20.0f, 0.0f);

            TextMeshProUGUI textContent = buttonText.GetComponent<TextMeshProUGUI>();
            textContent.color = Color.black;
            textContent.fontSize = 40;
            textContent.fontSizeMax = 40;
            textContent.font = ResourceManager.menuFont.asset;
            textContent.fontMaterial = ResourceManager.menuFont.material;
            textContent.enableWordWrapping = false;
            textContent.enableAutoSizing = true;
            textContent.enableKerning = false;
            textContent.alignment = TextAlignmentOptions.Left;
            textContent.text = text;
        }

        public static void DestroyButton(string menu, string name, bool isBuiltIn = true)
        {
            GameObject menuRoot = GetMenuObjectByName(menu, isBuiltIn);
            GameObject menuButtons = menuRoot.GetComponentInChildren<VerticalLayoutGroup>(true).gameObject;

            foreach (Transform child in menuButtons.transform)
            {
                if (child.name == name)
                {
                    GameObject.Destroy(child.gameObject);
                    return;
                }
            }
        }

        public static void EnableDisableButton(string menu, string name, bool enable, bool isBuiltIn = true)
        {
            GameObject menuRoot = GetMenuObjectByName(menu, isBuiltIn);
            GameObject menuButtons = menuRoot.GetComponentInChildren<VerticalLayoutGroup>(true).gameObject;

            foreach (Transform child in menuButtons.transform)
            {
                if (child.name == name)
                {
                    child.gameObject.SetActive(enable);
                    return;
                }
            }
        }

        public static void MoveButtons(string menu, float xOffset, float yOffset, bool isBuiltIn = true)
        {
            GameObject menuRoot = GetMenuObjectByName(menu, isBuiltIn);
            GameObject menuButtons = menuRoot.GetComponentInChildren<VerticalLayoutGroup>(true).gameObject;

            RectTransform transform = menuButtons.transform as RectTransform;
            if (transform is null)
                return;

            Vector2 offset = new Vector2(xOffset, yOffset);
            transform.anchorMax += offset;
            transform.anchorMin += offset;
        }

        public static void TransitionForward(MenuTransition from, MenuTransition to, float fadeOutTime = 0.3f, float fadeInTime = 0.3f)
        {
            MenuSystem.instance.FadeOutForward(from, fadeOutTime);
            MenuSystem.instance.FadeInForward(to, fadeInTime);
        }

        public static void TransitionBack(MenuTransition from, MenuTransition to, float fadeOutTime = 0.3f, float fadeInTime = 0.3f)
        {
            MenuSystem.instance.FadeOutBack(from, fadeOutTime);
            MenuSystem.instance.FadeInBack(to, fadeInTime);
        }

        public static GameObject GetMenuObjectByName(string menu, bool isBuiltIn = true)
        {
            return GameObject.Find($"/Game(Clone)/Menu/MenuSystem/{menu}" + (isBuiltIn ? "(Clone)" : ""));
        }

        public class BasicCustomMenu : MenuTransition
        {
            public GameObject parentMenuObject;
            public MenuTransition parentMenu;
            public bool blurBackground;

            public GameObject menuObject;

            public override void ApplyMenuEffects()
            {
                if (blurBackground)
                    MenuCameraEffects.FadeInPauseMenu();
            }

            public static BasicCustomMenu Create(string parentMenu, string name, string title, bool isParentBuiltIn = true, bool blurBackground = true)
            {
                GameObject menuObject = new GameObject(name, typeof(RectTransform),
                                                             typeof(Canvas),
                                                             typeof(CanvasGroup),
                                                             typeof(UnityEngine.UI.GraphicRaycaster),
                                                             typeof(AutoNavigation),
                                                             typeof(BasicCustomMenu));
                menuObject.SetActive(false);

                BasicCustomMenu menu = menuObject.GetComponent<BasicCustomMenu>();

                menu.parentMenuObject = GetMenuObjectByName(parentMenu, isParentBuiltIn);
                menu.parentMenu = menu.parentMenuObject.GetComponent<MenuTransition>();
                menu.blurBackground = blurBackground;

                menuObject.layer = LayerMask.NameToLayer("UI");

                RectTransform menuTransform = menuObject.transform as RectTransform;
                menuTransform.SetParent(MenuSystem.instance.transform);
                HelperFunctions.ResetRectTransform(menuTransform);

                Canvas canvas = menuObject.GetComponent<Canvas>();
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                  AdditionalCanvasShaderChannels.Normal |
                                                  AdditionalCanvasShaderChannels.Tangent;
                
                menuObject.GetComponent<AutoNavigation>().direction = NavigationDirection.Vertical;

                GameObject menuPanel = new GameObject("MenuPanel", typeof(RectTransform));
                menuPanel.layer = LayerMask.NameToLayer("UI");
                RectTransform panelTransform = menuPanel.transform as RectTransform;
                panelTransform.SetParent(menuTransform);
                HelperFunctions.ResetRectTransform(panelTransform);
                panelTransform.anchorMin = new Vector2(0.1f, 0.1f);
                panelTransform.anchorMax = new Vector2(1.0f, 0.9f);

                GameObject titleObject = new GameObject("Title", typeof(RectTransform),
                                                                 typeof(CanvasRenderer),
                                                                 typeof(TextMeshProUGUI));
                titleObject.layer = LayerMask.NameToLayer("UI");
                RectTransform titleTransform = titleObject.transform as RectTransform;
                titleTransform.SetParent(panelTransform);
                HelperFunctions.ResetRectTransform(titleTransform);
                
                TextMeshProUGUI titleContent = titleObject.GetComponent<TextMeshProUGUI>();
                titleContent.color = Color.white;
                titleContent.fontSize = 80;
                titleContent.fontSizeMax = 72;
                titleContent.font = ResourceManager.menuFont.asset;
                titleContent.fontMaterial = ResourceManager.menuFont.material;
                titleContent.enableWordWrapping = false;
                titleContent.enableAutoSizing = false;
                titleContent.enableKerning = false;
                titleContent.alignment = TextAlignmentOptions.TopLeft;
                titleContent.text = title;
                
                GameObject buttons = new GameObject("Buttons", typeof(RectTransform),
                                                               typeof(VerticalLayoutGroup),
                                                               typeof(ContentSizeFitter));
                buttons.layer = LayerMask.NameToLayer("UI");
                RectTransform buttonTransform = buttons.transform as RectTransform;
                buttonTransform.SetParent(panelTransform);
                HelperFunctions.ResetRectTransform(buttonTransform);
                buttonTransform.anchorMax = new Vector2(0.35f, 0.6f);
                buttonTransform.anchorMin = new Vector2(0.0f, 0.6f);

                buttons.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                AddButton(name, "BackButton", "BACK", 100, menu.BackClick, false);

                return menu;
            }

            public void BackClick()
            {
                if (!MenuSystem.CanInvoke)
                    return;

                MenuTools.TransitionBack(this, parentMenu);
            }

            public override void OnBack()
            {
                BackClick();
            }
        }
    }
}
