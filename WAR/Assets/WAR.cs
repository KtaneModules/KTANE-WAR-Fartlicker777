using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class WAR : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public KMSelectable Module;

   public AudioSource Music;
   public AudioClip[] MusicParts;

   public SpriteRenderer BombRenderer;
   public Sprite[] Bombs;
   public GameObject DeadBomb;
   public GameObject LightsParent;
   public GameObject LeftLight;
   public GameObject RightLight;

   public GameObject NumbersandColonsAndDoohickeysAndSuch;

   public CanvasGroup VignetteScreen;

   public static string[] ignoredModules = null;

   private float DefaultGameMusicVolume;

   int ModCount = 8008135;
   int Stage;
   bool WaitForModCount;

   int Minutes;
   int Seconds;

   public Sprite[] TimerDigits;
   public Sprite[] GreenTimerDigits;
   public SpriteRenderer[] TimerDigitsSecondsRen;
   public SpriteRenderer[] TimerDigitsMinutesRen;
   public SpriteRenderer Colon;
   public Sprite[] ColonColors;

   WARSettings Settings = new WARSettings();

   Coroutine Countdown;
   Coroutine AddTime;
   Coroutine Fade;
   Coroutine Spaztic;
   Coroutine Flicker;

   bool Adding;
   bool IsFading;
   bool ActiveTimer;
   bool Autosolved;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   bool StrikeReset;

   int TimeToAdd = 30;
   int CurrentTime = 0;
   int CurrentlyBeingAdded;

   bool PlaySFX = true;
   bool PlayThousandMarch = true;

   bool Selected;
   bool MusicPlaying;
   bool TenSecondWarning;

   bool ZenModeActive;

   Dictionary<string, int> ModToTime = new Dictionary<string, int>();

   class WARSettings {
      public bool Detonation = false;
   }

   static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
   {
      new Dictionary<string, object>
      {
         { "Filename", "WARSettings.json" },
         { "Name", "WAR Settings" },
         { "Listing", new List<Dictionary<string, object>>{
            new Dictionary<string, object>
            {
               { "Key", "Detonation" },
               { "Text", "The module will detonate the bomb upon running out of time." }
            }
         } }
      }
   };

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;

      if (!Application.isEditor) {
         ModConfig<WARSettings> modConfig = new ModConfig<WARSettings>("WARSettings");
         //Read from the settings file, or create one if one doesn't exist
         Settings = modConfig.Settings;
         //Update the settings file in case there was an error during read
         modConfig.Settings = Settings;
      }

      


      string missionDesc = KTMissionGetter.Mission.Description;
      if (missionDesc != null) {
         Regex regex = new Regex(@"WAR_DETONATE=(true|false)");
         var match = regex.Match(missionDesc);
         if (match.Success) {
            string[] options = match.Value.Replace("WAR_DETONATE=", "").Split(',');
            bool[] values = new bool[options.Length];
            for (int i = 0; i < options.Length; i++)
               values[i] = options[i] == "true" ? true : false;
            Settings.Detonation = values[0];
         }
      }


      /*
      foreach (KMSelectable object in keypad) {
          object.OnInteract += delegate () { keypadPress(object); return false; };
      }
      */

      //button.OnInteract += delegate () { buttonPress(); return false; };

      Module.OnFocus += delegate () { Selected = true; };
      Module.OnDefocus += delegate () { Selected = false; };

      try {
         DefaultGameMusicVolume = GameMusicControl.GameMusicVolume;
      }
      catch (Exception) { }

      if (Application.isEditor) {
         Selected = true;
      }

      if (ignoredModules == null) {
         ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("WAR", new string[] {
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "The Board Walk",
                "Busy Beaver",
                "Don't Touch Anything",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Ligma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForest",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "Reporting Anomalies",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Speech Jammer",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "The Twin",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "WAR",
                "Whiteout"
            });
      }

   }

   void OnDestroy () { //Shit you need to do when the bomb ends
      GameMusic(true);
   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on

   }

   IEnumerator PlayMusic () {
      /*Audio.PlaySoundAtTransform("ShotgunPickup", transform);
      yield return new WaitForSeconds(.8f);*/
      Music.volume = 1;
      if (Music.isPlaying) {
         Music.Stop();
      }
      MusicPlaying = true;
      GameMusic(false);
      Music.clip = MusicParts[0];
      Music.Play();
      while (Music.isPlaying) {
         yield return null;
      }
      if (!(StrikeReset || ModuleSolved)) {
         Music.clip = MusicParts[1];
         Music.loop = true;
         Music.Play();
      }
   }

   IEnumerator DETONATE () {
      if (Settings.Detonation) {
         if (ZenModeActive) {
            yield return null;
            Application.Quit();
         }
         while (true) {
            Strike();
            yield return new WaitForSeconds(.1f);
         }
      }
   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      WaitForModCount = true;
      ModToTime.Add("14", 90);
      ModToTime.Add("42", 30);
      ModToTime.Add("501", 30);
      ModToTime.Add("A>N<D", 20);
      ModToTime.Add("Black Arrows", 20);
      ModToTime.Add("Brainf---", 15);
      ModToTime.Add("The Board Walk", 30);
      ModToTime.Add("Busy Beaver", 90);
      ModToTime.Add("Floor Lights", 60);
      ModToTime.Add("Forget Enigma", 40);
      ModToTime.Add("Forget Everything", 25);
      ModToTime.Add("Forget Infinity", 15);
      ModToTime.Add("Forget It Not", 5);
      ModToTime.Add("Forget Maze Not", 50);
      ModToTime.Add("Forget Me Later", 25);
      ModToTime.Add("Forget Me Not", 7);
      ModToTime.Add("Forget Perspective", 120);
      ModToTime.Add("Forget The Colors", 60);
      ModToTime.Add("Forget This", 15);
      ModToTime.Add("Forget Us Not", 18);
      ModToTime.Add("Iconic", 3);
      ModToTime.Add("Keypad Directionality", 15);
      ModToTime.Add("Kugelblitz", 40);
      ModToTime.Add("Multitask", 20);
      ModToTime.Add("OmegaForget", 180);
      ModToTime.Add("Reporting Anomalies", 30);
      ModToTime.Add("RPS Judging", 25);
      ModToTime.Add("Security Council", 25);
      ModToTime.Add("Shoddy Chess", 25);
      ModToTime.Add("Simon Forgets", 120);
      ModToTime.Add("Simon's Stages", 25);
      ModToTime.Add("Souvenir", 5);
      ModToTime.Add("Tallordered Keys", 20);
      ModToTime.Add("The Twin", 50);
      ModToTime.Add("Übermodule", 7);
      ModToTime.Add("Whiteout", 7);

      StartCoroutine(AlternatingFlash());

      foreach (string Mod in Bomb.GetModuleNames()) {
         Debug.Log(Mod);
         foreach (KeyValuePair<string, int> BossModPair in ModToTime) {
            if (Mod == BossModPair.Key) {
               TimeToAdd += BossModPair.Value;
            }
         }
      }

      Spaztic = StartCoroutine(BombSpasm());
   }

   IEnumerator AlternatingFlash () {
      while (true) {
         LeftLight.SetActive(true);
         RightLight.SetActive(false);
         yield return new WaitForSeconds(.125f);
         LeftLight.SetActive(false);
         RightLight.SetActive(true);
         yield return new WaitForSeconds(.125f);
      }
   }

   IEnumerator BombSpasm () {
      while (true) {
         for (int i = 0; i < 3; i++) {
            BombRenderer.sprite = Bombs[i];
            yield return new WaitForSeconds((float) 1 / 30);
         }
      }
   }

   IEnumerator AddTimeToClockAnim () {
      Adding = true;
      if (Countdown != null) {
         StopCoroutine(Countdown);
      }
      if (PlaySFX) {
         Audio.PlaySoundAtTransform("Pickup", transform);
      }
      
      while (CurrentlyBeingAdded > 0) { //1.35f
         CurrentTime++;
         
         yield return new WaitForSeconds(.045f);
         CurrentlyBeingAdded--;
         Debug.Log(CurrentlyBeingAdded);
      }

      Adding = false;
      Countdown = StartCoroutine(Count());
   }

   IEnumerator Count () {
      while (CurrentTime > 0) {
         yield return new WaitForSeconds(1f);
         CurrentTime--;
         if (PlaySFX) {
            Audio.PlaySoundAtTransform("Tick", transform);
         }
      }
      yield return new WaitForSeconds(1f);
      if (PlaySFX) {
         Audio.PlaySoundAtTransform("Tick", transform);
      }
      yield return new WaitForSeconds(1f);
      StartCoroutine(OutOfTime());
   }

   IEnumerator OutOfTime () {
      Strike();
      Music.Stop();
      //Music.volume = 1;
      ActiveTimer = false;
      //Debug.Log(Music.isPlaying);

      Audio.PlaySoundAtTransform("TimesUp", transform);
      while (VignetteScreen.alpha > 0) {
         StrikeReset = true; 
         yield return null;
      }

      GameMusic(true);
      Countdown = null;
      MusicPlaying = false;
      IsFading = false;
      BombRenderer.gameObject.SetActive(true);
      NumbersandColonsAndDoohickeysAndSuch.SetActive(false);
      StrikeReset = false;
      if (Settings.Detonation) {
         StartCoroutine(DETONATE());
      }
      
   }

   void StartFade () {
      IsFading = true;
      VignetteScreen.alpha = 0f;
      StartCoroutine(StartFadePattern());
   }

   IEnumerator StartFadePattern () {

      var duration = .66f;
      var elapsed = 0f;

      while (elapsed < duration) {
         VignetteScreen.alpha = Mathf.Lerp(0, .33f, elapsed / duration);
         yield return null;
         elapsed += Time.deltaTime;
      }

      Fade = StartCoroutine(AlternateFade());
   }

   IEnumerator AlternateFade () {

      var duration = 2f; //The differences between smoothness in the test harness and in game make me want to kms
      var elapsed = 0f;

      while (!ModuleSolved && !StrikeReset) {
         while (elapsed < duration) {
            VignetteScreen.alpha = Mathf.Lerp(.33f, 1, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
         }
         VignetteScreen.alpha = 1;
         elapsed = 0f;
         while (elapsed < duration) {
            VignetteScreen.alpha = Mathf.Lerp(1, .33f, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
         }
         VignetteScreen.alpha = .33f;
         elapsed = 0f;
      }

      while (VignetteScreen.alpha > 0) {
         VignetteScreen.alpha = Mathf.Lerp(.33f, 0f, elapsed / duration);
         yield return null;
         elapsed += Time.deltaTime;
      }
   }

   void Update () { //Shit that happens at any point after initialization

      if (ActiveTimer && CurrentTime <= 10 && !Adding) {
         LightsParent.SetActive(true);
      }
      else {
         LightsParent.SetActive(false);
      }

      if (Selected && Input.GetKeyDown(KeyCode.X)) {
         PlayThousandMarch = !PlayThousandMarch;
      }
      if (Selected && Input.GetKeyDown(KeyCode.C)) {
         PlaySFX = !PlaySFX;
         Audio.PlaySoundAtTransform("Taunt", transform);
      }

      if (PlayThousandMarch) {
         Music.volume = 1;
         if (MusicPlaying) {
            GameMusic(false);
         }
      }
      else {
         Music.volume = 0;
         GameMusic(true);
      }

      //Debug.Log(CurrentTime);
      if (!Adding) {
         TimerDigitsMinutesRen[0].sprite = TimerDigits[CurrentTime / 60 > 9 ? CurrentTime / 600 : 0];
         TimerDigitsMinutesRen[1].sprite = TimerDigits[CurrentTime / 60 % 10];
         TimerDigitsSecondsRen[0].sprite = TimerDigits[CurrentTime % 60 / 10];
         TimerDigitsSecondsRen[1].sprite = TimerDigits[CurrentTime % 10];
         Colon.sprite = ColonColors[0];
      }
      else {
         TimerDigitsMinutesRen[0].sprite = GreenTimerDigits[CurrentTime / 60 > 9 ? CurrentTime / 600 : 0];
         TimerDigitsMinutesRen[1].sprite = GreenTimerDigits[CurrentTime / 60 % 10];
         TimerDigitsSecondsRen[0].sprite = GreenTimerDigits[CurrentTime % 60 / 10];
         TimerDigitsSecondsRen[1].sprite = GreenTimerDigits[CurrentTime % 10];
         Colon.sprite = ColonColors[1];
      }
      

      if (ModuleSolved || !WaitForModCount) {
         if (Countdown != null) {
            StopCoroutine(Countdown);
         }
         return;
      }

      ModCount = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));

      int Solved = Bomb.GetSolvedModuleNames().Count();

      //Debug.Log(Solved);

      if (Solved >= ModCount) { //Do input phase or something here
         Solve();
         StopCoroutine(Count());
         Music.Stop();
         return;
      }
      if (Solved > Stage) { //Put whatever your mod is supposed to do after a solve here. If you want a delay of solves for the purposes of TP, make it a coroutine.

         Stage++;

         if (StrikeReset) {
            return;
         }

         ActiveTimer = true;

         CurrentlyBeingAdded += TimeToAdd;
         BombRenderer.gameObject.SetActive(false);
         NumbersandColonsAndDoohickeysAndSuch.SetActive(true);
         /*if (Spaztic != null) {
            StopCoroutine(Spaztic);
            //DeadBomb.SetActive(true);
         }*/
         if (!MusicPlaying) {
            StartCoroutine(PlayMusic());
         }
         if (!IsFading) {
            StartFade();
         }
         if (!Adding) {
            AddTime = StartCoroutine(AddTimeToClockAnim());
         }
      }
   }

   void Solve () {
      GetComponent<KMBombModule>().HandlePass();
      ModuleSolved = true;
      GameMusic(true);
      StopCoroutine(Count());
      if (Bomb.GetStrikes() == 0) {
         Audio.PlaySoundAtTransform("PRank", transform);
      }
      else if (Bomb.GetStrikes() == 1) {
         Audio.PlaySoundAtTransform("ARank", transform);
      }
      else {
         Audio.PlaySoundAtTransform("DRank", transform);
      }
      ActiveTimer = false;
   }

   void Strike () {
      if (!Autosolved)
         GetComponent<KMBombModule>().HandleStrike();
      GameMusic(true);
   }

   void GameMusic (bool TurnOnGameMusic) {
      if (!Application.isEditor) {
         if (TurnOnGameMusic) {
            GameMusicControl.GameMusicVolume = DefaultGameMusicVolume;
         }
         else {
            GameMusicControl.GameMusicVolume = 0;
         }
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} toggleSFX to mute the SFX. Use !{0} toggleMusic to mute the music.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
      if (Command == "toggleSFX") {
         PlaySFX = !PlaySFX;
      }
      if (Command == "toggleMusic") {
         PlayThousandMarch = !PlayThousandMarch;
      }
   }

   void TwitchHandleForcedSolve () {
      Autosolved = true;
   }
}
