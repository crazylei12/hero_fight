using System;
using System.Collections.Generic;
using System.IO;
using Fight.UI.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fight.Editor.Preview
{
    public static class ChatGptPixelSheetPreviewBuilder
    {
        private const string SourceSheetPath = "Assets/Art/Heroes/chatgpt_20260425_bellcaster/source_sheet.png";
        private const string ResourceRoot = "Assets/Resources/HeroPreview/chatgpt_20260425_bellcaster";
        private const string ResourcePrefix = "HeroPreview/chatgpt_20260425_bellcaster";
        private const string PreviewPrefabPath = "Assets/Prefabs/Heroes/chatgpt_20260425_bellcaster/ChatGptPixelSheetPreview.prefab";
        private const string PreviewScenePath = "Assets/Scenes/ChatGptPixelSheetPreview.unity";
        private const float PixelsPerUnit = 96f;

        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

        private static readonly RowSpec[] Rows =
        {
            new RowSpec("Action01", "action01", 0, 175, 5f, Slot(0, 190), Slot(185, 370), Slot(370, 565)),
            new RowSpec("Action02", "action02", 176, 319, 8f, Slot(0, 190), Slot(185, 370), Slot(370, 550), Slot(550, 730), Slot(720, 885), Slot(885, 1045)),
            new RowSpec("Action03", "action03", 320, 474, 7f, Slot(0, 180), Slot(175, 355), Slot(355, 555), Slot(550, 700)),
            new RowSpec("Action04", "action04", 475, 618, 9f, Slot(0, 175), Slot(175, 370), Slot(360, 555), Slot(550, 750), Slot(745, 955), Slot(955, 1115)),
            new RowSpec("Action05", "action05", 619, 771, 8f, Slot(0, 175), Slot(185, 365), Slot(380, 555), Slot(575, 750), Slot(750, 945), Slot(940, 1130)),
            new RowSpec("Action06", "action06", 772, 916, 7f, Slot(0, 190), Slot(190, 390), Slot(410, 610), Slot(630, 850), Slot(870, 1085)),
            new RowSpec("Action07", "action07", 917, 1047, 6f, Slot(0, 175), Slot(185, 390), Slot(390, 510)),
            new RowSpec("Action08", "action08", 1048, 1152, 6f, Slot(0, 175), Slot(175, 365), Slot(365, 565), Slot(555, 735)),
            new RowSpec("Action09", "action09", 1153, 1329, 8f, Slot(0, 145), Slot(145, 320), Slot(310, 515), Slot(515, 710), Slot(705, 895), Slot(890, 1120)),
        };

        [MenuItem("Fight/Preview/Rebuild ChatGPT Pixel Sheet Preview")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            GenerateFrameFolders();
            ConfigureTextureImporters();
            CreatePreviewPrefab();
            CreatePreviewScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built ChatGPT pixel sheet row preview.");
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing source sheet: {SourceSheetPath}");
            }

            EnsureFolder(ResourceRoot);
            foreach (var row in Rows)
            {
                var folderPath = $"{ResourceRoot}/{row.Name}";
                EnsureFolder(folderPath);
                ClearGeneratedPngs(folderPath);

                for (var i = 0; i < row.Slots.Length; i++)
                {
                    var slot = row.Slots[i];
                    var rect = ClampRect(
                        new RectInt(slot.Left, row.Top, slot.Right - slot.Left, row.Bottom - row.Top + 1),
                        sourceTexture.width,
                        sourceTexture.height);

                    var frameTexture = CropPngRect(sourceTexture, rect);
                    RemoveConnectedCheckerboardBackground(frameTexture);
                    var assetPath = $"{folderPath}/{row.FilePrefix}_{i:00}.png";
                    File.WriteAllBytes(ToAbsolutePath(assetPath), frameTexture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                    ApplyFrameTextureImporter(assetPath);
                }
            }

            UnityEngine.Object.DestroyImmediate(sourceTexture);
        }

        private static void ConfigureTextureImporters()
        {
            ApplySourceTextureImporter(SourceSheetPath);
            foreach (var row in Rows)
            {
                var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { $"{ResourceRoot}/{row.Name}" });
                foreach (var guid in textureGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    ApplyFrameTextureImporter(path);
                }
            }
        }

        private static void CreatePreviewPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/chatgpt_20260425_bellcaster");
            var root = CreatePreviewRoot("ChatGptPixelSheetPreview");
            PrefabUtility.SaveAsPrefabAsset(root, PreviewPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreatePreviewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ChatGptPixelSheetPreview";

            RenderSettings.ambientLight = new Color(0.86f, 0.88f, 0.92f);
            Camera.main?.gameObject.SetActive(false);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f);

            CreateBackdrop();
            var previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
            if (previewPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(previewPrefab);
            }
            else
            {
                CreatePreviewRoot("ChatGptPixelSheetPreview");
            }

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        private static GameObject CreatePreviewRoot(string name)
        {
            var root = new GameObject(name);
            for (var i = 0; i < Rows.Length; i++)
            {
                var column = i % 3;
                var row = i / 3;
                var position = new Vector3(-4.35f + column * 4.35f, 3.55f - row * 3.45f, 0f);
                CreateLabeledPreview(root.transform, Rows[i], position, i * 10);
            }

            return root;
        }

        private static void CreateLabeledPreview(Transform parent, RowSpec row, Vector3 position, int sortingOrder)
        {
            var preview = new GameObject(row.Name);
            preview.transform.SetParent(parent, worldPositionStays: true);
            preview.transform.position = position;

            var spriteRenderer = preview.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;

            var animator = preview.AddComponent<SpriteTextureFrameAnimator>();
            animator.Configure(
                $"{ResourcePrefix}/{row.Name}",
                row.FramesPerSecond,
                PixelsPerUnit,
                CenterPivot,
                loop: true);

            var labelObject = new GameObject($"{row.Name} Label");
            labelObject.transform.SetParent(parent, worldPositionStays: true);
            labelObject.transform.position = position + new Vector3(0f, -1.22f, 0f);

            var text = labelObject.AddComponent<TextMesh>();
            text.text = $"{row.Name}  {row.Slots.Length}f";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.042f;
            text.color = new Color(0.88f, 0.9f, 0.96f);
        }

        private static void CreateBackdrop()
        {
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Preview Backdrop";
            backdrop.transform.position = new Vector3(0f, 0f, 1f);
            backdrop.transform.localScale = new Vector3(14.4f, 12.5f, 1f);

            var renderer = backdrop.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.12f, 0.13f, 0.16f, 1f)
            };

            UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<MeshCollider>());
        }

        private static Texture2D CropPngRect(Texture2D sourceTexture, RectInt pngRect)
        {
            var frameTexture = new Texture2D(pngRect.width, pngRect.height, TextureFormat.RGBA32, false);
            var sourcePixels = sourceTexture.GetPixels32();
            var framePixels = new Color32[pngRect.width * pngRect.height];

            for (var y = 0; y < pngRect.height; y++)
            {
                var sourceY = sourceTexture.height - pngRect.y - pngRect.height + y;
                for (var x = 0; x < pngRect.width; x++)
                {
                    var sourceX = pngRect.x + x;
                    framePixels[y * pngRect.width + x] = sourcePixels[sourceY * sourceTexture.width + sourceX];
                }
            }

            frameTexture.SetPixels32(framePixels);
            frameTexture.filterMode = FilterMode.Point;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.Apply();
            return frameTexture;
        }

        private static void RemoveConnectedCheckerboardBackground(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
                {
                    return;
                }

                var index = y * texture.width + x;
                if (visited[index] || !IsCheckerboardCandidate(pixels[index]))
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < texture.width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, texture.height - 1);
            }

            for (var y = 0; y < texture.height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(texture.width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % texture.width;
                var y = index / texture.width;
                var color = pixels[index];
                color.a = 0;
                pixels[index] = color;

                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static bool IsCheckerboardCandidate(Color32 color)
        {
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            return max - min <= 24 && average >= 184f;
        }

        private static bool TryLoadTexture(string assetPath, out Texture2D texture)
        {
            texture = null;
            var absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return false;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (texture.LoadImage(File.ReadAllBytes(absolutePath), markNonReadable: false))
            {
                return true;
            }

            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
            return false;
        }

        private static RectInt ClampRect(RectInt rect, int maxWidth, int maxHeight)
        {
            var x = Mathf.Clamp(rect.x, 0, maxWidth - 1);
            var y = Mathf.Clamp(rect.y, 0, maxHeight - 1);
            var width = Mathf.Clamp(rect.width, 1, maxWidth - x);
            var height = Mathf.Clamp(rect.height, 1, maxHeight - y);
            return new RectInt(x, y, width, height);
        }

        private static FrameSlot Slot(int left, int right)
        {
            return new FrameSlot(left, right);
        }

        private static void ApplySourceTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ApplyFrameTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
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

        private static void ClearGeneratedPngs(string folderPath)
        {
            var absoluteFolder = ToAbsolutePath(folderPath);
            if (!Directory.Exists(absoluteFolder))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(absoluteFolder, "*.png"))
            {
                File.Delete(path);
                var metaPath = $"{path}.meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/');
            if (folderPath == "Assets" || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var slashIndex = folderPath.LastIndexOf('/');
            var parent = slashIndex > 0 ? folderPath[..slashIndex] : "Assets";
            var folder = slashIndex > 0 ? folderPath[(slashIndex + 1)..] : folderPath;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath);
        }

        private sealed class RowSpec
        {
            public RowSpec(string name, string filePrefix, int top, int bottom, float framesPerSecond, params FrameSlot[] slots)
            {
                Name = name;
                FilePrefix = filePrefix;
                Top = top;
                Bottom = bottom;
                FramesPerSecond = framesPerSecond;
                Slots = slots ?? Array.Empty<FrameSlot>();
            }

            public string Name { get; }
            public string FilePrefix { get; }
            public int Top { get; }
            public int Bottom { get; }
            public float FramesPerSecond { get; }
            public FrameSlot[] Slots { get; }
        }

        private readonly struct FrameSlot
        {
            public FrameSlot(int left, int right)
            {
                Left = left;
                Right = right;
            }

            public int Left { get; }
            public int Right { get; }
        }
    }
}
