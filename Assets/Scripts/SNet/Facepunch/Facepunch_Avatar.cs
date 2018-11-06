using UnityEngine;
using Facepunch.Steamworks;
using UnityEngine.UI;

//
// To change at runtime call Fetch( steamid )
//
public class Facepunch_Avatar : MonoBehaviour
{
    public ulong SteamId;

    public void Fetch(ulong steamid)
    {
        if (steamid == 0) return;

        if (Client.Instance == null)
        {
            return;
        }

        SteamId = steamid;
        Client.Instance.Friends.GetAvatar(Friends.AvatarSize.Large, SteamId, (i) => OnImage(i, steamid));
    }

    private void OnImage(Facepunch.Steamworks.Image image, ulong steamid)
    {
        if (steamid != SteamId)
            return;

        if (image == null)
        {
            return;
        }

        var texture = new Texture2D(image.Width, image.Height);

        for (int x = 0; x < image.Width; x++)
            for (int y = 0; y < image.Height; y++)
            {
                var p = image.GetPixel(x, y);

                texture.SetPixel(x, image.Height - y, new UnityEngine.Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
            }

        texture.Apply();

        ApplyTexture(texture);
    }

    private void ApplyTexture(Texture texture)
    {
        var rawImage = GetComponent<RawImage>();
        if (rawImage != null)
            rawImage.texture = texture;
    }
}
