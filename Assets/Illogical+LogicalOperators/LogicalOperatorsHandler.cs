﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;

public class LogicalOperatorsHandler : MonoBehaviour {

	public HandleButtonInvert[] buttonInverts;
	public KMAudio audioSelf;
	public KMSelectable submitStatus;
	public GameObject statusLight;
	public KMBombModule modSelf;
	public KMRuleSeedable ruleSeedCore;
	public TextMesh[] displayChips;

	bool isAnimating = false;

	private const string hexDigits = "0123456789ABCDEF", possibleCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	int[] selectedDigits;
	bool[,] trueStates;
	float trivialThreshold = 0.5f;
	private static int modCounter = 1;
	int modID;
	int[] powersOf2 = new int[] { 1, 2, 4, 8, 16, 32, 64, 128 };
	bool[] forceSolveStates;
	void HandleRuleSeedSupport()
    {
		trueStates = new bool[16, 4];
		if (ruleSeedCore == null)
        {
			Debug.LogFormat("<Logical Operators #{0}>: Rule seed handler for Logical Operators do not exist, generating default settings...", modID);
			for (int x = 0; x < trueStates.GetLength(0); x++)
            {
                /* Reference thought, each hexdecimal has binary digits which can be used for logic operations
				* 9: corresponds to 1001
				* 8: corresponds to 1000
				* 7: corresponds to 0111
				* etc.
				* These values are formatted as the following for the inputs: 11, 10, 01, 00
				* 1 refers to the top bit on the module, 0 refers to the bottom bit on the module.
				*/
                
                for (int y = 0; y < trueStates.GetLength(1); y++)
                {
					trueStates[x, trueStates.GetLength(1) - 1 - y] = x / powersOf2[y] % 2 == 1;
                }
				Debug.LogFormat("<Logical Operators #{0}>: Hexdigit '{1}' with states: {2}", modID,
					hexDigits[x],
					new bool[] { trueStates[x, 0], trueStates[x, 1], trueStates[x, 2], trueStates[x, 3] }.Select(a => a ? "T" : "F").Join(""));
			}
			return;
        }
    }

	// Use this for initialization
	void Start () {
		modID = modCounter++;
		HandleRuleSeedSupport();
		// Generate 7 unique gates that creates a solution has < the specified % of states.
		List<int> possibleSolutions = new List<int>();
		int attemptsLeft = 10;
		do
		{
			possibleSolutions.Clear();
			// Generate the Logic Gates;
			selectedDigits = new int[] { -1,-1,-1,-1,-1,-1,-1 };
            for (int x = 0; x < 7; x++)
            {
				int selectedValue = uernd.Range(0, 16) % 16;
				while (selectedDigits.Contains(selectedValue))
                {
					selectedValue = uernd.Range(0, 16) % 16;
				}
				selectedDigits[x] = selectedValue;
            }
            // Check if the solution is possible.
            for (int idxSol = 0; idxSol < 256; idxSol++)
            {
				if (isCorrect(powersOf2.Select(a => idxSol / a % 2 == 1).ToArray()))
                {
					possibleSolutions.Add(idxSol);
                }
            }
			attemptsLeft--;
		}
		while (possibleSolutions.Count >= trivialThreshold * 256 || !possibleSolutions.Any());
        // Make sure there is at least 1 possible solution on the module and the number of solutions generated is less than the threshold and attempts can still be made.
        for (int x = 0; x < displayChips.Length; x++)
        {
			displayChips[x].text = hexDigits[selectedDigits[x]].ToString().ToUpper();
        }
		Debug.LogFormat("[Logical Operators #{0}]: From top to bottom, left to right, the chips read the following digits: {1}", modID, selectedDigits.Select(a => hexDigits[a]).Join(", "));
		int randomSolution = possibleSolutions.PickRandom();
		forceSolveStates = powersOf2.Select(a => randomSolution / a % 2 == 1).ToArray();

		Debug.LogFormat("[Logical Operators #{0}]: From top to bottom, one possible solution for this may be: {1}", modID, forceSolveStates.Select(a => a ? '1' : '0').Join(""));
		// Handle Inverting States
		submitStatus.OnInteract += delegate {
			if (!isAnimating)
			{
				isAnimating = true;
				StartCoroutine(HandleSubmit());
			}
			return false;
		};
        for (int x = 0; x < buttonInverts.Length; x++)
        {
			int y = x;
			buttonInverts[x].toggled = uernd.value < 0.5f;
			buttonInverts[x].selfSelectable.OnInteract = delegate
			{
				audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, buttonInverts[y].transform);
				buttonInverts[y].selfSelectable.AddInteractionPunch(0.1f);
				if (!isAnimating)
				{
					buttonInverts[y].ToggleState();
				}
				return false;
			};
        }
		if (!Application.isEditor)
			StartCoroutine(MakeStatusLightDisappear());
	}

	bool isCorrect()
    {
		List<bool> states = buttonInverts.Select(a => a.toggled).ToList();
		int curIdx = 0;
		while (states.Count > 1)
        {
			for (int x = 0; x < states.Count; x++)
            {
				if (x + 1 < states.Count)
				{
                    states[x] = trueStates[selectedDigits[curIdx], (states[x] ? 0 : 2) + (states[x + 1] ? 0 : 1)];
					states.RemoveAt(x + 1);
					curIdx++;
				}
            }
			Debug.LogFormat("<Logical Operators #{0}>: DEBUG BOOLEAN STATES: {1}", modID, states.Select(a => a ? "1" : "0").Join(""));
		}

		return states.ElementAtOrDefault(0);
    }
	bool isCorrect(bool[] finalStates)
	{
		List<bool> states = finalStates.ToList();
		int curIdx = 0;
		while (states.Count > 1)
		{
			for (int x = 0; x < states.Count; x++)
			{
				if (x + 1 < states.Count)
				{
					states[x] = trueStates[selectedDigits[curIdx], (states[x] ? 0 : 2) + (states[x + 1] ? 0 : 1)];
					states.RemoveAt(x + 1);
					curIdx++;
				}
			}
		}

		return states.ElementAtOrDefault(0);
	}
	Vector3 endPos = new Vector3(0.07f, 0.025f, 0.0f), startPos = new Vector3(0.07f, 0.05f, 0.0f);
	IEnumerator MakeStatusLightDisappear()
    {
		for (float x = 0; x <= 1; x = Mathf.Min(x + 4 * Time.deltaTime, 1))
		{
			statusLight.transform.localPosition = startPos * (x) + endPos * (1 - x);
			if (x == 1) break;
			yield return new WaitForSeconds(Time.deltaTime);
		}
		statusLight.SetActive(false);
	}

	IEnumerator HandleSubmit()
    {
		statusLight.SetActive(true);
		for (float x = 0; x <= 1; x = Mathf.Min(x + 4 * Time.deltaTime, 1))
		{
			statusLight.transform.localPosition = startPos * (1 - x) + endPos * (x);
			if (x == 1) break;
			yield return new WaitForSeconds(Time.deltaTime);
		}
		Debug.LogFormat("[Logical Operators #{0}]: From top to bottom, you submitted: {1}", modID, buttonInverts.Select(a => a.toggled ? "1" : "0").Join(""));
		if (isCorrect())
        {
			Debug.LogFormat("[Logical Operators #{0}]: Which results in the status light turning green. Module disarmed.", modID);
			modSelf.HandlePass();
			yield break;
        }
		Debug.LogFormat("[Logical Operators #{0}]: Which results in the status light turning red. Strike!", modID);
		modSelf.HandleStrike();
		yield return new WaitForSeconds(1.5f);
		yield return MakeStatusLightDisappear();
		isAnimating = false;
		yield return null;
    }

	// Update is called once per frame
	void Update () {

	}

	IEnumerator TwitchHandleForcedSolve()
    {
		while (!forceSolveStates.SequenceEqual(buttonInverts.Select(a => a.toggled)))
        {
			for (int x = 0; x < forceSolveStates.Length; x++)
            {
				if (forceSolveStates[x] != buttonInverts[x].toggled)
					buttonInverts[x].selfSelectable.OnInteract();
				yield return null;
			}
		}

		submitStatus.OnInteract();
		yield return null;
    }
#pragma warning disable IDE0051 // Remove unused private members
    readonly string TwitchHelpMessage = "Toggle selected LEDs with \"!{0} toggle # # #\", where the LEDs are labeled 1-8 from top to bottom. Submit the current state with \"!{0} submit\"";
	#pragma warning restore IDE0051 // Remove unused private members

    IEnumerator ProcessTwitchCommand(string cmd)
    {
		string cmdLower = cmd.ToLower().Trim();

		if (cmdLower.RegexMatch(@"^submit$"))
        {
			yield return null;
			submitStatus.OnInteract();
			yield return "solve";
			yield return "strike";
		}
		else if (cmdLower.RegexMatch(@"^toggle(\s\d+)+$"))
        {
			string[] possibleValues = cmdLower.Substring(6).Trim().Split();
			List<KMSelectable> selectedPressables = new List<KMSelectable>();
			foreach (string aValue in possibleValues)
            {
				int possibleValue;
				if (!int.TryParse(aValue, out possibleValue) || possibleValue < 1 || possibleValue > 8)
                {
					yield return string.Format("sendtochaterror I do not know of a position \"{0}\" on the module.",aValue);
					yield break;
                }
				selectedPressables.Add(buttonInverts[possibleValue - 1].selfSelectable);

            }
			yield return null;
			yield return selectedPressables.ToArray();
        }
		yield break;
    }
}
