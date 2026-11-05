using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NightShift
{
    internal static class NightShiftGamePreview
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Tools/Night Shift/Portrait Preview (1080 x 1920)")]
        private static void OpenPortraitPreview()
        {
            try
            {
                ApplyPortraitPreview();
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not set the portrait preview in this Unity version. " +
                    "In Game view, open the resolution dropdown, add a 1080 x 1920 Fixed Resolution, " +
                    "and lower Scale to fit. " + error.GetBaseException().Message);
            }
        }

        private static void ApplyPortraitPreview()
        {
            // Unity 2022.3 exposes Game view presets through its internal editor types.
            Assembly editor = typeof(EditorWindow).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes", throwOnError: true);
            Type sizeType = editor.GetType("UnityEditor.GameViewSize", throwOnError: true);
            Type gameViewType = editor.GetType("UnityEditor.GameView", throwOnError: true);
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            Type groupType = group.GetType();
            PropertyInfo kind = sizeType.GetProperty("sizeType");
            object fixedResolution = Enum.Parse(kind.PropertyType, "FixedResolution");
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int index = FindPortraitSize(group, sizeType, kind, fixedResolution, count);
            if (index == count)
            {
                var size = Activator.CreateInstance(sizeType, fixedResolution, Width, Height, "Night Shift Portrait");
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                sizesType.GetMethod("SaveToHDD").Invoke(sizes, null);
            }

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { index, null });
            // The minimum zoom fits the fixed target without magnifying it above 1x.
            float fit = (float)gameViewType.GetProperty("minScale", InstanceMembers).GetValue(gameView);
            gameViewType.GetMethod("SnapZoom", InstanceMembers).Invoke(gameView, new object[] { fit });
            gameView.Repaint();
        }

        private static int FindPortraitSize(object group, Type sizeType, PropertyInfo kind, object fixedResolution, int count)
        {
            MethodInfo sizeAt = group.GetType().GetMethod("GetGameViewSize");
            PropertyInfo width = sizeType.GetProperty("width");
            PropertyInfo height = sizeType.GetProperty("height");
            for (int index = 0; index < count; index++)
            {
                object size = sizeAt.Invoke(group, new object[] { index });
                if (Equals(kind.GetValue(size), fixedResolution) &&
                    (int)width.GetValue(size) == Width && (int)height.GetValue(size) == Height)
                    return index;
            }
            return count;
        }
    }
}
