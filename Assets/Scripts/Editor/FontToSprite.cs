using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace Tools
{
    public class FontToSprite:EditorWindow
    {
        private TMP_FontAsset font;
        private int size = 1024;
        
        static readonly int[] Sizes = {128, 256, 512, 1024, 2048};
        static readonly string[] SizeLabels = {"128", "256", "512", "1024", "2048"};
        
        [MenuItem("Tools/Font to Sprite")]
        static void Open() => GetWindow<FontToSprite>("Font to Sprite");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SDF to sprite baker", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", font, typeof(TMP_FontAsset), false);
            size = EditorGUILayout.IntPopup("Texture Size", size, SizeLabels, Sizes);

            using (new EditorGUI.DisabledScope(font == null))
            {
                if (GUILayout.Button("Generate"))
                {
                    Generate();
                }
            }
        }

        void Generate()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Font Texture", font.name + "_texture", "png",
                "Choose where to save the generated font texture");
            if (string.IsNullOrEmpty(path)) return;
            
            Texture2D texture = Bake(font, size, out int cols, out int rows);
            SaveSprite(texture, path);
            DestroyImmediate(texture);
            Debug.Log($"[FontToSprite] Baked {font.characterTable.Count} glyphs" +
                            $"into a {cols}x{rows} grid at {size}px -> {path}");
        }

        Texture2D Bake(TMP_FontAsset fontAsset, int textureSize, out int cols, out int rows)
        {
            var characters = fontAsset.characterTable;
            int count = characters.Count;
            
            cols = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(count)));
            rows = Mathf.CeilToInt(count/(float)cols);
            int cell = textureSize / cols;
            
            Texture2D[] pages = ReadableAssetCopies(fontAsset);
            bool valueInAlpha = fontAsset.atlasTexture.format == TextureFormat.Alpha8;

            int maxDim = 1;

            foreach (var ch in characters)
            {
                var r = ch.glyph.glyphRect;
                maxDim = Mathf.Max(maxDim, Mathf.Max(r.width, r.height));
            }

            const float fill = 0.9f;
            float scale = (cell*fill)/maxDim;
            
            var pixels = new Color32[textureSize * textureSize ];

            for (int i = 0; i < count; i++)
            {
                Glyph g = characters[i].glyph;
                if(g == null) continue;
                GlyphRect gr = g.glyphRect;
                if (gr.width == 0 || gr.height == 0) continue;

                Texture2D src = pages[g.atlasIndex];
                int dw = Mathf.RoundToInt(gr.width*scale);
                int dh = Mathf.RoundToInt(gr.height*scale);

                Vector2Int o = CellOrigin(i, cols, cell, textureSize);
                int ox = o.x + (cell - dw) / 2;
                int oy = o.y + (cell - dh) / 2;

                for (int dy = 0; dy < dh; dy++)
                {
                    for (int dx = 0; dx < dw; dx++)
                    {
                        float u = (gr.x + dx / scale) / src.width;
                        float v = (gr.y + dy / scale) / src.height;
                        Color c = src.GetPixelBilinear(u, v);
                        byte sdf = (byte)(Mathf.Clamp01(valueInAlpha ? c.a : c.r) * 255f);
                        pixels[(oy + dy) * textureSize + ox + dx] = new Color32(255, 255, 255, sdf);
                    }
                }
            }
            var sheet = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            sheet.SetPixels32(pixels);
            sheet.Apply();

            foreach (var p in pages) DestroyImmediate(p);
            return sheet;
        }

        Texture2D[] ReadableAssetCopies(TMP_FontAsset fontAsset)
        {
            Texture[] atlases = fontAsset.atlasTextures;
            var copies = new Texture2D[atlases.Length];

            for (int i = 0; i < atlases.Length; i++)
            {
                Texture src = atlases[i];
                var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(src, rt);
                
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                copy.wrapMode = TextureWrapMode.Clamp;
                copy.Apply();
                
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                copies[i] = copy;
            }
            return copies;
        }

        void SaveSprite(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = tex.width;
            importer.SaveAndReimport();
            
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Sprite>(path));
        }

        static Vector2Int CellOrigin(int index, int cols, int cell, int textureSize)
        {
            int col = index % cols;
            int row = index / cols;
            return new Vector2Int(col * cell, textureSize - (row+1)*cell);
        }
        
    }
}