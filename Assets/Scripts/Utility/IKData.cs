/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// IKData stores and loads ikdata.json from Resources folder.
/// Every bone structure needs a proper hand ik adjustment for weapons.
/// You can create unlimited ik classes and save them.
/// You can check videos for more information.
/// https://www.youtube.com/watch?v=L8Xovj4FW_s
/// https://www.youtube.com/watch?v=odVGxMrfGEs
/// https://www.youtube.com/watch?v=UrNnjNinY0w
/// 
/// I leave this script uncommented because you should not touch this.
/// </summary>
[ExecuteInEditMode]
public class IKData : MonoBehaviour
{
    [System.Serializable]
    public class data
    {
        public List<PlayerClass> list = new List<PlayerClass>();
    }

    [System.Serializable]
    public class PlayerClass
    {
        public string id = "Default";
        public List<IK> list = new List<IK>();
    }
    
    [System.Serializable]
    public class IK
    {
        public string ItemName = "enter item name";
        public Vector3 objLocalPosition;
        public Vector3 objLocalEulerAngles;
        public Vector3 objLocalScale = Vector3.one;
        public Vector3 leftHandPosition;
        public Vector3 leftHandEulerAngles;
        public Vector3 rightHandPosition;
        public Vector3 rightHandEulerAngles;
    }

    public static IKData instance;
    private void Start()
    {
        if (instance != null)
            return;

        iks = JsonUtility.FromJson<data>(Resources.Load<TextAsset>("ikdata").text);
        instance = this;
    }

    public data iks = new data();

    public bool SaveToFile = false;
	// Update is called once per frame
	void Update ()
    {
        if (!Application.isEditor)
            return;

        if (SaveToFile)
        {
            SaveToFile = false;

            string file = JsonUtility.ToJson (iks);

            File.WriteAllText(Application.dataPath + "/Resources/ikdata.json", file);

#if UNITY_EDITOR
            AssetDatabase.ImportAsset("Assets/Resources/ikdata.json");
#endif
        }
	}

    public static void SaveItemIK (string IKSetting, string itemName,
        Vector3 objPosition, Vector3 objEulerAngles, Vector3 objScale, 
        Vector3 leftHandPosition, Vector3 leftHandEulerAngles,
        Vector3 rightHandPosition, Vector3 rightHandEulerAngles)
    {
        PlayerClass pc = instance.iks.list.Find(x => x.id == IKSetting);
        if (pc == null)
        {
            pc = new PlayerClass();
            pc.id = IKSetting;
            instance.iks.list.Add(pc);
        }

        IK ik = pc.list.Find(x => x.ItemName == itemName);
        if (ik == null)
        {
            ik = new IK();
            ik.ItemName = itemName;
            pc.list.Add(ik);
        }

        ik.objLocalPosition = objPosition;
        ik.objLocalEulerAngles = objEulerAngles;
        ik.objLocalScale = objScale;
        ik.leftHandEulerAngles = leftHandEulerAngles;
        ik.leftHandPosition = leftHandPosition;
        ik.rightHandEulerAngles = rightHandEulerAngles;
        ik.rightHandPosition = rightHandPosition;

        instance.SaveToFile = true;
    }

    public static IK GetItemIK(string IKSetting, string itemName)
    {
        PlayerClass pc = instance.iks.list.Find(x => x.id == IKSetting);
        if (pc == null)
            return new IK ();

        IK ik = pc.list.Find(x => x.ItemName == itemName);
        if (ik == null)
            return new IK();

        return ik;
    }
}
