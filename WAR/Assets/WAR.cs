using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class WAR : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public AudioSource Music;
   public AudioClip[] MusicParts;

   public CanvasGroup VignetteScreen;

   public static string[] ignoredModules = null;

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

   Coroutine Countdown;
   Coroutine AddTime;
   Coroutine Fade;

   bool Adding;
   bool IsFading;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   int TimeToAdd = 30;
   int CurrentTime = 0;
   int CurrentlyBeingAdded;

   bool MusicPlaying;

   Dictionary<string, int> ModToTime = new Dictionary<string, int>();
   

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      /*
      foreach (KMSelectable object in keypad) {
          object.OnInteract += delegate () { keypadPress(object); return false; };
      }
      */

      //button.OnInteract += delegate () { buttonPress(); return false; };

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
      
   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on

   }

   IEnumerator PlayMusic () {
      MusicPlaying = true;
      Music.Play();
      while (Music.isPlaying) {
         yield return null;
      }
      Music.clip = MusicParts[1];
      Music.loop = true;
      Music.Play();
   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      WaitForModCount = true;
      ModToTime.Add("14", 90);
      ModToTime.Add("42", 30);
      ModToTime.Add("501", 30);
      ModToTime.Add("A>N<D", 20);
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
      ModToTime.Add("Forget The Colors", 240);
      ModToTime.Add("Forget This", 15);
      ModToTime.Add("Forget Us Not", 18);
      ModToTime.Add("Iconic", 3);
      ModToTime.Add("Keypad Directionality", 15);
      ModToTime.Add("Kugelblitz", 40);
      ModToTime.Add("Multitask", 20);
      ModToTime.Add("OmegaForget", 120);
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


      foreach (string Mod in Bomb.GetModuleNames()) {
         Debug.Log(Mod);
         foreach (KeyValuePair<string, int> BossModPair in ModToTime) {
            if (Mod == BossModPair.Key) {
               TimeToAdd += BossModPair.Value;
            }
         }
      }
   }

   IEnumerator AddTimeToClockAnim () {
      Adding = true;
      if (Countdown != null) {
         StopCoroutine(Countdown);
      }
      Audio.PlaySoundAtTransform("Pickup", transform);
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



      while (true) {
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
   }

   void Update () { //Shit that happens at any point after initialization

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
         CurrentlyBeingAdded += TimeToAdd;
         if (!MusicPlaying) {
            StartCoroutine(PlayMusic());
         }
         if (!IsFading) {
            StartFade();
         }
         if (!Adding) {
            AddTime = StartCoroutine(AddTimeToClockAnim());
         }
         
         Debug.Log(Stage); //Stage is 0 indexed, so adjust what you need for your specific circumstances.
         Stage++;
      }
   }

   void Solve () {
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike () {
      GetComponent<KMBombModule>().HandleStrike();
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} mute to mute the music.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
   }
}
