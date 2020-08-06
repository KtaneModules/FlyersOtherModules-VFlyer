﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using System;
using uerng = UnityEngine.Random;

public class NonverbalSimonHandler : MonoBehaviour {

    public Light lightR, lightG, lightO, lightY;
    public MeshRenderer[] buttonmesh = new MeshRenderer[4];
    public KMBombInfo bombInfo;
    public KMAudio someAudio;
    public KMBombModule modself;
    public KMSelectable[] buttons = new KMSelectable[4];
    public Material[] materials = new Material[4];

    private int batteryCount;
    private int indicatorOffset;
    private string serialno;
    public AudioClip[] sfxs = new AudioClip[4];


    private List<int> flashes = new List<int>(); // A list of all flashes in ther serial number
    private int[] correctInputs = new int[] {-1,-1,-1,-1}; // For Red, Orange, Yellow, Green flashes respecitvely
    private int currentpos = 0;
    private readonly string[] colorlist = new string[] { "Red", "Orange", "Yellow", "Green" };
    private readonly string[] pressNames = new string[] { "PressR", "PressO", "PressY", "PressG" };


    private static int modid_counter = 1;
    private int modid;
    private bool isActive = false;
    private int stagesCompleted = 0;
    private int stagesToComplete = 3;
    private bool canPlaySound = false, hasStruck = false, forceMute = false;

    // Use this for initialization
    public List<string> GrabCombinedFlashes() // In case of Souvenir Support. 
    {
        List<string> output = new List<string>();
        foreach (int oneFlash in flashes)
            output.Add(colorlist[oneFlash]);
        return output;
    }
    void Start() {
        modid = modid_counter++;
        stagesToComplete = uerng.Range(3,6);
        modself.OnActivate += delegate ()
        {
            // Calculate based on edgework shown.
            batteryCount = bombInfo.GetBatteryCount();
            int onIndicators = bombInfo.GetOnIndicators().Count();
            int offIndicators = bombInfo.GetOffIndicators().Count();
            indicatorOffset = onIndicators - offIndicators;
            serialno = bombInfo.GetSerialNumber();
            // Orange Flash Divider
            correctInputs[1] = 2;
            if (batteryCount > 1)
            {
                correctInputs[1] = 3;
                if (batteryCount > 3)
                {
                    correctInputs[1] = 1;
                    if (batteryCount > 5)
                    {
                        correctInputs[1] = 0;
                    }
                }
            }
            QuickDebug("The correct input for all Orange flashes is "+ colorlist[correctInputs[1]]);
            // Red Flash Divider
            string serinter = "";
            for (int x = 0; x < serialno.Length; x++)
            {
                string part = serialno.Substring(x, 1).ToUpper();
                if (part.RegexMatch(@"^[0-9]$"))
                {
                    serinter += "#";
                }
                else if (part.RegexMatch(@"^[A-Z]$"))
                {
                    serinter += "X";
                }
                else
                {
                    serinter += "?";
                }
            }
            if (serinter.Equals("###XX#"))
            {
                correctInputs[0] = 3;
            }
            else if (serinter.Equals("#X#XX#"))
            {
                correctInputs[0] = 0;
            }
            else if (serinter.Equals("XX#XX#"))
            {
                correctInputs[0] = 1;
            }
            else if (serinter.Equals("X##XX#"))
            {
                correctInputs[0] = 2;
            }
            if (correctInputs[0] >= 0 && correctInputs[0] < 4)
            {
                QuickDebug("The correct input for all Red flashes is " + colorlist[correctInputs[0]]);
            }
            else
                QuickDebug("Red cannot flash due to an unfamilar serial number.");
            // Yellow Flash Divider
            correctInputs[2] = indicatorOffset < 0 ? 0 : indicatorOffset > 0 ? 3 : 1;
            QuickDebug("The correct input for all Yellow flashes is " + colorlist[correctInputs[2]]);
            // Green Flash Divider
            if (bombInfo.IsPortPresent(Port.Serial))
            {
                correctInputs[3] = 1;
            }
            else if (bombInfo.IsPortPresent(Port.PS2))
            {
                correctInputs[3] = 0;
            }
            else if (bombInfo.IsPortPresent(Port.RJ45))
            {
                correctInputs[3] = 3;
            }
            else
            {
                correctInputs[3] = 2;
            }
            QuickDebug("The correct input for all Green flashes is " + colorlist[correctInputs[3]]);
            GenerateValidFlash();
            isActive = true;
        };
        
        for (int x = 0; x < buttons.Length; x++)
        {
            int y = x;
            buttons[x].OnInteract += delegate()
            {
                canPlaySound = !forceMute;
                buttons[y].AddInteractionPunch();
                someAudio.PlayGameSoundAtTransform( KMSoundOverride.SoundEffect.BigButtonPress, transform);
                HandlePress(y);
                return false;
            };
        }
        Light[] lights = new Light[] { lightR, lightO, lightY, lightG };
        float scalar = transform.lossyScale.x;// Account for scale factor for this module being smaller, check KTANE official discord in #modding
        foreach (Light onelight in lights)
        {
            onelight.enabled = false;
            onelight.range *= scalar;
        }
    }
    bool canAutoSolve = false;
    void GenerateValidFlash()
    {
        int toflash = uerng.Range(0, 4);
        while (correctInputs[toflash] < 0 || correctInputs[toflash] >= 4)// Begin generate different flash for invalid presses
        {
            canAutoSolve = true;
            for (int x = 0; x < correctInputs.Length; x++)
            {
                canAutoSolve = canAutoSolve && (correctInputs[x] < 0 || correctInputs[x] >= 4);
            }
            if (canAutoSolve) {
                QuickDebug("The module can auto-solve by just pressing a button, no acceptable presses were created from the options possible.");
                break;
            }
            toflash = uerng.Range(0, 4);
        }
        flashes.Add(toflash);
        QuickDebug("Flash number " + flashes.Count +" is " + colorlist[toflash]);
    }
    bool isFlashing = false;
    bool onCooldown = false;
    IEnumerator FlashSingle(int value)
    {
        Light[] lights = new Light[] { lightR, lightO, lightY, lightG };
        if (value >= 0 && value < lights.Length)
        {
            buttonmesh[value].material.color = new Color(materials[value].color.r + 0.5f, materials[value].color.g + 0.5f, materials[value].color.b + 0.5f, materials[value].color.a);
            lights[value].enabled = true;
            yield return new WaitForSeconds(0.5f);
            lights[value].enabled = false;
            buttonmesh[value].material = materials[value];
        }
        yield return new WaitForSeconds(3f);
        onCooldown = false;
        yield return null;
    }
    
    IEnumerator FlashSequence()
    {
        if (!isFlashing)
        {
            isFlashing = true;
            int currentFlashLength = flashes.Count;
            for (int x = 0; x < flashes.Count && currentFlashLength == flashes.Count && isActive; x++)
            {
                int value = flashes[x];
                Light[] lights = new Light[] { lightR, lightO, lightY, lightG };
                if (value >= 0 && value < lights.Length)
                {
                    buttonmesh[value].material.color = new Color(materials[value].color.r + 0.5f, materials[value].color.g + 0.5f, materials[value].color.b + 0.5f, materials[value].color.a);
                    if (canPlaySound)
                        someAudio.PlaySoundAtTransform(pressNames[value], transform);
                    lights[value].enabled = true;
                    yield return new WaitForSeconds(0.5f);
                    lights[value].enabled = false;
                    buttonmesh[value].material = materials[value];
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield return new WaitForSeconds(3f);
            isFlashing = false;
        }
        yield return null;
    }
	// Update is called once per frame
	void Update () {
        if (isActive)
        {
            if (!isFlashing&&!onCooldown)
            {
                
                currentpos = 0;
                StartCoroutine(FlashSequence());
            }
        }
	}
    void QuickDebug(string toDebug)
    {
        Debug.LogFormat("[❖ #{0}]: {1}", modid, toDebug);
    }

    void HandlePress(int input)
    {
        if (isActive)
        {
            StopCoroutine(FlashSingle(input));
            StopCoroutine(FlashSequence());
            if (canPlaySound)
                someAudio.PlaySoundAtTransform(pressNames[input], transform);
            StartCoroutine(FlashSingle(input));
            onCooldown = true;
            if (flashes.Count > 0 && input == correctInputs[flashes[currentpos]])
            {
                currentpos++;
                if (currentpos >= flashes.Count)
                {
                    currentpos = 0;
                    stagesCompleted++;
                    if (stagesCompleted >= stagesToComplete)
                    {
                        QuickDebug("Enough stages have been completed to disarm the module.");
                        modself.HandlePass();
                        isActive = false;
                        canPlaySound = false;
                    }
                    else
                        GenerateValidFlash();
                }
            }
            else if (canAutoSolve)
            {
                modself.HandlePass();
            }
            else
            {
                modself.HandleStrike();
                QuickDebug("The defuser pressed " + colorlist[input] + " which is wrong for the " + new string[] { "1st", "2nd", "3rd", "4th", "5th" }[currentpos] + " flash.");
                currentpos = 0;
                hasStruck = true;
            }
        }
    }
    // TP Handling
    public readonly string TwitchHelpMessage = "\"!{0} press Red/Orange/Yellow/Green\" to press the specified button in the command. Shorthand abbreviations are acceptable. Presses can be combined by spacing out the commands or as one word when abbreviated, I.E \"!{0} press royg\".";

    IEnumerator TwitchHandleForcedSolve()
    {
        QuickDebug("A force solve has been issued viva TP Handler.");
        while (isActive)
        {
            buttons[correctInputs[flashes[currentpos]]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield return true;
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        string input = command.ToLower();
        List<KMSelectable> kMSelectables = new List<KMSelectable>();
        if (input.RegexMatch(@"^(always)?mute$"))
        {
            yield return null;
            yield return "sendtochat {0}, the sounds will now stop playing from this module.";
            forceMute = true;
            canPlaySound = false;
            yield break;
        }
        else if (input.RegexMatch(@"^press\s[royg]+$"))
        {
            input = input.Substring(6).ToLower();
            foreach (char press in input)
            {
                switch (press)
                {
                    case 'r':
                        kMSelectables.Add(buttons[0]);
                        break;
                    case 'o':
                        kMSelectables.Add(buttons[1]);
                        break;
                    case 'y':
                        kMSelectables.Add(buttons[2]);
                        break;
                    case 'g':
                        kMSelectables.Add(buttons[3]);
                        break;
                    default: yield break;
                }
            }
        }
        else if (input.RegexMatch(@"^press(\s(r(ed)?|o(range)?|y(ellow)?|g(reen)?))+$"))
        {
            input = input.Substring(6).ToLower();
            string[] presses = input.Trim().Split(' ');
            foreach (string press in presses)
            {
                switch (press)
                {
                    case "red":
                    case "r":
                        kMSelectables.Add(buttons[0]);
                        break;
                    case "orange":
                    case "o":
                        kMSelectables.Add(buttons[1]);
                        break;
                    case "yellow":
                    case "y":
                        kMSelectables.Add(buttons[2]);
                        break;
                    case "green":
                    case "g":
                        kMSelectables.Add(buttons[3]);
                        break;
                    default: yield break;
                }
            }
        }
        if (kMSelectables.Any())
        {
            hasStruck = false;
            yield return "multiple strikes";
            for (int x = 0; x < kMSelectables.Count && !hasStruck; x++)
            {
                int idxInput = Array.IndexOf(buttons, kMSelectables[x]);

                yield return null;
                kMSelectables[x].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (hasStruck)
                {
                    yield return string.Format("strikemessage incorrectly pressing {0} for the {1} input!", idxInput < 0 ? "an unknown button" : colorlist[idxInput], new string[] { "1st", "2nd", "3rd", "4th", "5th" }[x]);
                }
            }
            yield return "end multiple strikes";
        }
    }
}
