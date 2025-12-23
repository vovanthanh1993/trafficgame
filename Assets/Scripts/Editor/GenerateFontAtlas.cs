using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Script để generate font atlas cho TMP fonts
/// </summary>
public class GenerateFontAtlas : EditorWindow
{
    [MenuItem("Tools/Generate Font Atlas/WALRUSGU SDF")]
    public static void GenerateWALRUSGUAtlas()
    {
        string fontPath = "Assets/Art By Kandles/Cute RPG UI Bundle/Cute RPG UI Kit/FOnts/WALRUSGU SDF.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        
        if (fontAsset == null)
        {
            Debug.LogError($"Không tìm thấy font asset tại: {fontPath}");
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy font asset tại:\n{fontPath}", "OK");
            return;
        }
        
        // Select font asset
        Selection.activeObject = fontAsset;
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(fontAsset);
        
        Debug.Log("Đã chọn font asset. Vui lòng làm theo các bước sau:");
        Debug.Log("1. Font asset đã được chọn trong Project window");
        Debug.Log("2. Mở font asset trong Inspector");
        Debug.Log("3. Scroll xuống phần 'Font Asset Creator'");
        Debug.Log("4. Click 'Generate Font Atlas' button");
        Debug.Log("5. Đợi quá trình generate hoàn thành");
        
        EditorUtility.DisplayDialog("Hướng dẫn Generate Font Atlas", 
            "Đã chọn font asset: WALRUSGU SDF\n\n" +
            "Để generate font atlas:\n\n" +
            "1. Font asset đã được chọn trong Project window\n" +
            "2. Mở font asset trong Inspector (double-click hoặc click vào nó)\n" +
            "3. Scroll xuống tìm phần 'Font Asset Creator'\n" +
            "4. Click nút 'Generate Font Atlas' hoặc 'Update Font Atlas'\n" +
            "5. Đợi quá trình generate hoàn thành (có thể mất vài giây)\n\n" +
            "Sau khi generate xong, text sẽ hiển thị bình thường.", 
            "OK");
    }
    
    [MenuItem("Tools/Generate Font Atlas/Check Atlas Status")]
    public static void CheckAtlasStatus()
    {
        string fontPath = "Assets/Art By Kandles/Cute RPG UI Bundle/Cute RPG UI Kit/FOnts/WALRUSGU SDF.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        
        if (fontAsset == null)
        {
            Debug.LogError($"Không tìm thấy font asset tại: {fontPath}");
            return;
        }
        
        System.Text.StringBuilder status = new System.Text.StringBuilder();
        status.AppendLine($"Font Asset: {fontAsset.name}");
        status.AppendLine($"Material: {(fontAsset.material != null ? fontAsset.material.name : "NULL - CẦN FIX")}");
        status.AppendLine($"Atlas Textures Count: {fontAsset.atlasTextureCount}");
        
        if (fontAsset.atlasTextureCount > 0)
        {
            for (int i = 0; i < fontAsset.atlasTextureCount; i++)
            {
                Texture2D atlas = fontAsset.atlasTextures[i];
                if (atlas != null)
                {
                    status.AppendLine($"  Atlas {i}: {atlas.width}x{atlas.height} - OK");
                }
                else
                {
                    status.AppendLine($"  Atlas {i}: NULL - CẦN GENERATE");
                }
            }
        }
        else
        {
            status.AppendLine("  Không có atlas texture - CẦN GENERATE");
        }
        
        Debug.Log(status.ToString());
        EditorUtility.DisplayDialog("Font Atlas Status", status.ToString(), "OK");
    }
}

