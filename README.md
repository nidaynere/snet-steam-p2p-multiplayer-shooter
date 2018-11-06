# snet-steam-p2p-multiplayer-shooter

# What is this?
This is a peer2peer multiplayer shooter template, using Steam API to make a P2P networking via Facepunch Steamworks.
https://www.youtube.com/watch?v=zN4kGZSSVOk

# Playable demos
https://drive.google.com/drive/folders/1zUl1Z3dIiZhCAnc7mR3zg9brvpq-_B7-

# Features
- Steam - Matchmaking (Lobbies). Inviting players on Steam, auto joining.
- Steam - Voice Chat.
- Steam - Proper VAC implementation, implemented as described in here https://partner.steamgames.com/doc/features/auth.
- Steam - Text Chat.
- Code is optimized & documented.
- Almost no lag. Low bandwidth on even a crowded game.
- Networking is similar with Unity.Networking. SNet_Animator, SNet_Transform, SNet_Rigidbody do their job but if it's necessary.
- Third person controller includes two layered player animator (Upper body and lower body).
- Item IK system. Different IK Settings on different bone structures is possible. You can find more information on this tutorial video https://www.youtube.com/watch?v=L8Xovj4FW_s&feature=youtu.be
- 3 weapon types:
Melee (Raycast)
Bullet (Raycast)
Throwable (Instantiates prefab. Used for Grenades, Rocket Launchers etc.)
- Multi-seat vehicles.
- Basic Car Controller (Unity's default wheel physics)
- Includes basic helicopter controller for tutorial to implement your own vehicle contollers to SNet_Vehicle (Even a horse:))
- Ragdolled players.

# Installation
There is one step. You should copy the files in Plugins\Facepunch.Steamworks\Native to - /../Project root.
If you have initialization errors on the console you didn't do this step correct.

# Requirements
UNITY 2018.2.14f1

# Additional Information
Steam must be opened to play this.

First start maybe failed because of Valve Anticheat. Steam adds Spacewar game on your library at first start. So you may need to do an open/close.
(This problem won't be happened on a proper app id, we tested it.)

Allow the firewall question in the background if you face it, to pass the Valve Anticheat.

You may not able to run the demo sometimes because Steam Test Lobby of Spacewar. 
We suggest you create your app on Steam and enter your appid to Facepunch_Client on SteamManager gameobject.

As you know this is using Facepunch Steamworks.
https://github.com/Facepunch/Facepunch.Steamworks