using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;

// Implements a piano
public class QwertyPianoController : InstrumentController
{
    public static string usedKeys = "zxcvbnm,./asdfghjkl;wetyuop2356790";
    public static byte[] semitones = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 1, 3, 6, 8, 10, 13, 15, 1, 3, 6, 8, 10, 13, 15 };
    public static string isBlack = "0000000000000000000011111111111111";
    public static int semitoneShift = -12;

    static Dictionary<char, byte> semitoneLookup = new Dictionary<char, byte>();
    static Dictionary<char, bool> isWhiteLookup = new Dictionary<char, bool>();
    static QwertyPianoController()
    {
        Debug.Assert(usedKeys.Length == semitones.Length && usedKeys.Length == isBlack.Length);
        for (byte i = 0; i < usedKeys.Length; ++i)
        {
            semitoneLookup[usedKeys[i]] = semitones[i];
            isWhiteLookup[usedKeys[i]] = isBlack[i] == '0';
        }
    }

    public AudioClip clip;
    public AudioMixerGroup mixerGroup;
    public Material material = null;

    public override Note GetNote(char c)
    {
        byte semitone;
        if (semitoneLookup.TryGetValue(c, out semitone))
        {
            Note note = new Note();
            note.clip = clip;
            note.mixerGroup = mixerGroup;
            note.color = isWhiteLookup[c] ? Color.white : Color.Lerp(Color.black, Color.grey, 0.8f);
            note.semitone = semitone + semitoneShift;
            note.material = material;
            return note;
        }
        else
        {
            return null;
        }
    }
}
