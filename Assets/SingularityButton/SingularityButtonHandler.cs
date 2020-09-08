﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class SingularityButtonHandler : MonoBehaviour {

	public GameObject disarmButtonObject, buttonFrontObject, entireModule, animatedPortion;
	public MeshRenderer buttonMainRenderer, buttonBacking, disarmBacking;
	public TextMesh textDisplay, textColorblind, textDisarm;
	public KMSelectable disarmButton, buttonFront;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;
	public KMColorblindMode colorblindMode;
	public KMAudio audioSelf;
	public Light lightStrike;


	private bool isSolved, hasDisarmed, hasActivated, colorblindDetected;
	private bool isPressedDisarm, isPressedMain;



	private static int modID = 1;
	private int curmodID;
	private static readonly Dictionary<KMBomb, SingularityButtonInfo> groupedSingularityButtons = new Dictionary<KMBomb, SingularityButtonInfo>();
	private SingularityButtonInfo singularityButtonInfo;
	IEnumerator flashingAnim;
	// Commented out because of some solve dependent modules not being easily detectable.
	/*
	private bool alwaysFlipToBack = false;
	public List<string> cautionaryModules = new List<string>();
	public readonly List<string> otherSolveDependModNames = new List<string>() {
		"Blind Maze",
		"Burglar Alarm",
		"Colour Code",
		"Combination Lock",
		"Langton's Ant",
		"The Number",
		"Planets",
		"The Plunger Button",
		"Press X",
		"Scripting",
		"Shapes and Bombs"
	};// Nonboss modules that are 100% dependent on solves. These CANNOT have an override for the solve condition or show up by chance. The Number will remove itself after 8 or more solves.

	void AddOthersModulesOntoList()
	{
		cautionaryModules.AddRange(bossModule.GetIgnoredModules("Singularity Button", new string[]
		{
			"14",
			"Brainf---",
			"Cookie Jars",
			"Divided Squares",
			"Encryption Bingo",
			"Forget Enigma",
			"Forget Everything",
			"Forget It Not",
			"Forget Infinity",
			"Forget Me Not",
			"Forget Perspective",
			"Forget Them All",
			"Forget This",
			"Forget Us Not",
			"Hogwarts",
			"Organization",
			"Simon Forgets",
			"Simon's Stages",
			"Tallordered Keys",
			"The Troll",
			"Übermodule",
			"Ultimate Custom Night"
		}));// Add boss modules onto this list that rely on checking stages or do not handle syncronized solves intentionally. Uses KMBossModule to grab latest list of boss modules
		cautionaryModules.AddRange(otherSolveDependModNames); // Add other solve dependent modules on the bomb.
		// BEGIN CHANCE SOLVES
		if (!(bombInfo.IsIndicatorPresent(Indicator.BOB) &&
			bombInfo.GetBatteryCount() == 5 &&
			bombInfo.GetBatteryHolderCount() == 3))// "If there are five batteries in three holders and at least one BOB indicator..."
		{
			cautionaryModules.Add("Big Circle");
			Debug.LogFormat("[Singularity Button #{0}]: Big Circle has no override active.", curmodID);
		}// Add Big Circle
		if (!"AEIOU".Contains(bombInfo.GetSerialNumber()) &&
			bombInfo.GetBatteryCount() <= 3 &&
			!bombInfo.IsPortPresent(Port.Serial))// Cases 1, 2, and 3 are NOT active.
		{
			cautionaryModules.Add("Modern Cipher");
			Debug.LogFormat("[Singularity Button #{0}]: Modern Cipher has no override active.", curmodID);
		}// Add Modern Cipher
		if (!(bombInfo.IsIndicatorOn(Indicator.BOB) &&
			bombInfo.GetBatteryCount() == 4 &&
			bombInfo.GetBatteryHolderCount() == 2))// "If there are exactly 4 batteries in 2 holders and a there is a lit BOB indicator..."
		{
			cautionaryModules.Add("Laundry");
			Debug.LogFormat("[Singularity Button #{0}]: Laundry has no override active.", curmodID);
		}// Add Laundry
		if (!(bombInfo.GetBatteryCount() == 2 &&
			bombInfo.GetBatteryHolderCount() == 1 &&
			bombInfo.IsIndicatorOn(Indicator.FRK) &&
			bombInfo.GetPortCount(Port.Parallel) == 0 &&
			bombInfo.GetPortCount(Port.Serial) == 0))//"If the bomb has exactly two batteries in one holder, a lit FRK indicator and no Serial or Parallel ports..."
		{
			cautionaryModules.Add("Heraldry");
			Debug.LogFormat("[Singularity Button #{0}]: Heraldry has no override active.", curmodID);
		}// Add Heraldry
		if (!(bombInfo.IsIndicatorPresent(Indicator.BOB) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "Double".ContainsIgnoreCase(a)).ToList().Count >= 3) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "Burglar Alarm".ContainsIgnoreCase(a)).ToList().Count >= 1 && bombInfo.GetSolvableModuleNames().Where(a => "Safety Safe".ContainsIgnoreCase(a)).ToList().Count >= 1) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "The Jewel Vault".ContainsIgnoreCase(a)).ToList().Count >= 1 && bombInfo.GetSolvableModuleNames().Where(a => "Safety Safe".ContainsIgnoreCase(a)).ToList().Count >= 1) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "Burglar Alarm".ContainsIgnoreCase(a)).ToList().Count >= 1 && bombInfo.GetSolvableModuleNames().Where(a => "The Jewel Vault".ContainsIgnoreCase(a)).ToList().Count >= 1)
			))// "If there is a lit or unlit BOB indicator..."
		{// "If you have two or more (not including repeats) out of Burglar Alarm, Safety Safe and The Jewel Vault, or if the Bomb contains three or more modules with the word 'Double' in their names..."
			cautionaryModules.Add("Free Parking");
			Debug.LogFormat("[Singularity Button #{0}]: Free Parking has no override active, ensure you check Free Parking's manual for other cases!", curmodID);
		}// Add Free Parking, note that it doesn't detect if the value falls below 0 after base/current modification.

		int litIndcnt = 0;
		int offIndcnt = 0;
		foreach (string litind in bombInfo.GetOnIndicators())
		{
			litIndcnt++;
		}
		foreach (string litind in bombInfo.GetOffIndicators())
		{
			offIndcnt++;
		}
		int lettersInRelation = 0;
		foreach (char letter in bombInfo.GetSerialNumber().ToCharArray())
		{
			if ("UNRELATED".Contains(letter))
			{
				lettersInRelation++;
			}
		}
		if (litIndcnt < 3 && offIndcnt < 3 && !(bombInfo.IsIndicatorOff(Indicator.BOB) && lettersInRelation >= 2))
		{
			cautionaryModules.Add("Unrelated Anagrams");
			Debug.LogFormat("[Singularity Button #{0}]: Unrelated Anagrams has no override active.",curmodID);
		}// Add Unrelated Anagrams, will remove itself after 9 or more solves have passed.

		// List of other chance solve dependent modules not shown in code:
		// Instructions, "Solved Modules" can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// Cruel Piano Keys, symbols for the solve dependent condition can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// Cruel Game of Life, has 2 overrides but green can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// The Hexabutton, no consistent way to detect if the given module is solve dependent or not.
		// Jack-O'-Lantern, no consistent way to detect if the given module is solve dependent or not.
		// Morse-A-Maze, no consistent way to detect if the given module is solve dependent or not.
		// Seven Wires, chance to get specific instances NOT the 6, 12, 18, 24,... one, no consistent way to detect if the given module is solve dependent or not.
		// Boolean Wires, has a chance where 2 solve dependent conditions can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// Dr Doctor, For the override, (3B 3H, LIT FRK, UNLIT TRN, Forget Me Not, LIT FRQ) and then NO Fever Symptom, has a chance to NOT show up. No consistent way to detect if the given module is solve dependent or not.
		// Double Expert, a couple rules rely on solves but no consistent way to detect if the given module is solve dependent or not.
		// Black Hole, you can solve this without advantagous solves.
		// The Stare, you can solve this without needing this to be at a multiple of 5 solves.
		// Curriculum, you can solve this without bookworm. Bookworm makes Curriculum easier but is not fully needed.
		// Challenge and Contact, after the first letter, the module can generate the 2nd letter based on solve parity. You can solve this with the remaining 2 letters having similar parities.
		// END CHANCE SOLVES
	}

	void UpdateCautionaryList()
	{
		if (cautionaryModules.Contains("The Number") && bombInfo.GetSolvedModuleNames().Count >= 8)
		{
			cautionaryModules.Remove("The Number");
			Debug.LogFormat("[Singularity Button #{0}]: The bomb has exceeded a certain number of solves. The Number is no longer detected!", curmodID);
		}// Remove The Number after 8 or more solves.
		if (cautionaryModules.Contains("Unrelated Anagrams") && bombInfo.GetSolvedModuleNames().Count >= 9)
		{
			cautionaryModules.Remove("Unrelated Anagrams");
			Debug.LogFormat("[Singularity Button #{0}]: The bomb has exceeded a certain number of solves. Unrelated Anagrams is no longer detected!", curmodID);
		}// Remove Unrelated Anagrams after 9 or more solves.
		int cheapCheckoutCount = bombInfo.GetSolvableModuleNames().Where(a => a.Equals("Cheap Checkout")).Count();
		int slotsCount = bombInfo.GetSolvableModuleNames().Where(a => a.Equals("Silly Slots")).Count();
		int jewelVaultCount = bombInfo.GetSolvableModuleNames().Where(a => a.Equals("The Jewel Vault")).Count();
		if (cautionaryModules.Contains("Free Parking") &&
			cheapCheckoutCount > 0 && bombInfo.GetSolvedModuleNames().Where(a => a.Equals("Cheap Checkout")).Count() >= cheapCheckoutCount &&
			slotsCount > 0 && bombInfo.GetSolvedModuleNames().Where(a => a.Equals("Silly Slots")).Count() >= slotsCount &&
			jewelVaultCount > 0 && bombInfo.GetSolvedModuleNames().Where(a => a.Equals("The Jewel Vault")).Count() >= jewelVaultCount)
		{
			cautionaryModules.Remove("Free Parking");
			Debug.LogFormat("[Singularity Button #{0}]: Free Parking has an override active! This module is no longer detected!", curmodID);
		}// Remove Free Parking if all Cheap Checkout's, Silly Slots', and Jewel Vault's are solved.
	}*/
	void Awake()
	{
		curmodID = modID++;
		groupedSingularityButtons.Clear();
		colorblindDetected = colorblindMode.ColorblindModeActive;
	}
	// Use this for initialization
	bool onHoldState;
	void Start() {
		disarmButton.OnInteract += delegate {
			audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			isPressedDisarm = true;
			return false;
		};
		disarmButton.OnInteractEnded += delegate
		{
			disarmButton.AddInteractionPunch(1f);
			audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
			isPressedDisarm = false;
			if (isSolved)
			{
				if (!hasDisarmed)
				{
					Debug.LogFormat("[Singularity Button #{0}]: Module disarmed.", curmodID);
					string[] possibleDisarmMessages = { "MODULE\nDISARMED", "CHECK\nYOUR\nMODULES" };
					textDisarm.text = possibleDisarmMessages.PickRandom();
					disarmBacking.material.color = Color.green * 0.5f;
				}
				modSelf.HandlePass();
				hasDisarmed = true;

			}
		};
		buttonFront.OnInteract += delegate
		{
			audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
			if (!isSolved && hasActivated && !singularityButtonInfo.canDisarm)
			{
				singularityButtonInfo.HandleInteraction(this, (int)bombInfo.GetTime());
			}
			isPressedMain = true;
			onHoldState = hasActivated;
			return false;
		};
		buttonFront.OnInteractEnded += delegate
		{
			audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
			buttonFront.AddInteractionPunch();
			if (!isSolved && hasActivated && onHoldState && !singularityButtonInfo.canDisarm)
			{
				singularityButtonInfo.HandleInteraction(this, (int)bombInfo.GetTime());
				singularityButtonInfo.HandleButtonRelease();
			}
			isPressedMain = false;
		};
		modSelf.OnActivate += delegate
		{
			textDisarm.text = "";
			// Setup Global Interaction
			KMBomb bombAlone = entireModule.GetComponentInParent<KMBomb>(); // Get the bomb that the module is attached on. Required for intergration due to modified value.
																			//Required for Multiple Bombs stable interaction in case of different bomb seeds.
			if (!groupedSingularityButtons.ContainsKey(bombAlone))
				groupedSingularityButtons[bombAlone] = new SingularityButtonInfo();
			singularityButtonInfo = groupedSingularityButtons[bombAlone];
			singularityButtonInfo.singularButtons.Add(this);
			// Start Main Handling
			//AddOthersModulesOntoList();
			StartCoroutine(HandleGlobalModule());
		};
		bombInfo.OnBombExploded += delegate
		{
			singularityButtonInfo.StopAll();
			StopAllCoroutines();
		};
		lightStrike.range *= modSelf.transform.lossyScale.x;
		textDisarm.text = "";
		hasActivated = true;
	}
	void HandleStrikeSelf()
    {
		modSelf.HandleStrike();
		if (flashingAnim != null)
			StopCoroutine(flashingAnim);
		flashingAnim = FlashLight();
		StartCoroutine(flashingAnim);
	}

	IEnumerator FlashLight()
	{
		lightStrike.intensity = 100;
		yield return new WaitForSeconds(0.5f);
		for (int x = 90; x >= 0; x = x * 4 / 5)
		{
			lightStrike.intensity = x;
			if (x <= 0) break;
			yield return new WaitForSeconds(0.1f);
		}
		yield return null;
	}
	IEnumerator HandleGlobalModule()
	{
		StartCoroutine(singularityButtonInfo.StartBootUpSequence());
		int lastSolveCount = 0;
		while (!singularityButtonInfo.canDisarm)
		{
			var curSolveCount = bombInfo.GetSolvedModuleNames().Count;
			if (lastSolveCount != curSolveCount)
			{
				lastSolveCount = curSolveCount;
				//UpdateCautionaryList();
			}
			yield return new WaitForEndOfFrame();
		}
		//yield return new WaitForSeconds(Random.Range(0f, 4f));
		isSolved = true;
		
		Debug.LogFormat("[Singularity Button #{0}]: A correct set of actions caused the Singularity Buttons to enter a solve state.", curmodID);

		// Commented out because of some solve dependent modules not being easily detectable.
		/*
		if (!alwaysFlipToBack && (!bombInfo.GetSolvableModuleNames().Any(a => cautionaryModules.Contains(a)) || singularityButtonInfo.CountSingularityButtons() == 1))
		{// Does the bomb contain any cautionary modules or is there 1 Singularity Button present on this bomb and does it not need to flip to the back?
			hasDisarmed = true;
			modSelf.HandlePass();
		}
		else if (alwaysFlipToBack)
			Debug.LogFormat("[Singularity Button #{0}]: A setting is enforcing the module to always flip to the back. You must instead press the manual disarm button to disarm this module!", curmodID);
		else
			Debug.LogFormat("[Singularity Button #{0}]: At least one cautionary module is present on the bomb. You must instead press the manual disarm button to disarm this module!", curmodID);
		*/
		audioSelf.PlaySoundAtTransform("paul368_sfx-door-open", transform);
		if (bombInfo.GetSolvableModuleIDs().Count(a => a == modSelf.ModuleType) == bombInfo.GetSolvableModuleIDs().Count() && !TwitchPlaysActive)
        {
			Debug.LogFormat("[Singularity Button #{0}]: Module disarmed. Only Singulaity Buttons are present.", curmodID);
			string[] possibleDisarmMessages = { "BOMB\nDISARMED", "BOMB\nDEFUSED" };
			textDisarm.text = possibleDisarmMessages.PickRandom();
			disarmBacking.material.color = Color.green * 0.5f;
			hasDisarmed = true;
			modSelf.HandlePass();
		}
		else
        {
			textDisarm.text = "DISARM\nMODULE\nMANUALLY";
		}
		yield return null;
	}
	// Update is called once per frame
	int frameMain = 45, frameDisarm = 45, frameSwitch = 30, animLength = 30;
	void Update () {
		if (!hasActivated) return;
		if (!isPressedMain)
		{
			frameMain = Mathf.Min(frameMain + 1, 45);
		}
		else
			frameMain = Mathf.Max(frameMain - 1, 30);
		if (!isPressedDisarm)
		{
			frameDisarm = Mathf.Min(frameDisarm + 1, 45);
		}
		else
			frameDisarm = Mathf.Max(frameDisarm - 1, 40);
		if (isSolved)
		{
			frameSwitch = Mathf.Min(frameSwitch + 1, animLength);
		}
		else
		{
			frameSwitch = Mathf.Max(frameSwitch - 1, 0);
		}
		disarmButtonObject.SetActive(frameSwitch > 0);
		buttonFrontObject.SetActive(frameSwitch < animLength);
		buttonFrontObject.transform.localPosition = new Vector3(0, 0.03f * (frameMain / 45f), 0);
		disarmButtonObject.transform.localPosition = new Vector3(0, -0.019f * (frameDisarm / 45f), 0);
		animatedPortion.transform.localEulerAngles = new Vector3(0, 0, 180f * (frameSwitch / (float)animLength));
		textColorblind.gameObject.SetActive(colorblindDetected);
		if (colorblindDetected)
		{
			textColorblind.text = singularityButtonInfo.GrabColorofButton(this).ToUpper();
		}
	}
	// Handle Twitch Plays
	IEnumerator TwitchHandleForcedSolve()
	{
		Debug.LogFormat("[Singularity Button #{0}]: A force solve has been issued viva TP Handler. ALL Singularity Buttons will be set to a solve state because of it.", curmodID);
		if (!isSolved)
			singularityButtonInfo.DisarmAll(); // Call the protected method, if the module is not solved yet.
		while (frameSwitch < animLength)
		{
			if (hasDisarmed) yield return true;
			yield return true;
		}
		disarmButton.OnInteract();
		yield return new WaitForSeconds(0.2f);
		disarmButton.OnInteractEnded();
		textDisarm.text = "FORCE\nSOLVED";
		yield return true;
	}
	#pragma warning disable 0414
		string TwitchHelpMessage = "To press the disarm button: \"!{0} disarm\", To grab the info of the button: \"!{0} state/color\"\n" +
		"To interact the main button based on seconds digits: \"!{0} tap/hold/release ##\"; based on the digit being present: \"!{0} tap/hold/release #\"; anytime: \"!{0} tap/hold\"\n" +
		"Multiple time stamps based on seconds digits can be used. I.E \"!{0} tap 55 44 33\"...\n" +
		"To interact with the button based on even/odd conditions \"!{0} hold/tap/release even/odd\"";
		bool TwitchPlaysActive;
	#pragma warning restore 0414
	IEnumerator ProcessTwitchCommand(string command)
	{
		string interpetedCommand = command.ToLower();
		string[] separatedCommands = command.Split(';');
		string pressDisarm = @"^disarm$",
			tapTimeStamp = @"^tap( \d{2})+$", tapDigit = @"^tap( \d)?$",
			holdTimeStamp = @"^hold( \d{2})+$", holdDigit = @"^hold( \d)?$",
			releaseTimeStamp = @"^release( \d{2})+$", releaseDigit = @"^release \d$",
			tapTimeParity = @"^tap (at |on )?(even|odd)$", releaseTimeParity = @"^release (at |on )?(even|odd)$", holdTimeParity = @"^hold (at |on )?(even|odd)$",
			grabState = @"^state$", grabColor = @"^color$", enableColorblind = @"^colou?rblind$";

		string[] commandParts = interpetedCommand.Split(' ');

		if (hasDisarmed)
		{
			yield return "sendtochaterror Are you trying to interact the button when its already solved? You might want to think again. (This is an anarchy command prevention message.)";
			yield break;
		}
		else
		if (interpetedCommand.RegexMatch(pressDisarm))
		{
			if (!isSolved)
			{
				yield return "sendtochaterror The module is not solved yet! Figure out to set ALL Singularity Buttons to a solve state first.";
				yield break;
			}
			while (frameSwitch < animLength)
				yield return "trycancel";
			yield return null;
			yield return disarmButton;
			yield return "solve";
			yield return new WaitForSeconds(0.2f);
			yield return disarmButton;
			
		}
		else if (isSolved)
		{
			yield return "sendtochaterror The module is already put in a solve state! Check the help command for this module to figure out how to press the manual disarm button.";
			yield break;
		}
		else if (interpetedCommand.RegexMatch(grabState))
		{
			yield return "sendtochat This button is currently " + (isPressedMain ? "held" : "not held");
			yield break;
		}
		else if (interpetedCommand.RegexMatch(enableColorblind))
		{
			yield return null;
			colorblindDetected = !colorblindDetected;
			yield break;
		}
		else if (interpetedCommand.RegexMatch(grabColor))
		{
			yield return "sendtochat The color of this button is " + singularityButtonInfo.GrabColorofButton(this);
			yield break;
		}
		else if (singularityButtonInfo.IsAnyButtonHeld())
		{
			if (isPressedMain)
			{ 
				if (interpetedCommand.RegexMatch(releaseTimeStamp))
				{
					int idx = commandParts.Length - 1;
					List<int> possibleSecondsTimer = new List<int>();
					while (idx >= 0 && commandParts[idx].RegexMatch(@"^\d\d$"))
					{
						possibleSecondsTimer.Add(int.Parse(commandParts[idx]));
						idx--;
					}
					do
						yield return "trycancel";
					while (!possibleSecondsTimer.Contains((int)bombInfo.GetTime() % 60));
					yield return null;
					yield return buttonFront;
					yield return "strike";
					yield return "solve";

				}
				else if (interpetedCommand.RegexMatch(releaseTimeParity))
				{
					bool requireEven = interpetedCommand[interpetedCommand.Length - 1].Equals("even");
					do
						yield return "trycancel";
					while ((int)bombInfo.GetTime() % 2 == 0 == requireEven);
					yield return null;
					yield return buttonFront;
					yield return "strike";
					yield return "solve";
				}
				else if (interpetedCommand.RegexMatch(releaseDigit))
				{
					string digitGoal = commandParts[1];
					do
						yield return "trycancel";
					while (!((int)bombInfo.GetTime() % 60).ToString().Contains(digitGoal));
					yield return null;
					yield return buttonFront;
					yield return "strike";
				}
			}
			else
				yield return "sendtochaterror Another Singularity Button is currently held! Find the Singularity Button being held and release that first!";
		}
		else if (interpetedCommand.RegexMatch(tapTimeStamp))
		{
			int idx = commandParts.Length - 1;
			List<int> possibleSecondsTimer = new List<int>();
			while (idx >= 0 && commandParts[idx].RegexMatch(@"^\d\d$"))
			{
				possibleSecondsTimer.Add(int.Parse(commandParts[idx]));
				idx--;
			}
			do
				yield return "trycancel";
			while (!possibleSecondsTimer.Contains((int)bombInfo.GetTime() % 60));
				
			yield return null;
			yield return buttonFront;
			yield return buttonFront;
			yield return "strike";
		}
		else if (interpetedCommand.RegexMatch(tapTimeParity))
		{
			bool requireEven = interpetedCommand[interpetedCommand.Length - 1].Equals("even");
			do
				yield return "trycancel";
			while ((int)bombInfo.GetTime() % 2 == 0 == requireEven);
			yield return null;
			yield return buttonFront;
			yield return buttonFront;
			yield return "strike";
			yield return "solve";
		}
		else if (interpetedCommand.RegexMatch(tapDigit))
		{
			if (commandParts.Length == 2)
			{
				string digitGoal = commandParts[1];
				do {
					yield return "trycancel";
				} while (!((int)bombInfo.GetTime() % 60).ToString().Contains(digitGoal));
			}
			yield return null;
			yield return buttonFront;
			yield return buttonFront;
			yield return "strike";
			yield return "solve";
		}
		else if (interpetedCommand.RegexMatch(holdTimeStamp))
		{
			int idx = commandParts.Length - 1;
			List<int> possibleSecondsTimer = new List<int>();
			while (idx >= 0 && commandParts[idx].RegexMatch(@"^\d\d$"))
			{
				possibleSecondsTimer.Add(int.Parse(commandParts[idx]));
				idx--;
			}
			do
				yield return "trycancel";
			while (!possibleSecondsTimer.Contains((int)bombInfo.GetTime() % 60));
			yield return null;
			yield return buttonFront;
		}
		else if (interpetedCommand.RegexMatch(holdTimeParity))
		{
			bool requireEven = interpetedCommand[interpetedCommand.Length - 1].Equals("even");
			do
				yield return "trycancel";
			while ((int)bombInfo.GetTime() % 2 == 0 == requireEven);
			yield return null;
			yield return buttonFront;
		}
		else if (interpetedCommand.RegexMatch(holdDigit))
		{
			if (commandParts.Length == 2)
			{
				string digitGoal = commandParts[1];
				do {
					yield return "trycancel";
				} while (!((int)bombInfo.GetTime() % 60).ToString().Contains(digitGoal));
			}
			yield return null;
			yield return buttonFront;
		}
		yield break;
	}
}
