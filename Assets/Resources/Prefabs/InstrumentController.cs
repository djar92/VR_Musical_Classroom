using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

// Information about a single note
public class Note
{
    public AudioClip clip { get; set; }
    public AudioMixerGroup mixerGroup { get; set; }
    public int semitone { get; set; }
    public Color color { get; set; }
    public Material material { get; set; }
}

// The script for a single key game object
public class KeyController : MonoBehaviour
{
    public List<GameObject> dupes; // List of keys with same semitone as this one
    public Note note; // The original note
    AudioSource source; // Each key has its own audio source created by InstrumentController, null means it is an unuseable key

    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void Play()
    {
        Debug.Assert(note != null);
        // Play sound and animate every duplicate key
        source.Play();
        dupes.ForEach(a => a.transform.Translate(0.0f, -InstrumentController.downShift, 0.0f, Space.Self));
    }

    public void Release()
    {
        Debug.Assert(note != null);
        // Animate every duplicate key
        dupes.ForEach(a => a.transform.Translate(0.0f, InstrumentController.downShift, 0.0f, Space.Self));
    }
}

// Template for different kinds of instruments
public abstract class InstrumentController : MonoBehaviour, NetworkSupport<KeyCode>
{
    public static int keyRows = 4, keyColumns = 10;
    public static string order = "1234567890qwertyuiopasdfghjkl;zxcvbnm,./";
    public static byte[] ascii = Encoding.ASCII.GetBytes(order);
    public static float keyWidth = 0.25f, rowShift = 0.09f, downShift = 0.05f, fillWidth = keyWidth + 0.01f, inactiveDrop = downShift;
    public static Color noNoteColor = new Color(0.05f, 0.01f, 0.01f);
    public static byte numLastNotes = 5;

    // The instrument specific implementation
    public abstract Note GetNote(char c);

    public List<Note> recentNotes { get; private set; }
    public Action<Note> noteDown; // Subscribe to get notifications

    Dictionary<KeyCode, Note> noteLookup = new Dictionary<KeyCode, Note>();
    Dictionary<KeyCode, GameObject> objLookup = new Dictionary<KeyCode, GameObject>();
    Dictionary<int, List<GameObject>> dupeLookup = new Dictionary<int, List<GameObject>>();

    // Network
    NetworkRedirector<KeyCode> networkRd = null;
    public bool isNetworkSetup() { return networkRd != null; }

    public void SetupNetwork(int ownerID)
    {
        if (isNetworkSetup()) throw new Exception("Network already set up");
        networkRd = new NetworkRedirector<KeyCode>(this, ownerID);
    }

    void Start()
    {
        recentNotes = new List<Note>();
        Debug.Assert(keyRows * keyColumns == order.Length);

        // Use the custom instrument implementation to get which sounds to play on each key
        for (int i = 0; i < order.Length; ++i)
        {
            noteLookup[(KeyCode)ascii[i]] = GetNote(order[i]);
        }
        
        // Create a virtual QWERTY keyboard
        int counter = 0;
        // For each key
        for (int i = 0; i < keyRows; ++i)
        {
            for (int j = 0; j < keyColumns; ++j)
            {
                KeyCode keyCode = (KeyCode)ascii[counter];

                // Find the note properties
                Note note = null;
                noteLookup.TryGetValue(keyCode, out note);

                // Make the 3D cube
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = gameObject.transform;
                cube.transform.localScale = new Vector3(keyWidth, keyWidth, keyWidth);
                cube.transform.localPosition = new Vector3(i * rowShift + j * fillWidth, note == null ? -inactiveDrop : 0.0f, -i * fillWidth);
                var meshRend = cube.GetComponent<MeshRenderer>();
                if (note != null && note.material != null) meshRend.material = note.material;
                meshRend.material.color = note == null ? noNoteColor : note.color;
                objLookup[keyCode] = cube;

                // Add a controller
                var keyCtr = cube.AddComponent<KeyController>();

                if (note != null)
                {
                    // Group duplicates
                    List<GameObject> arr;
                    if (!dupeLookup.TryGetValue(note.semitone, out arr))
                    {
                        arr = dupeLookup[note.semitone] = new List<GameObject>();
                    }
                    arr.Add(cube);
                    keyCtr.dupes = arr;
                    keyCtr.note = note;

                    // Add an audio source, copying the note properties
                    var keySrc = cube.AddComponent<AudioSource>();
                    keySrc.pitch = Mathf.Pow(2.0f, note.semitone / 12.0f);
                    keySrc.playOnAwake = false; // This is by default true
                    keySrc.clip = note.clip;
                    keySrc.outputAudioMixerGroup = note.mixerGroup;
                    keySrc.spatialize = true;
                    keySrc.spatialBlend = 1.0f;
                }

                counter++;
            }
        }
    }

    public void NoteEvent(bool isDown, KeyCode note)
    {
        GameObject cube;

        if (isDown)
        {   
            if (objLookup.TryGetValue(note, out cube)) // Find the key object
            {
                var ctl = cube.GetComponent<KeyController>();   
                if (ctl.note != null) // If it is a playable key
                {
                    // Play it and keep the last few played notes
                    ctl.Play();
                    if (recentNotes.Count >= numLastNotes) recentNotes.RemoveAt(0);
                    recentNotes.Add(ctl.note);
                    noteDown(ctl.note);
                }
            }
        }
        else
        {   
            if (objLookup.TryGetValue(note, out cube)) // Find the key object
            {
                // Release the note
                var ctl = cube.GetComponent<KeyController>();
                if (ctl.note != null)
                {
                    ctl.Release();
                }
            }
        }
    }

    void Update()
    {
        // Not set up or is a network instrument => Cannot play it with keys
        if (networkRd == null || !networkRd.isLocal) return;

        for (int i = 0; i < ascii.Length; ++i)
        {
            KeyCode keyCode = (KeyCode)ascii[i];
            if (Input.GetKeyDown(keyCode))
                networkRd.Play(true, keyCode);
            else if (Input.GetKeyUp(keyCode))
                networkRd.Play(false, keyCode);
        }
    }
}
