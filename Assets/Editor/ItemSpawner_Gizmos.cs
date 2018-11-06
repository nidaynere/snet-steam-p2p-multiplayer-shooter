using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ItemSpawnerGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForItemSpawner(ItemSpawner scr, GizmoType gizmoType)
    {
        foreach (ItemSpawner.Spawnable s in scr.spawnables)
        {
            Gizmos.color = new Color (0,1,0,0.2f);
            Gizmos.DrawSphere(s.position.position, s.position.radius);
        }
    }
}
