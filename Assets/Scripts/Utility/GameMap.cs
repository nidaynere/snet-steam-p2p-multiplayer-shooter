/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// Every game map must have this on one of the gameobjects.
/// </summary>
public class GameMap : MonoBehaviour
{
    /// <summary>
    /// Player respawn time. I habit this 'resurrect' word because of Ultima Online. It's respawn.
    /// </summary>
    [Tooltip ("Player respawn time. I habit this 'resurrect' word because of Ultima Online. It's respawn.")]
    public float PlayerResurrectTime = 5f;

    /// <summary>
    /// A game map can have multiple spawn points
    /// </summary>
    [System.Serializable]
    public class SpawnPosition
    {
        public Vector3 position;
        public float radius = 1;

        public Vector3 get()
        {
            Vector3 vPos = new Vector3(Random.Range(position.x - radius, position.x + radius), 0, Random.Range(position.z - radius, position.z + radius));
            vPos.y = HeightRaycaster.GroundY (vPos);

            return vPos + Vector3.up;
        }
    }
    /// <summary>
    /// Current map.
    /// </summary>
    public static GameMap instance;

    [Tooltip("Players will have this item by default.")]
    public string startingItem; // Starting items wont dropped from players when die, it will be removed from inventory.

    [Tooltip("List of player spawn positions with their radius")]
    public SpawnPosition[] spawnPositions = new SpawnPosition[0];

    // Use this for initialization
    void Start()
    {
        instance = this;
    }

    public static Vector3 getSpawnPosition()
    {
        return instance.spawnPositions[Random.Range(0, instance.spawnPositions.Length)].get();
    }
}
