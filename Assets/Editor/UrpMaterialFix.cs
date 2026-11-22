using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class UrpMaterialFix
{
    [MenuItem("The Last Guest/Fix Pink Materials")]
    public static void Fix()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogError("MATERIALS FIX FAILED: Universal Render Pipeline/Lit was not found.");
            return;
        }

        var upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));
        int upgraded = 0;
        int converted = 0;
        int textured = 0;
        int skipped = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            var shaderName = material.shader != null ? material.shader.name : string.Empty;
            if (IsAlreadySupported(shaderName))
            {
                skipped++;
                continue;
            }

            var shaderBefore = material.shader;
            var message = string.Empty;
            if (shaderName != "Hidden/InternalErrorShader")
            {
                MaterialUpgrader.Upgrade(material, upgraders, MaterialUpgrader.UpgradeFlags.None, ref message);
                if (material.shader != shaderBefore && IsAlreadySupported(material.shader.name))
                {
                    EditorUtility.SetDirty(material);
                    upgraded++;
                    continue;
                }
            }

            if (ConvertToLit(material, lit))
                textured++;
            EditorUtility.SetDirty(material);
            converted++;
        }

        var restored = RestoreVintageTextures();
        AssetDatabase.SaveAssets();
        Debug.Log("MATERIALS FIXED upgraded=" + upgraded + " converted=" + converted + " withTexture=" + textured + " restored=" + restored + " alreadyOk=" + skipped);
    }

    static int RestoreVintageTextures()
    {
        const string root = "Assets/Scenes/ZNS3D/Vintage Living Room Game Pack";
        var baseColors = new List<KeyValuePair<string, Texture2D>>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root + "/Textures" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.ToLowerInvariant().Contains("basecolor"))
                continue;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                baseColors.Add(new KeyValuePair<string, Texture2D>(Normalize(fileName), texture));
        }

        var restored = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { root + "/Materials" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || material.shader == null || !material.shader.name.StartsWith("Universal Render Pipeline/Lit"))
                continue;

            var key = Normalize(material.name);
            Texture2D albedo = null;
            foreach (var candidate in baseColors)
            {
                if (key.Length >= 5 && candidate.Key.Contains(key))
                {
                    albedo = candidate.Value;
                    break;
                }
            }

            var glass = material.name.ToLowerInvariant().Contains("glass");
            if (albedo != null)
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_MainTex", albedo);
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                restored++;
            }
            else if (glass)
            {
                material.SetColor("_BaseColor", new Color(0.86f, 0.91f, 0.95f, 0.28f));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_Smoothness", 0.9f);
                material.SetFloat("_Metallic", 0f);
            }
            else if (material.GetTexture("_BaseMap") == null)
            {
                material.SetColor("_BaseColor", new Color(0.45f, 0.38f, 0.32f, 1f));
            }

            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", glass ? 0.9f : 0.28f);
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            if (material.GetTexture("_BumpMap") != null)
                material.EnableKeyword("_NORMALMAP");

            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);
        }

        return restored;
    }

    static string Normalize(string value)
    {
        value = value.ToLowerInvariant();
        value = value.Replace("gramophone", "gramafon");
        value = value.Replace("mat_", "").Replace("mat-", "");
        return value.Replace("_", "").Replace("-", "").Replace(" ", "");
    }

    static bool IsAlreadySupported(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return false;

        return shaderName.StartsWith("Universal Render Pipeline")
            || shaderName.StartsWith("Shader Graphs")
            || shaderName.StartsWith("Sprites/")
            || shaderName.StartsWith("UI/")
            || shaderName.StartsWith("GUI/")
            || shaderName.StartsWith("Skybox/")
            || shaderName.StartsWith("TextMesh");
    }

    static bool ConvertToLit(Material material, Shader lit)
    {
        var saved = ReadSaved(material);
        var transparent = saved.renderQueue >= 3000
            || saved.surfaceType > 0.5f
            || material.name.ToLowerInvariant().Contains("glass");

        material.shader = lit;
        material.SetFloat("_WorkflowMode", 1f);
        material.SetColor("_BaseColor", saved.color);
        material.SetColor("_Color", saved.color);
        material.SetFloat("_Smoothness", saved.smoothness);
        material.SetFloat("_Metallic", saved.metallic);
        material.SetFloat("_BumpScale", saved.bumpScale <= 0f ? 1f : saved.bumpScale);
        material.SetFloat("_OcclusionStrength", 1f);

        var hasAlbedo = saved.albedo != null;
        if (hasAlbedo)
        {
            material.SetTexture("_BaseMap", saved.albedo);
            material.SetTexture("_MainTex", saved.albedo);
            material.SetTextureScale("_BaseMap", saved.albedoScale);
            material.SetTextureOffset("_BaseMap", saved.albedoOffset);
        }

        if (saved.normal != null)
        {
            material.SetTexture("_BumpMap", saved.normal);
            material.EnableKeyword("_NORMALMAP");
        }

        if (saved.metallicMap != null)
        {
            material.SetTexture("_MetallicGlossMap", saved.metallicMap);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        if (saved.occlusion != null)
        {
            material.SetTexture("_OcclusionMap", saved.occlusion);
            material.EnableKeyword("_OCCLUSIONMAP");
        }

        if (saved.emissionMap != null || saved.emission.grayscale > 0.01f)
        {
            if (saved.emissionMap != null)
                material.SetTexture("_EmissionMap", saved.emissionMap);
            material.SetColor("_EmissionColor", saved.emission.grayscale > 0.01f ? saved.emission : Color.white);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            if (saved.color.a >= 0.99f)
            {
                var tint = saved.color;
                tint.a = 0.35f;
                material.SetColor("_BaseColor", tint);
            }
        }
        else
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", saved.alphaClip ? 1f : 0f);
            if (saved.alphaClip)
            {
                material.SetFloat("_Cutoff", saved.cutoff);
                material.EnableKeyword("_ALPHATEST_ON");
            }
        }

        if (saved.doubleSided)
        {
            material.SetFloat("_Cull", 0f);
            material.doubleSidedGI = true;
        }
        else
        {
            material.SetFloat("_Cull", 2f);
        }

        BaseShaderGUI.SetupMaterialBlendMode(material);
        return hasAlbedo;
    }

    struct SavedMaterial
    {
        public Texture albedo;
        public Vector2 albedoScale;
        public Vector2 albedoOffset;
        public Texture normal;
        public Texture metallicMap;
        public Texture occlusion;
        public Texture emissionMap;
        public Color color;
        public Color emission;
        public float smoothness;
        public float metallic;
        public float bumpScale;
        public float surfaceType;
        public float cutoff;
        public int renderQueue;
        public bool alphaClip;
        public bool doubleSided;
    }

    static SavedMaterial ReadSaved(Material material)
    {
        var saved = new SavedMaterial
        {
            color = Color.white,
            emission = Color.black,
            smoothness = 0.35f,
            metallic = 0f,
            bumpScale = 1f,
            cutoff = 0.5f,
            albedoScale = Vector2.one,
            renderQueue = material.renderQueue
        };

        var serialized = new SerializedObject(material);
        var textures = ReadTextures(serialized);
        var colors = ReadColors(serialized);
        var floats = ReadFloats(serialized);

        saved.albedo = FirstTexture(textures, "_BaseColorMap", "_BaseMap", "_MainTex", "_Albedo", "_Diffuse");
        saved.normal = FirstTexture(textures, "_BumpMap", "_NormalMap");
        saved.metallicMap = FirstTexture(textures, "_MetallicGlossMap");
        saved.occlusion = FirstTexture(textures, "_OcclusionMap");
        saved.emissionMap = FirstTexture(textures, "_EmissionMap", "_EmissiveColorMap");

        if (textures.TryGetValue("_MainTex", out var main) && saved.albedo == main.texture)
        {
            saved.albedoScale = main.scale;
            saved.albedoOffset = main.offset;
        }
        else if (textures.TryGetValue("_BaseColorMap", out var baseMap) && saved.albedo == baseMap.texture)
        {
            saved.albedoScale = baseMap.scale;
            saved.albedoOffset = baseMap.offset;
        }

        if (colors.TryGetValue("_BaseColor", out var baseColor))
            saved.color = baseColor;
        else if (colors.TryGetValue("_Color", out var color))
            saved.color = color;

        if (colors.TryGetValue("_EmissiveColor", out var emissive))
            saved.emission = emissive;
        else if (colors.TryGetValue("_EmissionColor", out var emission))
            saved.emission = emission;

        if (floats.TryGetValue("_Smoothness", out var smoothness))
            saved.smoothness = smoothness;
        else if (floats.TryGetValue("_Glossiness", out var gloss))
            saved.smoothness = gloss;

        if (floats.TryGetValue("_Metallic", out var metallic))
            saved.metallic = metallic;
        if (floats.TryGetValue("_BumpScale", out var bump))
            saved.bumpScale = bump;
        if (floats.TryGetValue("_SurfaceType", out var surface))
            saved.surfaceType = surface;
        if (floats.TryGetValue("_Cutoff", out var cutoff))
            saved.cutoff = cutoff;
        if (floats.TryGetValue("_AlphaCutoffEnable", out var clip))
            saved.alphaClip = clip > 0.5f;
        if (floats.TryGetValue("_DoubleSidedEnable", out var sided))
            saved.doubleSided = sided > 0.5f;

        return saved;
    }

    struct SavedTexture
    {
        public Texture texture;
        public Vector2 scale;
        public Vector2 offset;
    }

    static Dictionary<string, SavedTexture> ReadTextures(SerializedObject serialized)
    {
        var result = new Dictionary<string, SavedTexture>();
        var textures = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
        if (textures == null)
            return result;

        for (var i = 0; i < textures.arraySize; i++)
        {
            var entry = textures.GetArrayElementAtIndex(i);
            var texture = entry.FindPropertyRelative("m_Texture");
            if (texture == null)
                continue;

            var scale = entry.FindPropertyRelative("m_Scale");
            var offset = entry.FindPropertyRelative("m_Offset");
            result[entry.displayName] = new SavedTexture
            {
                texture = texture.objectReferenceValue as Texture,
                scale = scale != null ? scale.vector2Value : Vector2.one,
                offset = offset != null ? offset.vector2Value : Vector2.zero
            };
        }

        return result;
    }

    static Dictionary<string, Color> ReadColors(SerializedObject serialized)
    {
        var result = new Dictionary<string, Color>();
        var colors = serialized.FindProperty("m_SavedProperties.m_Colors");
        if (colors == null)
            return result;

        for (var i = 0; i < colors.arraySize; i++)
        {
            var entry = colors.GetArrayElementAtIndex(i);
            result[entry.displayName] = entry.colorValue;
        }

        return result;
    }

    static Dictionary<string, float> ReadFloats(SerializedObject serialized)
    {
        var result = new Dictionary<string, float>();
        var floats = serialized.FindProperty("m_SavedProperties.m_Floats");
        if (floats == null)
            return result;

        for (var i = 0; i < floats.arraySize; i++)
        {
            var entry = floats.GetArrayElementAtIndex(i);
            result[entry.displayName] = entry.floatValue;
        }

        return result;
    }

    static Texture FirstTexture(Dictionary<string, SavedTexture> textures, params string[] names)
    {
        foreach (var name in names)
        {
            if (textures.TryGetValue(name, out var saved) && saved.texture != null)
                return saved.texture;
        }

        return null;
    }
}

[InitializeOnLoad]
static class UrpMaterialFixAuto
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "FixMaterials.request"));

    static UrpMaterialFixAuto()
    {
        if (File.Exists(Flag))
            EditorApplication.delayCall += RunWhenReady;
    }

    static void RunWhenReady()
    {
        if (!File.Exists(Flag))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }

        File.Delete(Flag);
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            UrpMaterialFix.Fix();
    }
}
