/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is a material generator script for Adobe Fuse BETA.
/// We wrote this script because we tired to create materials for Fuse models. :)
/// </summary>
public class FuseImporter : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("SNet Tools/Mixamo Fuse Importer")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        FuseImporter window = (FuseImporter)GetWindow(typeof(FuseImporter));
        window.minSize = window.maxSize = new Vector2(800, 500);

        window.Show();
    }

    public class FuseTarget
    {
        public string assetPath;
        public string mat;
        public string tag;

        public bool done = false;
    }

    public static List<FuseTarget> targets = new List<FuseTarget>();

    /// <summary>
    /// Is fuse currently importing?, used by texturepostprocessor
    /// </summary>
    public static bool isImporting;

    void OnGUI()
    {
        GUILayout.Label("Use this extension to import Adobe Fuse characters.", EditorStyles.boldLabel);
        GUILayout.Label("Textures must be placed in a folder named Textures beside .FBX file.", EditorStyles.label);

        if (GUILayout.Button("Import Fuse FBX"))
        {
            var selected = EditorUtility.OpenFilePanel("Select FBX file created by Adobe Fuse", "", "fbx");
            if (selected.Length == 0)
            {
                // No path given
                return;
            }
            
            CreateFolder("", "Adobe_Fuse_Characters");

            string folderName = Path.GetFileNameWithoutExtension(selected);

            isImporting = true;

            DirectoryCopy(Directory.GetParent(selected).ToString (), Application.dataPath + "/Adobe_Fuse_Characters/" + folderName, true);

            AssetDatabase.Refresh();

            isImporting = false;

            EditorApplication.delayCall += MaterialFixer;
        }
    }

    void MaterialFixer ()
    {
        bool eyesCreated = false;
        foreach (FuseTarget ft in targets)
        {
            Texture texture = (Texture)AssetDatabase.LoadAssetAtPath(ft.assetPath, typeof(Texture));

            Material m = (Material)AssetDatabase.LoadAssetAtPath(ft.mat, typeof(Material));
            m.SetTexture(ft.tag, texture);

            if (ft.done)
                continue;

            ft.done = true;

            SetMaterial(ft.mat);
            /*
             * CHECK BODY.
             * */

            if (m.name == "Body" || !eyesCreated)
            {
                eyesCreated = true;
                // Create eyeslashes and eyes from body material

                string[] pParse = ft.mat.Split('/');

                pParse[pParse.Length - 1] = "Eyeslashes.mat";
                string eyeSlashesMat = "";
                bool parsed = false;
                foreach (string s in pParse)
                {
                    if (parsed)
                        eyeSlashesMat += "/";
                    parsed = true;
                    eyeSlashesMat += s;
                } 

                AssetDatabase.CopyAsset(ft.mat, eyeSlashesMat);

                SetMaterial(eyeSlashesMat);

                pParse[pParse.Length - 1] = "Eyes.mat";
                string eyesMat = "";
                parsed = false;
                foreach (string s in pParse)
                {
                    if (parsed)
                        eyesMat += "/";
                    parsed = true;
                    eyesMat += s;
                }

                AssetDatabase.CopyAsset(ft.mat, eyesMat);

                SetMaterial(eyesMat);
            }
        }

        targets.Clear();

        AssetDatabase.Refresh();
    }

    void SetMaterial(string path)
    {
        Material m = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));

        BodyPart bp = _parts.ToList().Find(x => x.Name == m.name);

        if (bp == null)
        {
            Debug.Log(m.name + " is missing.");
            return;
        }

        m.SetFloat("_Mode", (float) bp.BlendMode);
        // Configure the part parameters.
        m.SetFloat("_GlossMapScale", bp.GlossScale);
        m.SetFloat("_BumpScale", bp.BumpScale);

        if (bp.BlendMode == BlendMode.Cutout)
        {
            m.SetFloat("_Cutoff", bp.CutoffAlpha);
        }
    }

    private enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
    }

    private class BodyPart
    {
        public string Name;
        public BlendMode BlendMode = BlendMode.Opaque;
        public float BumpScale = 1.2f;
        public float GlossScale = 1.0f;
        public float CutoffAlpha = 0.5f;
    }

    // Configuration for each body part material.
    private static BodyPart[] _parts = {
      new BodyPart { Name="Body", GlossScale=0.8f },
      new BodyPart { Name="Eyes", GlossScale=0.8f },
      new BodyPart { Name="Eyeslashes", BlendMode=BlendMode.Fade },
      new BodyPart { Name="Top" },
      new BodyPart { Name="Bottom" },
      new BodyPart { Name="Hair", BlendMode=BlendMode.Fade, GlossScale=0.95f },
      new BodyPart { Name="Moustache", BlendMode=BlendMode.Fade, GlossScale=0.6f },
      new BodyPart { Name="Beard", BlendMode=BlendMode.Fade, GlossScale=0.6f },
      new BodyPart { Name="Eyewear", BlendMode=BlendMode.Fade },
      new BodyPart { Name="Glove" },
      new BodyPart { Name="Shoes" },
      new BodyPart { Name="Hat" },
      new BodyPart { Name="Mask", BlendMode=BlendMode.Cutout, CutoffAlpha=0.75f },
  };

    /// <summary>
    /// Creates a new folder by using assetdatabase but checks first if the folder exists.
    /// </summary>
    /// <param name="parentF"></param>
    /// <param name="folderF"></param>
    private static void CreateFolder (string parentF, string folderF)
    {
        var abs = Application.dataPath + "/" + parentF + folderF;

        if (Directory.Exists(abs))
        {
            return;
        }

        AssetDatabase.CreateFolder("Assets" + (!string.IsNullOrEmpty (parentF) ? "/" : "") + parentF, folderF);
    }

    /// <summary>
    /// Copy from the current directory, include subdirectories.
    /// </summary>
    /// <param name="sourceDirName"></param>
    /// <param name="destDirName"></param>
    /// <param name="copySubDirs"></param>
    private static void DirectoryCopy (string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }
}
