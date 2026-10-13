using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class RectTransformAnchorsToSelf
    {
        [MenuItem("CONTEXT/RectTransform/Anchors To Self")]
        static void AnchorsToSelf(MenuCommand command)
        {
            var rt = (RectTransform)command.context;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;

            Undo.RecordObject(rt, "Anchors To Self");

            Vector2 parentSize = parent.rect.size;

            Vector2 newAnchorMin = new Vector2(
                (rt.anchorMin.x * parentSize.x + rt.offsetMin.x) / parentSize.x,
                (rt.anchorMin.y * parentSize.y + rt.offsetMin.y) / parentSize.y
            );

            Vector2 newAnchorMax = new Vector2(
                (rt.anchorMax.x * parentSize.x + rt.offsetMax.x) / parentSize.x,
                (rt.anchorMax.y * parentSize.y + rt.offsetMax.y) / parentSize.y
            );

            rt.anchorMin = newAnchorMin;
            rt.anchorMax = newAnchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}