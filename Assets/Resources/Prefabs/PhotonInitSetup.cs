using System;
using UnityEngine;
using System.Collections.Generic;
using Photon;
using UnityEngine.UI;

using Random = UnityEngine.Random; // Name conflict

// Sets up Photon assuming LoginBtnController is set up
public class PhotonInitSetup : PunBehaviour
{
    public static readonly string roomName = "myRoom";
    public static readonly byte maxPlayers = 3;
    public static readonly Vector3[] playerPositions, spotPositions;
    public static readonly float triLength = 2.0f, floorHeight = 2.0f, spotTriLength = 5.0f, spotHeight = 10.0f;

    static PhotonInitSetup()
    {
        // Max 3 players, and sit in a triangle
        playerPositions = new Vector3[] {
            new Vector3(-triLength, floorHeight, -triLength),
            new Vector3(triLength, floorHeight, -triLength),
            new Vector3(triLength, floorHeight, triLength)
        };
        // Positions of spot lights
        spotPositions = new Vector3[] {
            new Vector3(-spotTriLength, spotHeight, -spotTriLength),
            new Vector3(spotTriLength, spotHeight, -spotTriLength),
            new Vector3(spotTriLength, spotHeight, spotTriLength)
        };
    }

    ///////////////////////////////////////////////////
    // Photon connection ritual

    void Awake()
    {
        PhotonNetwork.logLevel = PhotonLogLevel.Full;
        PhotonNetwork.autoJoinLobby = false;
        PhotonNetwork.automaticallySyncScene = true;
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings("5.4");
        text.text = "Connecting to Photon servers";
        positionIndexToID = new int?[maxPlayers];
    }

    public override void OnConnectedToMaster()
    {
        text.text = "Connecting to a room";
        PhotonNetwork.playerName = LoginBtnController.loadedName;
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions() { IsVisible = false, MaxPlayers =  maxPlayers }, null);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("Player connected: " + newPlayer.name + " " + newPlayer.ID);
    }

    public override void OnDisconnectedFromPhoton()
    {
        text.text = "Disconnected - Please restart";
    }

    public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        text.text = "Create room failed";
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        text.text = "Join room failed";
    }
    
    public override void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        text.text = "Connecting to Photon failed";
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        text.text = "Authentication failed";
    }

    ///////////////////////////////////////////////////

    public Text text; // Displays some text to user
    public Image image; // Background image covering up the scene

    // Look up instrument game object from Photon's ID field
    Dictionary<int, GameObject> instrumentObjLookup = new Dictionary<int, GameObject>();
    int?[] positionIndexToID; // Given the index (0 to maxPlayers - 1), look up the Photon ID for that player
    int? localIndex = null;

    // Creates the instrument and the visualization object
    GameObject CreateInstrument(Instrument instrument, int index, int ownerID)
    {
        if (instrument != Instrument.Drums)
        {
            var container = new GameObject();

            var instrumentPf = Resources.Load<GameObject>("Prefabs/QwertyPiano");
            var vizPf = Resources.Load<GameObject>("Prefabs/Viz");
            var instrumentObj = (GameObject)Instantiate(instrumentPf, container.transform, false);
            var vizObj = (GameObject)Instantiate(vizPf, container.transform, false);
            instrumentObj.transform.position = playerPositions[index];
            vizObj.transform.position = spotPositions[index];
            var instrumentCtl = instrumentObj.GetComponent<InstrumentController>();
            var vizCtl = vizObj.GetComponent<VizController>();
            vizCtl.instrument = instrumentObj;
            instrumentCtl.SetupNetwork(ownerID);

            return container;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    // When the local player joins, give a number from 0 to maxPlayers - 1
    // This is stored in the array/hash-table "positionIndexToID"
    int AssignPlayerIndex(int id)
    {
        for (var i = 0; i < positionIndexToID.Length; ++i)
        {
            if (positionIndexToID[i] == null)
            {
                positionIndexToID[i] = id;
                return i;
            }
        }
        return -1; // Failed, no slots available
    }

    // Inverse operation of simply looking up "positionIndexToID", so this is "idToPositionIndex"
    int GetPlayerIndex(int id)
    {
        for (var i = 0; i < positionIndexToID.Length; ++i)
            if (positionIndexToID[i] == id)
                return i;
        return -1; // Not found
    }

    // Free an index 
    void RemovePlayerIndex(int id)
    {
        var index = GetPlayerIndex(id);
        if (index != -1)
            positionIndexToID[index] = null;
    }

    ///////////////////////////////////////////////////
    // Making instruments upon joining rooms etc.

    public override void OnCreatedRoom()
    {
        Debug.Log("Made a room");
        // If we are the first, we just make our local instrument
        MakeLocalInstrument();
    }

    public override void OnJoinedRoom()
    {
        // If we are not the first, we are waiting for buffered messages 
        // from other network instruments, after we receive all of them,
        // then we make our own, this is to see if we've run out of space or not
        Debug.Log("Joined a room");
    }

    // Another networked (not local) player disconnects
    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Debug.Log("Player disconnected: " + otherPlayer.name + " " + otherPlayer.ID);
        if (!instrumentObjLookup.ContainsKey(otherPlayer.ID)) return; // In case this gets called multiple times
        // Remove container object owned by this player
        GameObject.Destroy(instrumentObjLookup[otherPlayer.ID]);
        // Remove ID entry
        instrumentObjLookup.Remove(otherPlayer.ID);
        // Remove player index
        RemovePlayerIndex(otherPlayer.ID);
    }

    // Put camera on top of the instrument
    void SetupLocalCamera(GameObject instObj, Instrument instrument)
    {
        if (instrument != Instrument.Drums)
        {
            Camera.main.transform.position = instObj.transform.position + new Vector3(1.5f, 2.4f, -1.5f);
        }
        else
        {
            throw new NotImplementedException();
        }

        // Vive instructions...
    }

    void MakeLocalInstrument()
    {
        if (instrumentObjLookup.ContainsKey(PhotonNetwork.player.ID)) return; // In case this gets called multiple times
        // Assign an index
        localIndex = AssignPlayerIndex(PhotonNetwork.player.ID);
        Debug.Assert(localIndex != -1); // No space, but somehow we could connect?
        // Make the local instrument
        var instObj = CreateInstrument(LoginBtnController.loadedInstrument, localIndex.Value, PhotonNetwork.player.ID);
        instrumentObjLookup[PhotonNetwork.player.ID] = instObj;
        // Put camera on top of the instrument
        SetupLocalCamera(instObj.transform.Find("QwertyPiano(Clone)").gameObject, LoginBtnController.loadedInstrument);
        // Tell everyone my instrument type and my index, and all future people who join (AllBuffered)
        photonView.RPC("MakeNetInstrument", PhotonTargets.AllBuffered, LoginBtnController.loadedInstrument, localIndex.Value);
        image.enabled = false; // Show the scene
        text.text = "";
    }

    [PunRPC]
    // Someone else tells me their instrument type and index, make an instrument at their location
    public void MakeNetInstrument(Instrument instrument, int index, PhotonMessageInfo info)
    {
        Debug.Assert(PhotonNetwork.otherPlayers.Length < maxPlayers);
        if (instrumentObjLookup.ContainsKey(info.sender.ID)) return; // In case this gets called multiple times
        positionIndexToID[index] = info.sender.ID;
        var instObj = CreateInstrument(instrument, index, info.sender.ID);
        instrumentObjLookup[info.sender.ID] = instObj;
        // We've created all the other instruments
        if (instrumentObjLookup.Count == PhotonNetwork.otherPlayers.Length)
            MakeLocalInstrument(); // Make our own
    }
}
