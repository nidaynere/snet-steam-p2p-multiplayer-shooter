using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameMap_Gizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForGameMap(GameMap scr, GizmoType gizmoType)
    {
         foreach (GameMap.SpawnPosition s in scr.spawnPositions)
         {
             Gizmos.color = new Color(1, 0.2f, 0, 0.4f);
             Gizmos.DrawSphere(s.position, s.radius);
         }
    }
}
