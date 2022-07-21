﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using KModkit;

public class ThreeXThreeGridHandlerScript : MonoBehaviour {

    // Use this for initialization
    public Material[] statusLED = new Material[2];
    public Material[] buttonStatus = new Material[2];
    public Material buttonDeactivated;
    public KMAudio sound;
    public KMNeedyModule needySelf;
    public KMBombInfo bombInfo;
    public MeshRenderer[] goalLeds = new MeshRenderer[9];
    public MeshRenderer[] buttonColors = new MeshRenderer[9];
    public Light[] lightKeys = new Light[9];
    public Light[] lightGoal = new Light[9];

    public KMSelectable[] buttonSelectables = new KMSelectable[9];
    public GameObject[] buttons = new GameObject[9];
    private bool[] lightStates = new bool[9];
    private bool[] goalLights = new bool[9];
    private bool mustInvert = false, hasActivated = false, isWarning = false, forceDisable = false;

    static int modIDCnt = 1;
    int modIDLogging;
    IEnumerator warningHandler;

    void Start()
    {
        modIDLogging = modIDCnt++;
        needySelf.OnActivate += delegate
        {
            mustInvert = bombInfo.GetSerialNumberNumbers().LastOrDefault() % 2 != 0;
            // If there is at least 1 number in the serial number, grab the last one and check if it's odd or even. Otherwise set it to false by default.
            Debug.LogFormat("[3x3 Grid #{0}]: The last digit of the serial number is {1} so the defuser must match {2} what is shown on the display.", modIDLogging, mustInvert ? "odd" : "even", mustInvert ? "the inverse of" : "exactly");
        };
        needySelf.OnNeedyActivation += delegate
        {// Generate board with goal presses and mix up interactable board with lit/unilt tiles
            if (forceDisable)
            {
                needySelf.HandlePass();
                return;
            }
            hasActivated = true;
            bool[] choices = new bool[] { true, false };
            for (int pos = 0; pos < lightStates.Count(); pos++)
            {
                lightStates[pos] = choices.PickRandom();
            }

            for (int pos = 0; pos < goalLights.Count(); pos++)
            {
                goalLights[pos] = choices.PickRandom();
            }
        };
        needySelf.OnNeedyDeactivation += delegate// If the module is force-deactivated
        {
            hasActivated = false;
            for (int x = 0; x < buttonColors.Count(); x++)
            {
                buttonColors[x].material = buttonDeactivated;
                lightKeys[x].enabled = false;
            }
        };

        needySelf.OnTimerExpired += delegate
        {
            if (warningHandler != null)
                StopCoroutine(warningHandler);
            isWarning = false;
            if (lightStates.SequenceEqual(goalLights))
            {
                sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                StartCoroutine(PlayCorrectAnim());
            }
            else
            {
                int[] tempVar = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
                string[] positions = { "TL", "ML", "BL", "TM", "MM", "BM", "TR", "MR", "BR" };
                Debug.LogFormat("[3x3 Grid #{0}]: Strike! The smaller displayed has these following lights lit: [ {1} ]; but you left these lit on the bigger display instead: [ {2} ]",
                    modIDLogging,
                    tempVar.Where(a => !goalLights[a] ^ mustInvert).Select(b => positions[b]).Join(", "),
                    tempVar.Where(a => !lightStates[a]).Select(b => positions[b]).Join(", "));
                needySelf.HandleStrike();
                StartCoroutine(PlayStrikeAnim());
            }
            hasActivated = false;
        };
        for (int x = 0; x < buttons.Count(); x++)
        {
            int pos = x;
            buttonSelectables[pos].OnInteract += delegate ()
            {
                if (!hasActivated) return false;
                buttonSelectables[pos].AddInteractionPunch(0.5f);
                sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                lightStates[pos] = !lightStates[pos];
                return false;
            };
            }
    }

    IEnumerator PlayCorrectAnim()// This flashes the ring if the inputs are correct.
    {
        for (int fme = 0; fme < 5; fme++)
        {
            for (int x = 0; x < buttonColors.Count(); x++)
            {
                buttonColors[x].material = x==4 ? buttonDeactivated : buttonStatus[1];
                lightKeys[x].enabled = x != 4;
            }
            yield return new WaitForSeconds(0.2f);
            for (int x = 0; x < buttonColors.Count(); x++)
            {
                buttonColors[x].material = buttonDeactivated;
                lightKeys[x].enabled = false;
            }
            yield return new WaitForSeconds(0.2f);
        }
        yield return null;
    }
    IEnumerator PlayStrikeAnim()// This flashes the X if at least 1 input is wrong.
    {
        for (int fme = 0; fme < 5; fme++)
        {
            for (int x = 0; x < goalLeds.Count(); x++)
            {
                buttonColors[x].material = x % 2 == 0 ? buttonStatus[1] : buttonDeactivated;
                lightKeys[x].enabled = x % 2 == 0;
            }
            yield return new WaitForSeconds(0.2f);
            for (int x = 0; x < goalLeds.Count(); x++)
            {
                buttonColors[x].material = buttonDeactivated;
                lightKeys[x].enabled = false;
            }
            yield return new WaitForSeconds(0.2f);
        }
        yield return null;
    }
    // Update is called once per frame

    IEnumerator FlashGoalOnWarning()
    {
        while (needySelf.GetNeedyTimeRemaining() > 0)
        {
            for (int x = 0; x < goalLeds.Count(); x++)
            {
                goalLeds[x].material = buttonStatus[0];
            }
            yield return new WaitForSeconds(0.25f);
            for (int pos = 0; pos < goalLights.Count(); pos++)
                {
                    goalLeds[pos].material = (mustInvert ^ goalLights[pos]) ? buttonStatus[0] : buttonStatus[1];
                }
            yield return new WaitForSeconds(0.25f);
        }
        yield return null;
    }
    void Update()
    {
        if (hasActivated)
        {
            for (int x = 0; x < buttonColors.Count(); x++)
            {
                buttonColors[x].material = lightStates[x] ? buttonStatus[0] : buttonColors[x].material = buttonStatus[1];
                lightKeys[x].enabled = !lightStates[x];
            }
            for (int x = 0; x < goalLeds.Count() && !isWarning; x++)
            {
                goalLeds[x].material = (mustInvert ^ goalLights[x]) ? buttonStatus[0] : buttonStatus[1];
                lightGoal[x].enabled = mustInvert ^ goalLights[x];
            }
        }
        else
        {
            for (int x = 0; x < goalLeds.Count(); x++)
            {
                goalLeds[x].material = buttonStatus[0];
                lightGoal[x].enabled = false;
            }
        }
        var timeleft = needySelf.GetNeedyTimeRemaining();
        if (timeleft > 0 && timeleft < 5 && !isWarning)
        {
            isWarning = true;
            warningHandler = FlashGoalOnWarning();
            StartCoroutine(warningHandler);
        }
    }
    // TP Handling

    void TwitchHandleForcedSolve()
    {
        forceDisable = true;
        needySelf.ResetDelayMin = float.MaxValue;
        needySelf.ResetDelayMax = float.MaxValue;
        //needySelf.SetResetDelayTime(float.MaxValue, float.MaxValue);
        needySelf.HandlePass();
        hasActivated = false;
    }
    public readonly string TwitchHelpMessage = "Press a button with “!{0} tl” or “!{0} 1”. Buttons are tl, tm, tr, ml, mm, mr, bl, bm, br, or numbered 1–9 in reading order. Commands can be chained but must be spaced out. \"press\" is optional.";
    KMSelectable[] ProcessTwitchCommand(string input)
    {
        string locinput = input.RegexMatch(@"^press\s") ? input.Substring(6).ToLower() : input.ToLower(); // Filter out "press " if necessary.
        string[] locInputs = locinput.Split(' ');
        List<KMSelectable> presses = new List<KMSelectable>();
        foreach (string singleIn in locInputs)
        { 
            switch (singleIn.Replace("center","middle").Replace("centre","middle"))
            {
                // Top Left Conditions
                case "tl":
                case "lt":
                case "topleft":
                case "lefttop":
                case "left-top":
                case "top-left":
                case "1":
                    presses.Add(buttonSelectables[0]);
                    break;
                // Top Middle Conditions
                case "tm":
                case "tc":
                case "mt":
                case "ct":
                case "topmiddle":
                case "middletop":
                case "top-middle":
                case "middle-top":
                case "2":
                    presses.Add(buttonSelectables[3]);
                    break;
                // Top Right Conditions
                case "tr":
                case "rt":
                case "topright":
                case "righttop":
                case "top-right":
                case "right-top":
                case "3":
                    presses.Add(buttonSelectables[6]);
                    break;
                // Middle Left Conditions
                case "ml":
                case "cl":
                case "lm":
                case "lc":
                case "middleleft":
                case "leftmiddle":
                case "left-middle":
                case "middle-left":
                case "4":
                    presses.Add(buttonSelectables[1]);
                    break;
                // Middle Middle Conditions (AKA Center)
                case "mm":
                case "cm":
                case "mc":
                case "cc":
                case "middle":
                case "middlemiddle":
                case "middle-middle":
                case "5":
                    presses.Add(buttonSelectables[4]);
                    break;
                // Middle Right Conditions
                case "mr":
                case "cr":
                case "rc":
                case "rm":
                case "middleright":
                case "rightmiddle":
                case "middle-right":
                case "right-middle":
                case "6":
                    presses.Add(buttonSelectables[7]);
                    break;
                // Bottom Left Conditions
                case "bl":
                case "lb":
                case "bottomleft":
                case "leftbottom":
                case "bottom-left":
                case "left-bottom":
                case "7":
                    presses.Add(buttonSelectables[2]);
                    break;
                // Bottom Middle Conditions
                case "bm":
                case "cb":
                case "mb":
                case "bc":
                case "bottommiddle":
                case "middlebottom":
                case "bottom-middle":
                case "middle-bottom":
                case "8":
                // Bottom Right Conditions
                    presses.Add(buttonSelectables[5]);
                    break;
                case "br":
                case "rb":
                case "bottomright":
                case "rightbottom":
                case "right-bottom":
                case "bottom-right":
                case "9":
                    presses.Add(buttonSelectables[8]);
                    break;
                // If the command is not specifed:
                default: return null;
            }
        }
        return presses.Count > 0 ? presses.ToArray() : null;
    }

}
