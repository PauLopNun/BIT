using UnityEngine;
using UnityEditor;

// ============================================================================
// TAGSETUP.CS - Configura los tags necesarios para el juego
// ============================================================================

namespace BIT.Editor
{
    [InitializeOnLoad]
    public static class TagSetup
    {
        static TagSetup()
        {
            AddRequiredTags();
        }

        [MenuItem("BIT/Setup Tags")]
        public static void AddRequiredTags()
        {
            string[] requiredTags = { "Player", "Enemy", "Coin", "Health", "Hazard", "Projectile" };

            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            foreach (string tag in requiredTags)
            {
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                    if (t.stringValue.Equals(tag))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                    newTag.stringValue = tag;
                    Debug.Log("[TagSetup] Tag añadido: " + tag);
                }
            }

            tagManager.ApplyModifiedProperties();
        }
    }
}
