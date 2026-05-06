using System.IO;
using System.Linq;
using UnityEngine;

namespace BIT.Core
{
    public static class RuntimeAssetLoader
    {
        public static T LoadAsset<T>(string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
            return Resources.Load<T>(ToResourcesPath(assetPath));
#endif
        }

        public static Sprite LoadFirstSprite(string assetPath)
        {
            Sprite[] sprites = LoadSprites(assetPath);
            if (sprites != null && sprites.Length > 0) return sprites[0];

            Texture2D texture = LoadAsset<Texture2D>(assetPath);
            if (texture == null) return null;

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        public static Sprite LoadFirstAvailableSprite(params string[] assetPaths)
        {
            foreach (string assetPath in assetPaths)
            {
                Sprite sprite = LoadFirstSprite(assetPath);
                if (sprite != null) return sprite;
            }

            return null;
        }

        public static Sprite[] LoadSprites(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

#if UNITY_EDITOR
            Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(GetSpriteIndex)
                .ToArray();
#else
            Sprite[] sprites = Resources.LoadAll<Sprite>(ToResourcesPath(assetPath))
                .OrderBy(GetSpriteIndex)
                .ToArray();
#endif
            return sprites.Length > 0 ? sprites : null;
        }

        public static GameObject LoadPickupPrefab(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(
                $"t:Prefab {name}", new[] { "Assets/_Project/Prefabs/Pickups" });
            if (guids.Length > 0)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
#endif
            return Resources.Load<GameObject>($"_Project/Prefabs/Pickups/{name}");
        }

        private static int GetSpriteIndex(Sprite sprite)
        {
            if (sprite == null) return int.MaxValue;
            int index = sprite.name.LastIndexOf('_');
            return index >= 0 && int.TryParse(sprite.name.Substring(index + 1), out int n)
                ? n
                : int.MaxValue;
        }

        private static string ToResourcesPath(string assetPath)
        {
            string path = assetPath.Replace("\\", "/");

            const string resourcesPrefix = "Assets/Resources/";
            if (path.StartsWith(resourcesPrefix))
                path = path.Substring(resourcesPrefix.Length);
            else if (path.StartsWith("Assets/"))
                path = path.Substring("Assets/".Length);

            string extension = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(extension))
                path = path.Substring(0, path.Length - extension.Length);

            return path;
        }
    }
}
