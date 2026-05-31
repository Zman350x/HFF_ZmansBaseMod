using System.Linq;

namespace HffArchipelagoClient
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using TMPro;

    public static class MenuButtonTools
    {
        public static void AddButton(string menu, string name, string text, int order, UnityAction callback)
        {
            GameObject menuRoot = GameObject.Find($"/Game(Clone)/Menu/MenuSystem/{menu}(Clone)");
            GameObject menuButtons = menuRoot.GetComponentInChildren<VerticalLayoutGroup>(true).gameObject;
            GameObject button = new GameObject(name, typeof(RectTransform),
                                                     typeof(CanvasRenderer),
                                                     typeof(Image),
                                                     typeof(Button),
                                                     typeof(LayoutElement));

            button.layer = LayerMask.NameToLayer("UI");
            button.transform.SetParent(menuButtons.transform);
            button.transform.localScale = Vector3.one;
            button.transform.localPosition = Vector3.zero;
            button.transform.localRotation = Quaternion.identity;
            button.GetComponent<LayoutElement>().preferredHeight = 70;
            button.transform.SetSiblingIndex(order);

            button.GetComponent<Button>().colors = new ColorBlock {
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

            buttonText.transform.SetParent(button.transform);
            buttonText.transform.localRotation = Quaternion.identity;
            buttonText.transform.localScale = Vector3.one;
            buttonText.layer = LayerMask.NameToLayer("UI");

            TextMeshProUGUI textContent = buttonText.GetComponent<TextMeshProUGUI>();
            textContent.color = Color.black;
            textContent.fontSize = 40;
            textContent.fontSizeMax = 40;
            textContent.font = ResourceManager.menuFont;
            textContent.fontMaterial = ResourceManager.menuFontMaterial;
            textContent.enableWordWrapping = false;
            textContent.enableAutoSizing = true;
            textContent.enableKerning = false;
            textContent.alignment = TextAlignmentOptions.Left;
            textContent.text = text;

            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition3D = Vector3.zero;
            textRect.offsetMax = new Vector2(-20.0f, 0.0f);
            textRect.offsetMin = new Vector2(20.0f, 0.0f);
        }

        public static void DestroyButton(string menu, string name)
        {
            GameObject menuRoot = GameObject.Find($"/Game(Clone)/Menu/MenuSystem/{menu}(Clone)");
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

        public static void EnableDisableButton(string menu, string name, bool enable)
        {
            GameObject menuRoot = GameObject.Find($"/Game(Clone)/Menu/MenuSystem/{menu}(Clone)");
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
    }
}
