using UnityEngine;
using System.Collections.Generic;
using System;

// REMINDER
// - Average pitch = fog density
// - Key up issue

// The controller for an object which has a light and particle system
// which will display visualizations in response to the recent intervals played
// on some target instrument
public class VizController : MonoBehaviour {

    // Assume color-wise, fifths = fourths, sixths = thirds, etc.
    public static Color[] intervalColors = {
        Color.white, // Octave
        Color.Lerp(Color.red, Color.yellow, 0.35f), Color.yellow, // Seconds
        Color.Lerp(Color.yellow, Color.green, 0.35f), Color.blue,  // Thirds
        Color.Lerp(Color.red, Color.blue, 0.68f), Color.red // Fourth, tritone
    };

    public static float maxIntensity = 15.0f, fadeTime = 2.0f, particleMaxSpeed = 15.0f, intervalDecay = 0.85f,
        maxFogDensity = 0.09f, lessFogPerSemi = 0.009f;
    public static int baseSemitone = -12;

    public static Color SemiToColor(int semitones)
    {
        int n = Mathf.Abs(semitones) % 12; // 0-11
        if (n > 6) // 0-6 is ascending, so up to and including the tritone
            n = 12 - n; // 7-11 comes back down descending
        return intervalColors[n];
    }

    public static int GetInterval(int noteA, int noteB)
    {
        return Mathf.Abs(noteB - noteA) % 12;
    }

    // The instrument to visualize, set through Unity editor
    public GameObject instrument; // Warning: Assume this is only set once in the Unity editor!
    InstrumentController instCtl; // Because this var doesn't auto update from the above

    // Visualization objects and components
    new GameObject light;
    GameObject particles;
    Light lightComp;
    ParticleSystem particleSys;

    // Current intervals being played, this is basically an ordered hash table
    int[] intervals = new int[12]; // Indices to intervalOrder
    List<int> intervalOrder = new List<int>();

    float timeLastKeyPress = 0.0f;

    void Start () {
        // Create the visual objects to modify when a note is pressed
        light = new GameObject();
        light.transform.parent = transform;
        light.transform.localPosition = Vector3.zero;
        light.transform.localRotation = Quaternion.AngleAxis(90.0f, Vector3.right);
        lightComp = light.AddComponent<Light>();
        lightComp.type = LightType.Spot;
        lightComp.spotAngle = 90.0f;
        lightComp.intensity = 0.0f;
        lightComp.range = 20.0f;
        
        particles = new GameObject();
        particles.transform.parent = transform;
        particles.transform.position = instrument.transform.position;
        particles.transform.localRotation = Quaternion.AngleAxis(-90.0f, Vector3.right);
        particleSys = particles.AddComponent<ParticleSystem>();
        particleSys.startSpeed = particleMaxSpeed;
        particleSys.gravityModifier = 0.2f;
        particleSys.startSize = 0.1f;   
        var emission = particleSys.emission;
        emission.enabled = false;
        emission.rate = 70;
        var shape = particleSys.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 2.2f;

        // Subscribe when notes are played
        instCtl = instrument.GetComponent<InstrumentController>();
        instCtl.noteDown += NoteDown;
    }

    void Update()
    {
        var emission = particleSys.emission;

        if (timeLastKeyPress == 0.0f)
        {
            // Dark at start of game
            lightComp.intensity = 0.0f;
            emission.enabled = false;
            return;
        }
        
        // Make the light fade away if notes stop being played
        lightComp.intensity = maxIntensity * (Mathf.Max(0.0f, fadeTime - Time.time + timeLastKeyPress) / fadeTime);
        if (RenderSettings.fogDensity < maxFogDensity) RenderSettings.fogDensity += maxFogDensity * Time.deltaTime;
        if (RenderSettings.fogDensity >= maxFogDensity) RenderSettings.fogDensity = maxFogDensity;
        // No particles if completely faded
        emission.enabled = lightComp.intensity != 0.0f;
        particleSys.startSpeed = particleMaxSpeed * lightComp.intensity / maxIntensity;
    }

    int NoteComparer(Note a, Note b)
    {
        if (a.semitone == b.semitone)
            return 0;
        else
            return a.semitone > b.semitone ? 1 : -1;
    }

    // When a note is being played, update the colors
    void NoteDown(Note note)
    {
        timeLastKeyPress = Time.time;
        var recent = instCtl.recentNotes;
        var toArray = recent.ToArray();       
        Array.Sort<Note>(toArray, NoteComparer);

        //////////////////////////////////////////////////////
        // Set the light color to average of interval colors

        // Clear intervals
        for (int i = 0; i < intervals.Length; ++i)
            intervals[i] = -1;
        intervalOrder.Clear();

        // Find all intervals being played
        if (toArray.Length == 1)
        {
            // Special case: one note = just an octave
            intervals[0] = 0;
            intervalOrder.Add(0);
        }
        else
        {
            for (int i = 0; i < toArray.Length; ++i)
            {
                for (int j = i + 1; j < toArray.Length; ++j)
                {
                    var interval = GetInterval(toArray[j].semitone, toArray[i].semitone);
                    if (intervals[interval] == -1)
                    {
                        // If interval not seen yet, mark it as such, and record the order
                        intervalOrder.Add(interval);
                        intervals[interval] = intervalOrder.Count - 1; // Store the index for the intervalOrder entry
                    }
                }
            }
        }

        // Average their colors with decay, set this to the light color
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < intervalOrder.Count; ++i)
        {
            var color = SemiToColor(intervalOrder[i]);
            var asVec = new Vector3(color.r, color.g, color.b);
            // Decay the color strength of intervals higher up in the chord (my hypothesis)
            // Based on observation that:
            // - Major/minor triads have the same intervals, but sound different
            // - 2nd inversion major triads sound weak, because of the fourth at the bottom, despite same intervals as other inversions
            sum += asVec * Mathf.Pow(intervalDecay, i);
        }
        sum /= intervalOrder.Count;
        lightComp.color = new Color(sum.x, sum.y, sum.z);
        RenderSettings.fogDensity = Mathf.Max(0.0f, maxFogDensity - lessFogPerSemi * (note.semitone - baseSemitone));

        //////////////////////////////////////////////////////////////
        // Set the particle color to the latest interval color

        // Special case: one note is just an octave
        int mostRecentInt = recent.Count == 1 ? 0 : GetInterval(recent[recent.Count - 1].semitone, recent[recent.Count - 2].semitone);
        particleSys.startColor = SemiToColor(mostRecentInt);
    }
}
