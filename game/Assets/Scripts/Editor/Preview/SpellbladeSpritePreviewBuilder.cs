using Fight.UI.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fight.Editor.Preview
{
    public static class SpellbladeSpritePreviewBuilder
    {
        private const string ResourceRoot = "Assets/Resources/HeroPreview/warrior_004_spellblade";
        private const string PreviewPrefabPath = "Assets/Prefabs/Heroes/warrior_004_spellblade/SpellbladeSpritePreview.prefab";
        private const string PreviewScenePath = "Assets/Scenes/SpellbladeSpritePreview.unity";

        [MenuItem("Fight/Preview/Rebuild Spellblade Sprite Preview")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            ConfigureTextureImporters();
            CreatePreviewPrefab();
            CreatePreviewScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureTextureImporters()
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ResourceRoot });
            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void CreatePreviewPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/warrior_004_spellblade");

            var preview = CreatePreviewObject(
                "SpellbladeSpritePreview",
                "HeroPreview/warrior_004_spellblade/Idle",
                Vector3.zero,
                8f,
                0);

            PrefabUtility.SaveAsPrefabAsset(preview, PreviewPrefabPath);
            Object.DestroyImmediate(preview);
        }

        private static void CreatePreviewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SpellbladeSpritePreview";

            RenderSettings.ambientLight = new Color(0.82f, 0.84f, 0.88f);
            Camera.main?.gameObject.SetActive(false);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f);

            var root = new GameObject("Spellblade Preview Root");
            CreateBackdrop(root.transform);

            CreateLabeledPreview(root.transform, "Idle", "HeroPreview/warrior_004_spellblade/Idle", new Vector3(-4.4f, 2.05f, 0f), 8f, 0);
            CreateLabeledPreview(root.transform, "Run", "HeroPreview/warrior_004_spellblade/Run", new Vector3(0f, 2.05f, 0f), 10f, 10);
            CreateLabeledPreview(root.transform, "Attack", "HeroPreview/warrior_004_spellblade/Attack", new Vector3(4.4f, 2.05f, 0f), 10f, 20);
            CreateLabeledPreview(root.transform, "Hit", "HeroPreview/warrior_004_spellblade/Hit", new Vector3(-4.4f, -1.95f, 0f), 8f, 30);
            CreateLabeledPreview(root.transform, "Death", "HeroPreview/warrior_004_spellblade/Death", new Vector3(0f, -1.95f, 0f), 7f, 40);
            CreateLabeledPreview(root.transform, "Skill", "HeroPreview/warrior_004_spellblade/Skill", new Vector3(4.4f, -1.95f, 0f), 7f, 50);

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        private static void CreateLabeledPreview(
            Transform parent,
            string label,
            string resourceFolder,
            Vector3 position,
            float framesPerSecond,
            int sortingOrder)
        {
            var preview = CreatePreviewObject(label, resourceFolder, position, framesPerSecond, sortingOrder);
            preview.transform.SetParent(parent, worldPositionStays: true);

            var labelObject = new GameObject($"{label} Label");
            labelObject.transform.SetParent(parent, worldPositionStays: true);
            labelObject.transform.position = position + new Vector3(0f, -1.35f, 0f);

            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.045f;
            text.color = new Color(0.88f, 0.9f, 0.96f);
        }

        private static GameObject CreatePreviewObject(
            string name,
            string resourceFolder,
            Vector3 position,
            float framesPerSecond,
            int sortingOrder)
        {
            var preview = new GameObject(name);
            preview.transform.position = position;

            var spriteRenderer = preview.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;

            var animator = preview.AddComponent<SpriteTextureFrameAnimator>();
            var serializedAnimator = new SerializedObject(animator);
            serializedAnimator.FindProperty("resourcesFolder").stringValue = resourceFolder;
            serializedAnimator.FindProperty("framesPerSecond").floatValue = framesPerSecond;
            serializedAnimator.FindProperty("pixelsPerUnit").floatValue = 100f;
            serializedAnimator.FindProperty("spritePivot").vector2Value = new Vector2(0.5f, 0.08f);
            serializedAnimator.FindProperty("playInEditMode").boolValue = true;
            serializedAnimator.FindProperty("loop").boolValue = true;
            serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

            return preview;
        }

        private static void CreateBackdrop(Transform parent)
        {
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Preview Backdrop";
            backdrop.transform.SetParent(parent, worldPositionStays: false);
            backdrop.transform.position = new Vector3(0f, 0f, 1f);
            backdrop.transform.localScale = new Vector3(12.8f, 7.4f, 1f);

            var renderer = backdrop.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.12f, 0.13f, 0.16f, 1f)
            };

            Object.DestroyImmediate(backdrop.GetComponent<MeshCollider>());
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var slashIndex = folderPath.LastIndexOf('/');
            var parent = folderPath[..slashIndex];
            var folder = folderPath[(slashIndex + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
