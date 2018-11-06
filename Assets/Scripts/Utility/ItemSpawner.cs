/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// You can use this script on game maps. It spawns item with multiple positions with their radius.
/// Also a rate system.
/// For example you have 3 items in ItemSpawner
/// Dagger, rate = 50
/// Sword, rate = 90
/// Pistol, rate = 60
/// 
/// The spawner will select Pistol(%30) Sword (%45) Dagger (%25) to drop.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        [Tooltip("An object named 'prefabName' must be placed at Resources/Spawnables/Items")]
        public string prefabName;
        [Tooltip("Spawn frequency")]
        public double spawnRate;
    }

    [System.Serializable]
    public class Spawnable
    {
        public List<SpawnableItem> spawnables = new List<SpawnableItem>();

        [Tooltip ("This is the value of blocking. Default=5, if any SNet_Identity in 10 meters radius, it won't spawn.")]
        public float checkDistance = 5;

        ProportionValue<string>[] clist;
        public string requestPrefabByRate ()
        {
            if (clist.Length == 1)
                return spawnables[0].prefabName;
            
            return clist.ChooseByRandom();
        }

        public void Init()
        {
            nextSpawn = Time.time + spawnTime;

            clist = new ProportionValue<string>[spawnables.Count];
            for (int a = 0; a < spawnables.Count; a++)
                clist[a] = ProportionValue.Create(spawnables[a].spawnRate, spawnables[a].prefabName);
        }

        public GameMap.SpawnPosition position = new GameMap.SpawnPosition();

        public float spawnTime = 0.5f;

        [HideInInspector]
        public float nextSpawn;
    }

    [Tooltip ("Spawn all items at start.")]
    public bool SpawnAtStart = false;

    public List<Spawnable> spawnables = new List<Spawnable>();

    // Use this for initialization
	void Start ()
    {
        if (spawnables.Count == 0)
            Destroy(gameObject);

        nextUpdate = Time.time + 1;

        for (int i = 0; i < spawnables.Count; i++)
        {
            spawnables[i].Init();
        }

        if (SpawnAtStart && SNet_Network.instance.isHost())
        {
            for (int i = 0; i < spawnables.Count; i++)
            {
                Spawn(spawnables[i]);
            }
        }
	}

    float nextUpdate = 0;

    void Update ()
    {
        if (Facepunch.Steamworks.Client.Instance == null || nextUpdate > Time.time)
            return;

        nextUpdate = Time.time + 1;

        bool isHost = SNet_Network.instance.isHost();

        for (int i = 0; i < spawnables.Count; i++)
        {
            if (isHost && spawnables[i].nextSpawn > Time.time)
                continue;

            spawnables[i].nextSpawn = Time.time + spawnables[i].spawnTime;

            if (!isHost)
            {
                return;
            }

            Spawn(spawnables[i]);
        }
    }

    void Spawn(Spawnable spawnable)
    {
        Vector3 position = spawnable.position.get();
        if (Physics.OverlapSphere(position, spawnable.checkDistance).ToList().Find(x => x.GetComponent<SNet_Identity>()))
            return; // Closest identity detected range sphere

        string target = spawnable.requestPrefabByRate();
        SNet_Network.Spawn_Item si = new SNet_Network.Spawn_Item
            (target, position + Vector3.up, Random.rotation, Vector3.zero, 0);
        SNet_Network.instance.Send_Message(si);
    }
}
