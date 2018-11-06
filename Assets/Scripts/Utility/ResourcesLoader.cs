/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Instead of Resources.Load, use this. prefabs list will be loaded at game start. you can find your prefab by calling ResourcesLoader.prefabs.Find (x=>x.name == yourprefabname);
/// </summary>
public class ResourcesLoader : MonoBehaviour
{
    public static List<Transform> prefabs = new List<Transform>();
    public static List<Sprite> icons = new List<Sprite>();

    public static bool isLoaded = false;
	// Use this for initialization
	void Start ()
    {
        if (isLoaded)
        {
            SNet_Manager.instance.panel_Loading.Open();
            return;
        }            

        isLoaded = true;

        prefabs.AddRange(Resources.LoadAll<Transform>("Spawnables/PlayerCharacters").ToList ());
        prefabs.AddRange(Resources.LoadAll<Transform>("Spawnables/NPCs").ToList());
        prefabs.AddRange(Resources.LoadAll<Transform>("Spawnables/Items").ToList());
        prefabs.AddRange(Resources.LoadAll<Transform>("Spawnables/Objects").ToList());
        prefabs.AddRange(Resources.LoadAll<Transform>("Particles").ToList());
        icons.AddRange(Resources.LoadAll<Sprite>("Icons").ToList());
        SNet_Manager.instance.panel_Loading.Open(false);
    }
}
