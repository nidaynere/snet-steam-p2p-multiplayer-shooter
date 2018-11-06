/*! 
@author Veli V http://wiki.unity3d.com/index.php?title=MouseOrbitImproved
@lastupdate 13 February 2018
*/

using UnityEngine;

/// <summary>
/// This is like an event listener. Required a key. Like hold tab to see scores, escape to open menu etc.
/// </summary>
[RequireComponent(typeof (UIVisibility))]
public class PanelActivator : MonoBehaviour
{
    /// <summary>
    /// Button name defined on Input Manager
    /// </summary>
    [Tooltip ("Button name defined on Input Manager")]
    public string buttonName;

    public UIVisibility targetPanel;

    public bool requireGameStarted = false;
    public bool hideMe = false;

    UIVisibility visibility;
    private void Start()
    {
        visibility = GetComponent<UIVisibility>();
    }

    [Tooltip ("If hold is enabled, user must hold the button to keep the panel open")]
    public bool hold = true;
	// Update is called once per frame
	void Update ()
    {
        if (visibility.alphaDown != 0)
            return;

        if (requireGameStarted && !SNet_Manager.instance.gameStatus.iS)
            return;

        if (hold)
        {
            if (Input.GetButtonDown(buttonName))
            {
                if (InputBlocker.isBlocked())
                    return;

                targetPanel.Open();
            }


            if (Input.GetButtonUp(buttonName))
            {
                if (InputBlocker.isBlocked())
                    return;

                targetPanel.Open(false);
            }
        }
        else
        {
            if (Input.GetButtonDown(buttonName))
            {
                if (InputBlocker.isBlocked())
                    return;

                targetPanel.Open (!targetPanel.activeSelf);
                if (targetPanel.activeSelf && hideMe)
                    visibility.Open(false);
            }
        }
	}
}
