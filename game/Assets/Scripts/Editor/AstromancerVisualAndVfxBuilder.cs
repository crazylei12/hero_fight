using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class AstromancerVisualAndVfxBuilder
    {
        private const string MenuPath = "Fight/Stage 01/Build Astromancer Visuals And VFX";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated/Astromancer";
        private const string HeroPrefabFolder = "Assets/Prefabs/Heroes/mage_005_astromancer";
        private const string ProjectilePrefabFolder = "Assets/Prefabs/VFX/Projectiles";
        private const string SkillPrefabFolder = "Assets/Prefabs/VFX/Skills";

        public const string HeroPrefabPath = HeroPrefabFolder + "/Astromancer.prefab";
        public const string HeroPortraitPath = HeroPrefabFolder + "/Astromancer_idle_front.png";
        public const string ProjectilePrefabPath = ProjectilePrefabFolder + "/AstromancerStarBoltProjectile.prefab";
        public const string FallingStarWarningPrefabPath = SkillPrefabFolder + "/AstromancerFallingStarWarning.prefab";
        public const string MeteorChoirFieldPrefabPath = SkillPrefabFolder + "/AstromancerMeteorChoirField.prefab";

        private const int BodyTextureSize = 128;
        private const int EffectTextureSize = 128;

        [MenuItem(MenuPath)]
        public static void BuildAstromancerVisualsAndVfx()
        {
            BuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildAstromancerVisualsAndVfxBatch()
        {
            try
            {
                BuildAstromancerVisualsAndVfx();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAll()
        {
            EnsureFolder("Assets/Art/VFX/Generated", "Astromancer");
            EnsureFolder("Assets/Prefabs/Heroes", "mage_005_astromancer");
            EnsureFolder("Assets/Prefabs/VFX", "Projectiles");
            EnsureFolder("Assets/Prefabs/VFX", "Skills");

            var bodySprite = CreateAstromancerBodySprite();
            var projectileSprite = CreateStarProjectileSprite();
            var warningSprite = CreateWarningRingSprite();
            var fieldSprite = CreateMeteorFieldSprite();

            BuildHeroPrefab(bodySprite);
            BuildProjectilePrefab(projectileSprite);
            BuildAreaPrefab(FallingStarWarningPrefabPath, "AstromancerFallingStarWarning", warningSprite, new Color(0.62f, 0.84f, 1f, 0.58f));
            BuildAreaPrefab(MeteorChoirFieldPrefabPath, "AstromancerMeteorChoirField", fieldSprite, new Color(0.28f, 0.36f, 0.78f, 0.42f));
        }

        private static Sprite CreateAstromancerBodySprite()
        {
            var texture = CreateTransparentTexture(BodyTextureSize, BodyTextureSize);
            var robe = new Color32(56, 48, 118, 255);
            var robeDark = new Color32(30, 28, 72, 255);
            var trim = new Color32(180, 213, 255, 255);
            var skin = new Color32(222, 196, 162, 255);
            var hair = new Color32(231, 237, 255, 255);
            var gold = new Color32(255, 224, 104, 255);

            FillEllipse(texture, 64, 88, 20, 18, hair);
            FillEllipse(texture, 64, 84, 14, 14, skin);
            FillTriangle(texture, new Vector2Int(64, 76), new Vector2Int(32, 18), new Vector2Int(96, 18), robe);
            FillTriangle(texture, new Vector2Int(64, 73), new Vector2Int(48, 19), new Vector2Int(80, 19), robeDark);
            DrawLine(texture, 47, 27, 37, 65, trim, 3);
            DrawLine(texture, 81, 27, 91, 65, trim, 3);
            DrawLine(texture, 64, 75, 64, 24, trim, 2);
            DrawStar(texture, 64, 103, 13, 6, gold);
            DrawStar(texture, 85, 68, 7, 3, new Color32(166, 219, 255, 230));
            DrawStar(texture, 43, 64, 6, 3, new Color32(166, 219, 255, 210));
            ApplySoftOutline(texture, new Color32(16, 18, 34, 210));
            texture.Apply();

            SaveTexture(texture, HeroPortraitPath, pixelsPerUnit: 96f);
            return AssetDatabase.LoadAssetAtPath<Sprite>(HeroPortraitPath);
        }

        private static Sprite CreateStarProjectileSprite()
        {
            var texture = CreateTransparentTexture(64, 64);
            FillEllipse(texture, 32, 32, 13, 13, new Color32(83, 173, 255, 155));
            FillEllipse(texture, 32, 32, 7, 7, new Color32(231, 247, 255, 235));
            DrawStar(texture, 32, 32, 23, 7, new Color32(152, 211, 255, 240));
            DrawStar(texture, 32, 32, 14, 4, new Color32(255, 236, 150, 250));
            texture.Apply();

            var path = GeneratedArtFolder + "/astromancer_star_projectile.png";
            SaveTexture(texture, path, pixelsPerUnit: 96f);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateWarningRingSprite()
        {
            var texture = CreateTransparentTexture(EffectTextureSize, EffectTextureSize);
            var center = (EffectTextureSize - 1) * 0.5f;
            for (var y = 0; y < EffectTextureSize; y++)
            {
                for (var x = 0; x < EffectTextureSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    var ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.78f) / 0.035f);
                    var inner = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.34f) / 0.025f) * 0.35f;
                    var cross = (Mathf.Abs(dx) < 1.4f || Mathf.Abs(dy) < 1.4f) && distance < 0.82f ? 0.18f : 0f;
                    var alpha = Mathf.Clamp01(ring + inner + cross);
                    if (alpha <= 0f)
                    {
                        continue;
                    }

                    texture.SetPixel(x, y, new Color(0.48f, 0.78f, 1f, alpha));
                }
            }

            DrawStar(texture, 64, 64, 18, 5, new Color32(255, 233, 142, 120));
            texture.Apply();

            var path = GeneratedArtFolder + "/astromancer_falling_star_warning.png";
            SaveTexture(texture, path, pixelsPerUnit: 128f);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateMeteorFieldSprite()
        {
            var texture = CreateTransparentTexture(EffectTextureSize, EffectTextureSize);
            var center = (EffectTextureSize - 1) * 0.5f;
            for (var y = 0; y < EffectTextureSize; y++)
            {
                for (var x = 0; x < EffectTextureSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    var rim = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.86f) / 0.025f) * 0.32f;
                    var haze = Mathf.Clamp01(1f - distance) * 0.08f;
                    var alpha = Mathf.Clamp01(rim + haze);
                    if (alpha > 0f)
                    {
                        texture.SetPixel(x, y, new Color(0.24f, 0.35f, 0.88f, alpha));
                    }
                }
            }

            DrawStar(texture, 35, 83, 6, 3, new Color32(214, 236, 255, 140));
            DrawStar(texture, 78, 92, 5, 2, new Color32(255, 234, 155, 130));
            DrawStar(texture, 90, 47, 7, 3, new Color32(214, 236, 255, 155));
            DrawStar(texture, 51, 42, 4, 2, new Color32(255, 234, 155, 120));
            texture.Apply();

            var path = GeneratedArtFolder + "/astromancer_meteor_choir_field.png";
            SaveTexture(texture, path, pixelsPerUnit: 128f);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BuildHeroPrefab(Sprite bodySprite)
        {
            var root = new GameObject("Astromancer");
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = 0;

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            body.transform.localScale = new Vector3(1.16f, 1.16f, 1f);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = bodySprite;
            renderer.sortingOrder = 0;

            var hand = new GameObject("HandR");
            hand.transform.SetParent(root.transform, false);
            hand.transform.localPosition = new Vector3(0.28f, 0.64f, 0f);

            SavePrefab(root, HeroPrefabPath);
        }

        private static void BuildProjectilePrefab(Sprite sprite)
        {
            var root = new GameObject("AstromancerStarBoltProjectile");
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = 0;
            root.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 0;

            SavePrefab(root, ProjectilePrefabPath);
        }

        private static void BuildAreaPrefab(string prefabPath, string name, Sprite sprite, Color color)
        {
            var root = new GameObject(name);
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = 0;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 0;

            SavePrefab(root, prefabPath);
        }

        private static Texture2D CreateTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var transparent = new Color32(0, 0, 0, 0);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            return texture;
        }

        private static void FillEllipse(Texture2D texture, int cx, int cy, int rx, int ry, Color32 color)
        {
            for (var y = cy - ry; y <= cy + ry; y++)
            {
                for (var x = cx - rx; x <= cx + rx; x++)
                {
                    if (!IsInside(texture, x, y))
                    {
                        continue;
                    }

                    var nx = (x - cx) / (float)rx;
                    var ny = (y - cy) / (float)ry;
                    if (nx * nx + ny * ny <= 1f)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void FillTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color32 color)
        {
            var minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            var maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            var minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            var maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            var area = Edge(a, b, c);
            if (Mathf.Approximately(area, 0f))
            {
                return;
            }

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (!IsInside(texture, x, y))
                    {
                        continue;
                    }

                    var p = new Vector2Int(x, y);
                    var w0 = Edge(b, c, p) / area;
                    var w1 = Edge(c, a, p) / area;
                    var w2 = Edge(a, b, p) / area;
                    if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static float Edge(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;
            while (true)
            {
                FillEllipse(texture, x0, y0, thickness, thickness, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void DrawStar(Texture2D texture, int cx, int cy, int outerRadius, int innerRadius, Color32 color)
        {
            var points = new Vector2[10];
            for (var i = 0; i < points.Length; i++)
            {
                var radius = i % 2 == 0 ? outerRadius : innerRadius;
                var angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                points[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
            }

            for (var i = 0; i < points.Length; i++)
            {
                var a = new Vector2Int(cx, cy);
                var b = Vector2Int.RoundToInt(points[i]);
                var c = Vector2Int.RoundToInt(points[(i + 1) % points.Length]);
                FillTriangle(texture, a, b, c, color);
            }
        }

        private static void ApplySoftOutline(Texture2D texture, Color32 outlineColor)
        {
            var width = texture.width;
            var height = texture.height;
            var snapshot = texture.GetPixels32();
            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var index = y * width + x;
                    if (snapshot[index].a > 0)
                    {
                        continue;
                    }

                    var nearOpaque = false;
                    for (var oy = -1; oy <= 1 && !nearOpaque; oy++)
                    {
                        for (var ox = -1; ox <= 1; ox++)
                        {
                            if (snapshot[(y + oy) * width + x + ox].a > 128)
                            {
                                nearOpaque = true;
                                break;
                            }
                        }
                    }

                    if (nearOpaque)
                    {
                        texture.SetPixel(x, y, outlineColor);
                    }
                }
            }
        }

        private static bool IsInside(Texture2D texture, int x, int y)
        {
            return x >= 0 && y >= 0 && x < texture.width && y < texture.height;
        }

        private static void SaveTexture(Texture2D texture, string assetPath, float pixelsPerUnit)
        {
            var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void SavePrefab(GameObject root, string prefabPath)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var folderPath = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
