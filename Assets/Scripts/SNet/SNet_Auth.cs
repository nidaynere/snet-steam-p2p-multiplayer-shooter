/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 15 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;

/// <summary>
/// SNet_Auth is for the Valve Anti Cheat.
/// There is no Auth.StartSession method on Client.Instance at Facepunch.Steamworks, so we had to create a local server in background to use Auth methods.
/// Don't worry, this is harmless.
/// </summary>
public class SNet_Auth : MonoBehaviour
{
    public static void Validate(ulong steamid)
    {
        Transform member = SNet_Manager.instance.lobbyMemberHolder.Find(steamid.ToString());

        if (member != null)
        {
            Validate(member);
        }
    }

    public static void Validate(Transform member)
    {
        member.Find("VACError").gameObject.SetActive(false);
        member.Find("VAC").GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0, 1, 0, 0.5f);
    }
}
