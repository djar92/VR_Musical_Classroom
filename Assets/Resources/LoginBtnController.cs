using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public enum Instrument
{
    QwertyGuitar, QwertyPiano, Drums
}

// Login button controller for the menu where you pick an instrument and your name
// You must log in before the game scene starts, and your login data is stored in static variables
// in this class
public class LoginBtnController : MonoBehaviour
{
    public static string loadedName;
    public static Instrument loadedInstrument;

    Text text;
    Dropdown dropdown;
    
    void Start()
    {
        text = GameObject.Find("Canvas/InputField/Text").GetComponent<Text>();
        dropdown = GameObject.Find("Canvas/Dropdown").GetComponent<Dropdown>();

        foreach (var e in Enum.GetValues(typeof(Instrument)))
        {
            dropdown.options.Add(new Dropdown.OptionData(e.ToString()));
        }

        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    public void OnLoginBtnClick()
    {
        loadedName = text.text == "" ? "Player1" : text.text;
        loadedInstrument = (Instrument)dropdown.value;
        Debug.Log("Loaded: " + loadedName + ", " + loadedInstrument);
        SceneManager.LoadScene("scene1");
    }
}
