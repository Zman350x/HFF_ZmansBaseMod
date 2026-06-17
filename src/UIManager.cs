using System.Collections.Generic;

namespace ZmanBase
{
    using UnityEngine;
    using TMPro;

    public static class UIManager
    {
        public enum Corner
        {
            TOP_LEFT = 0,
            TOP_RIGHT = 1,
            BOTTOM_LEFT = 2,
            BOTTOM_RIGHT = 3
        }

        private static Canvas menuCanvas;

        static UIManager()
        {
            menuCanvas = GameObject.Find("Menu").GetComponent<Canvas>();
        }

        public class CornerText
        {
            private static ArrayByEnum<List<CornerText>, Corner> allCornerText;

            static CornerText()
            {
                allCornerText = new ArrayByEnum<List<CornerText>, Corner>();

                allCornerText[Corner.TOP_LEFT] = new List<CornerText>();
                allCornerText[Corner.TOP_RIGHT] = new List<CornerText>();
                allCornerText[Corner.BOTTOM_LEFT] = new List<CornerText>();
                allCornerText[Corner.BOTTOM_RIGHT] = new List<CornerText>();
            }

            private GameObject gameObject;
            private TextMeshProUGUI textContent;
            private RectTransform textRect;

            public readonly Corner corner;
            public readonly int priority;

            public CornerText(string name, Corner corner = Corner.BOTTOM_LEFT, int priority = 0, int fontSize = 50) : this(name, corner, priority, fontSize, Color.white, ResourceManager.bloggerSansBoldFont) {}

            public CornerText(string name, Corner corner, int priority, int fontSize, Color color, ResourceManager.HffFont font)
            {
                gameObject = new GameObject(name);
                gameObject.transform.parent = GameObject.Find("Menu").transform;
                gameObject.AddComponent<CanvasRenderer>();

                textContent = gameObject.AddComponent<TextMeshProUGUI>();
                textContent.color = color;
                textContent.fontSize = fontSize;
                textContent.font = font.asset;
                textContent.fontMaterial = font.material;
                textContent.enableWordWrapping = false;

                textRect = gameObject.GetComponent<RectTransform>();
                textRect.sizeDelta = Vector2.zero;

                switch (corner)
                {
                    case Corner.TOP_LEFT:
                        textContent.alignment = TextAlignmentOptions.TopLeft;
                        textRect.anchorMin = Vector2.up;
                        textRect.anchorMax = Vector2.up;
                        break;
                    case Corner.TOP_RIGHT:
                        textContent.alignment = TextAlignmentOptions.TopRight;
                        textRect.anchorMin = Vector2.one;
                        textRect.anchorMax = Vector2.one;
                        break;
                    default:
                    case Corner.BOTTOM_LEFT:
                        textContent.alignment = TextAlignmentOptions.BaselineLeft;
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.zero;
                        break;
                    case Corner.BOTTOM_RIGHT:
                        textContent.alignment = TextAlignmentOptions.BaselineRight;
                        textRect.anchorMin = Vector2.right;
                        textRect.anchorMax = Vector2.right;
                        break;
                }

                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;
                gameObject.layer = LayerMask.NameToLayer("UI");

                this.priority = priority;
                this.corner = corner;

                bool inserted = false;
                for (int i = 0; i < allCornerText[corner].Count; ++i)
                {
                    if (priority > allCornerText[corner][i].priority)
                    {
                        allCornerText[corner].Insert(i, this);
                        inserted = true;
                        break;
                    }
                }
                if (!inserted)
                {
                    allCornerText[corner].Add(this);
                }

                UpdatePositions(corner);
            }

            public void Delete()
            {
                Object.Destroy(gameObject);
                allCornerText[corner].Remove(this);
                UpdatePositions(corner);
            }

            public void SetText(string text)
            {
                textContent.text = text;
            }

            public static void UpdatePositions(Corner corner)
            {
                int direction;
                Vector3 coords;

                switch (corner)
                {
                    case Corner.TOP_LEFT:
                        coords = new Vector3(5f, -5f, 0f);
                        direction = -1;
                        break;
                    case Corner.TOP_RIGHT:
                        coords = new Vector3(-5f, -5f, 0f);
                        direction = -1;
                        break;
                    default:
                    case Corner.BOTTOM_LEFT:
                        coords = new Vector3(5f, 9f, 0f);
                        direction = 1;
                        break;
                    case Corner.BOTTOM_RIGHT:
                        coords = new Vector3(-5f, 9f, 0f);
                        direction = 1;
                        break;
                }

                foreach (CornerText cornerText in allCornerText[corner])
                {
                    cornerText.textRect.anchoredPosition3D = coords;
                    coords.y += direction * cornerText.textContent.fontSize;
                }
            }
        }
    }
}
