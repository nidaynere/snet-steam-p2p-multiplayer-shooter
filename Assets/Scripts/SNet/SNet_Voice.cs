/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facepunch.Steamworks;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System;

/// <summary>
/// It controls user voices.
/// </summary>
public class SNet_Voice : MonoBehaviour
{
    /*
     * THIS LIST MUST BE CLEARED AT GAME LOAD & GAME UNLOAD.
     * */
    public List<VoiceData> vdList = new List<VoiceData>();

    [System.Serializable]
    public class VoiceData
    {
        public VoiceData(ulong _id)
        {
            id = _id;
            SNet_Controller controller = SNet_Controller.list.Find(x => x.identity.identity == id);
            aSource = (controller != null) ? controller.identity.asource : instance.source2d;
        }

        public ulong id;
        public AudioSource aSource;
        public MemoryStream ms = new MemoryStream();

        /// <summary>
        /// Played clip must be offseted the next one. 0 means not playing.
        /// </summary>
        public float playOffset = 0;

        /// <summary>
        /// Current step of Memory stream. 1 means 500.000, 2 means 1.000.000
        /// </summary>
        public int currentStep = 0;

        /// <summary>
        /// Reset playback
        /// </summary>
        public void Reset()
        {
            playOffset = 0;
            currentStep = 0;
            ms = new MemoryStream();
        }
    }

    public void OnVM(byte[] data, int length, ulong sender)
    {
        SNet_Identity go = null;
        SNet_Identity.find(sender, out go);

        Transform lobbyPlayer = SNet_Manager.instance.lobbyMemberHolder.Find(sender.ToString());

        bool voiceDisable = false;
        if (lobbyPlayer.Find("voiceDisable").GetComponent<Toggle>().isOn)
            voiceDisable = true;

        if (go != null && go.controller.UI_PlayerAgent != null)
        {
            Transform voice = go.controller.UI_PlayerAgent.Find("voice");
            voice.GetComponent<UIVisibility>().Open();

            GameObject g = voice.Find("blocked").gameObject;
            if (g.activeSelf != voiceDisable)
                g.SetActive(voiceDisable);
        }

        lobbyPlayer.Find("speaking").GetComponent<UIVisibility>().Open();

        if (voiceDisable)
            return;

        VoiceData vd = vdList.Find(x => x.id == sender);
        if (vd == null)
        {
            vd = new VoiceData(sender);
            vdList.Add(vd);
        }

        if (Client.Instance.Voice.Decompress(data, length, vd.ms))
        {
            if (vd.ms.Length > stepSize && vd.playOffset == 0)
            {
                // The data is enough to play.
                vd.playOffset = 1; 
            }
        }
    }

    public static SNet_Voice instance;

    public static int stepSize = 10000;

    AudioSource source2d;
    // Use this for initialization
    void Awake ()
    {
        if (instance == null)
        {
            instance = this;
            source2d = GetComponent<AudioSource>();
        }
	}

    public void Init()
    {
        Client.Instance.Voice.OnCompressedData = (buffer, length) =>
        {
            ulong[] members = Client.Instance.Lobby.GetMemberIDs();
            int membersLength = members.Length;

            for (int i = 0; i < membersLength; i++)
            {
                Client.Instance.Networking.SendP2PPacket(members[i], buffer, length, Networking.SendType.UnreliableNoDelay, 1);
            }
        };
    }

    void Update ()
    {
        Client.Instance.Voice.WantsRecording = Input.GetKey(KeyCode.Q) && SNet_Manager.instance.memberCount > 0;

        foreach (VoiceData vd in vdList)
        {
            if (vd.playOffset != 0 && vd.playOffset < Time.time)
            {
                int tStep = vd.currentStep * stepSize;
                vd.currentStep++;
                int take = (vd.ms.Length < tStep + stepSize) ? ((int)vd.ms.Length - tStep) : stepSize;

                byte[] data = Extensions.Get (vd.ms.ToArray(), tStep, take);

                AudioClip ac = AudioClip.Create("wav", data.Length, 1, 22050, false);

                float[] af = new float[data.Length / 2];
                for (int i = 0; i < af.Length; i++)
                {
                    try
                    {
                        af[i] = (short)(data[i * 2] | data[i * 2 + 1] << 8) / 32768.0f;
                    }
                    catch
                    {
                        break;
                    }
                }

                ac.SetData(af, 0);

                if (take < stepSize)
                {
                    /*
                     * DONE
                     * */
                    vd.Reset();
                }
                else 
                    vd.playOffset = Time.time + ac.length/2;

                vd.aSource.PlayOneShot(ac);
            }
        }
	}
}
