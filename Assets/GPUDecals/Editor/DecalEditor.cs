using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yunchang
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DecalRenderer))]
    public class DecalEditor : Editor
    {
        [MenuItem("GameObject/YunChang/Decal", false, 10)]
        public static void CreateDecalObject(MenuCommand menuCommand)
        {
            DecalsManager dm = DecalsManager.Instance;
            if (dm == null)
                dm = new GameObject("Decals Manager", typeof(DecalsManager)).GetComponent<DecalsManager>();

            GameObject go = new GameObject();
            go.name = "New Decal";
            go.AddComponent<BoxCollider>();
            go.AddComponent<DecalRenderer>();
            go.transform.SetParent(dm.transform, false);
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        private static Sprite[] _Sprites;
        private void OnEnable()
        {
            if (DecalsManager.Instance.decalBaseTexture != null)
                _Sprites = GetSprites(DecalsManager.Instance.decalBaseTexture);
        }
        private void OnDisable()
        {
            _Sprites = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var sprite = serializedObject.FindProperty("sprite");
            DrawDefaultInspector();
            if (_Sprites != null)
            {
                sprite.objectReferenceValue = DrawSpriteList(sprite.objectReferenceValue as Sprite, _Sprites);
            }
            if (serializedObject.ApplyModifiedProperties())
            {

            }
        }

        public Sprite DrawSpriteList(Sprite sprite, Sprite[] list)
        {
            foreach (var item in DrawGrid(list))
            {
                var selected = DrawSprite(item.Value, item.Key, item.Key == sprite);
                if (selected) sprite = item.Key;
            }
            return sprite;
        }

        private static Rect ToRect01(Rect rect, Texture2D texture)
        {
            rect.x /= texture.width;
            rect.y /= texture.height;
            rect.width /= texture.width;
            rect.height /= texture.height;
            return rect;
        }

        private static bool DrawSprite(Rect rect, Sprite sprite, bool isSelected)
        {
            var texture = sprite.texture;
            var uvRect = ToRect01(sprite.rect, texture);
            if (isSelected) EditorGUI.DrawRect(rect, Color.gray);
            GUI.DrawTextureWithTexCoords( rect, texture, uvRect );
            return GUI.Button(rect, GUIContent.none, GUI.skin.label);
        }

        private IEnumerable<KeyValuePair<T, Rect>> DrawGrid<T>(T[] list)
        {
            var xCount = 5;
            var yCount = Mathf.CeilToInt((float)list.Length / xCount);
            var i = 0;
            foreach (var rect in DrawGrid(xCount, yCount))
            {
                if (i < list.Length) yield return new KeyValuePair<T, Rect>(list[i], rect);
                i++;
            }
        }

        private IEnumerable<Rect> DrawGrid(int xCount, int yCount)
        {
            var id = GUIUtility.GetControlID("Grid".GetHashCode(), FocusType.Keyboard);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                for (var y = 0; y < yCount; y++)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (var x = 0; x < xCount; x++)
                        {
                            var rect = GUILayoutUtility.GetAspectRect(1);

                            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                            {
                                GUIUtility.hotControl = GUIUtility.keyboardControl = id;
                            }
                            yield return rect;
                        }
                    }
                }
            }
        }

        private Sprite[] GetSprites(Texture texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

    }
}

