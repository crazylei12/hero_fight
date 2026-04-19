using System;
using System.IO;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Fight.Data;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    public static class HeroPortraitGenerator
    {
        private const string Stage01HeroRoot = "Assets/Data/Stage01Demo/Heroes";
        private const string PortraitRoot = "Assets/Art/Heroes";
        private const int PortraitLayer = 31;
        private const int PortraitSize = 256;

        [MenuItem("Fight/Dev/Generate Hero Portraits")]
        public static void GenerateStage01DemoPortraits()
        {
            var generatedCount = 0;
            var heroGuids = AssetDatabase.FindAssets("t:HeroDefinition", new[] { Stage01HeroRoot });
            Array.Sort(heroGuids, StringComparer.Ordinal);

            for (var i = 0; i < heroGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(heroGuids[i]);
                var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
                if (hero == null || hero.visualConfig == null || hero.visualConfig.battlePrefab == null)
                {
                    continue;
                }

                var portrait = GeneratePortrait(hero);
                if (portrait == null)
                {
                    continue;
                }

                hero.visualConfig.portrait = portrait;
                EditorUtility.SetDirty(hero);
                generatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[HeroPortraitGenerator] Generated or refreshed {generatedCount} hero portraits.");
        }

        public static void GenerateStage01DemoPortraitsBatchmode()
        {
            try
            {
                GenerateStage01DemoPortraits();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static Sprite GeneratePortrait(HeroDefinition hero)
        {
            var outputDirectory = EnsureHeroPortraitFolder(hero.heroId);
            var outputPath = $"{outputDirectory}/{hero.heroId}_idle_portrait.png";
            var texture = RenderPortraitTexture(hero);
            if (texture == null)
            {
                return null;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var fullOutputPath = Path.Combine(projectRoot, outputPath);
            var folderPath = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            File.WriteAllBytes(fullOutputPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImporter(outputPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        }

        private static Texture2D RenderPortraitTexture(HeroDefinition hero)
        {
            var captureRoot = new GameObject($"{hero.heroId}_PortraitCaptureRoot")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var cameraRoot = new GameObject("PortraitCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraRoot.transform.SetParent(captureRoot.transform, false);

            GameObject instance = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;

            try
            {
                instance = UnityEngine.Object.Instantiate(hero.visualConfig.battlePrefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.SetParent(captureRoot.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                PreparePortraitPose(hero, instance);
                SetLayerRecursively(captureRoot, PortraitLayer);

                if (!TryGetVisibleBounds(instance, out var bounds))
                {
                    return null;
                }

                var camera = cameraRoot.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.cullingMask = 1 << PortraitLayer;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                camera.allowHDR = false;
                camera.allowMSAA = false;

                var horizontalPadding = bounds.extents.x * 0.2f + 0.15f;
                var verticalPadding = bounds.extents.y * 0.26f + 0.15f;
                var halfWidth = bounds.extents.x + horizontalPadding;
                var halfHeight = bounds.extents.y + verticalPadding;
                camera.orthographicSize = Mathf.Max(0.5f, halfHeight, halfWidth);
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y + (bounds.extents.y * 0.06f), -10f);
                camera.transform.rotation = Quaternion.identity;

                renderTexture = RenderTexture.GetTemporary(PortraitSize, PortraitSize, 24, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 1;
                camera.targetTexture = renderTexture;
                camera.Render();

                var previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture = new Texture2D(PortraitSize, PortraitSize, TextureFormat.ARGB32, false);
                texture.ReadPixels(new Rect(0f, 0f, PortraitSize, PortraitSize), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                return texture;
            }
            finally
            {
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (cameraRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraRoot);
                }

                if (captureRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(captureRoot);
                }
            }
        }

        private static void PreparePortraitPose(HeroDefinition hero, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null && hero.visualConfig != null && hero.visualConfig.animatorController != null)
            {
                animator.runtimeAnimatorController = hero.visualConfig.animatorController;
            }

            var character = instance.GetComponentInChildren<Character4D>(true);
            if (character != null)
            {
                character.Initialize();
                character.SetDirection(Vector2.right);

                var animationManager = instance.GetComponentInChildren<AnimationManager>(true);
                if (animationManager != null)
                {
                    animationManager.IsAction = false;
                    animationManager.SetState(CharacterState.Idle);
                }
            }
            else
            {
                var scale = instance.transform.localScale;
                var facesLeftByDefault = hero.visualConfig != null && hero.visualConfig.battlePrefabFacesLeftByDefault;
                scale.x = Mathf.Abs(scale.x) * (facesLeftByDefault ? -1f : 1f);
                instance.transform.localScale = scale;
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static bool TryGetVisibleBounds(GameObject instance, out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (instance == null)
            {
                return false;
            }

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static string EnsureHeroPortraitFolder(string heroId)
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Heroes");
            EnsureFolder(PortraitRoot, heroId);
            return $"{PortraitRoot}/{heroId}";
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, child);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            for (var i = 0; i < root.transform.childCount; i++)
            {
                SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
