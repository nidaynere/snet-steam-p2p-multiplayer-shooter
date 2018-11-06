/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// This is a part of FuseImporter
/// </summary>
public class TexturePostProcessor : AssetPostprocessor
{
    void OnPostprocessTexture (Texture2D texture)
    {
        TextureImporter importer = assetImporter as TextureImporter;

        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        bool isNormal = fileName.Contains("_Normal");
        if (!isNormal)
            isNormal = fileName.Contains("_normal");

        if (isNormal)
            importer.textureType = TextureImporterType.NormalMap;

        // FUSE PART

        if (FuseImporter.isImporting)
        {
            string materialName = fileName.Split('_') [1];
            string textureType = fileName.Split('_')[2];
            DirectoryInfo fileFolder = Directory.GetParent(assetPath).Parent;

            string path = "Assets/Adobe_Fuse_Characters/" + fileFolder.Name + "/Materials/" + materialName + ".mat";
            Material material = (Material) AssetDatabase.LoadAssetAtPath(path, typeof(Material));
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.ImportAsset(path);
            }

            string tag = "";

            switch (textureType)
            {
                case "BaseColor":
                    tag = "_MainTex";
                    break;
                case "AmbientOcclusion":
                    tag = "_OcclusionMap";
                    break;
                case "MetallicAndSmoothness":
                    tag = "_MetallicGlossMap";
                    break;
                case "Normal":
                    tag = "_BumpMap";
                    break;
            }

            FuseImporter.targets.Add(new FuseImporter.FuseTarget() { assetPath = assetPath, mat = path, tag = tag});
        }
    }
}
