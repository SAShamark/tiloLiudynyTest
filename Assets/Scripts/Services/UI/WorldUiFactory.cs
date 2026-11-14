using TMPro;
using UnityEngine;

namespace Services.UI
{
    public static class WorldUiFactory
    {
        public static TextMeshPro CreateWorldLabel(Transform parent, string name, Vector3 localPosition, float scale,
            TMP_FontAsset font)
        {
            var label = new GameObject(name).AddComponent<TextMeshPro>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localScale = Vector3.one * scale;
            label.font = font;
            label.fontSize = 2.3f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(4f, 1f);
            return label;
        }
    }
}
