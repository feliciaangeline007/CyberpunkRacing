using System.IO;
using UnityEditor;
using UnityEngine;

namespace CyberpunkRacing.Editor
{
    public static class SyntyURPMaterialUpgrader
    {
        [MenuItem("Tools/Cyberpunk Racing/🎨 Upgrade Semua Material Synty ke URP Lit", priority = 3)]
        public static void UpgradeAllSyntyMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpSimpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null)
            {
                Debug.LogError("[SyntyURPUpgrader] Shader 'Universal Render Pipeline/Lit' tidak ditemukan!");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SyntyStudios" });
            int upgradedCount = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Jika sudah URP Lit, lewati
                if (mat.shader != null && (mat.shader.name.StartsWith("Universal Render Pipeline/") || mat.shader.name.StartsWith("URP/")))
                {
                    continue;
                }

                // Ambil properti lama
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Texture emitTex = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                Color emitCol = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

                bool isGlass = mat.name.ToLower().Contains("glass") || (mat.HasProperty("_Color") && mat.GetColor("_Color").a < 0.95f);

                mat.shader = urpLit;

                if (mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                }
                mat.SetColor("_BaseColor", col);

                if (bump != null)
                {
                    mat.SetTexture("_BumpMap", bump);
                    mat.EnableKeyword("_NORMALMAP");
                }

                if (emitTex != null || emitCol.maxColorComponent > 0.05f)
                {
                    if (emitTex != null) mat.SetTexture("_EmissionMap", emitTex);
                    mat.SetColor("_EmissionColor", emitCol.maxColorComponent > 0.05f ? emitCol : Color.white);
                    mat.EnableKeyword("_EMISSION");
                }

                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);

                if (isGlass)
                {
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }

                EditorUtility.SetDirty(mat);
                upgradedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SyntyURPUpgrader] Berhasil upgrade {upgradedCount} material Synty ke URP Lit!");
        }
    }
}
