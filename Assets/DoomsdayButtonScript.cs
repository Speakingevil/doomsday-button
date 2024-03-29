﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;
public class DoomsdayButtonScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public KMBossModule bossModule;

    public GameObject lid;
    public KMSelectable modselect;
    public KMSelectable button;
    public Renderer led, btnFace;
    public List<Material> cols;
    public TextMesh clock;
    public GameObject matstore;

    private int presstime;
    private int limit;
    private int solveCount;
    private bool armed;
    private bool gameover;
    private bool[] flip = new bool[2];
    private IEnumerator[] move = new IEnumerator[2]; 
    private IEnumerator blonk;
    private IEnumerator f;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;
    private string[] ignoredMods;

    private DoomsdayButtonSettings settings;
    private bool alterFailure, interactable;
    private Renderer storedBtnRenderer;
    public class DoomsdayButtonSettings 
    {
        public bool ForgivingOutsideofTP = false;
        public bool UseExperimentalDetoTP = false;
    }
    private void Awake()
    {
        f = ItsFuckedForReal();
        moduleID = ++moduleIDCounter;
        modselect.OnFocus = delegate () { if (!moduleSolved && !flip[0] && interactable) { move[0] = MoveLid(true); StartCoroutine(move[0]); } };
        modselect.OnDefocus = delegate () { if (!moduleSolved && !flip[1] && interactable) { move[1] = MoveLid(false); StartCoroutine(move[1]); } };
        button.OnInteract = delegate () { if(!moduleSolved && interactable) Press(); return false; };
        info.OnBombExploded = delegate () {if (gameover) StopCoroutine(f); };
        blonk = Blink();
        matstore.SetActive(false);
        module.OnActivate = Activate;
    }

    private void Activate()
    {
        int time = (int)info.GetTime();
        presstime = Mathf.Max(0, Random.Range((time / 10) * 7, Mathf.Min(time - 19, (time / 10) * 9)));
        clock.color = TimeModeActive ? new Color(1, 0.5f, 0) : ZenModeActive ? Color.cyan : Color.red; // Set the color of the text based on the mode.
        clock.text = Clock(presstime);
        ignoredMods = bossModule.GetIgnoredModules(module, new[] { "Doomsday Button" });
        //limit = info.GetSolvableModuleNames().Where(x => x != "Doomsday Button").Count();
        limit = info.GetSolvableModuleNames().Count(x => !ignoredMods.Contains(x));
        if (!ZenModeActive)
        {
            Debug.LogFormat("[Doomsday Button #{0}] Target time generated: {1}", moduleID, Clock(presstime));
            StartCoroutine(Disarm());
        }
        else
            StartCoroutine(DisarmInZen());
        // Set the color of the button's face and backing based on the mode.
        storedBtnRenderer = button.GetComponent<Renderer>();
        storedBtnRenderer.material.color = TimeModeActive ? new Color(1, 0.5f, 0) : ZenModeActive ? Color.cyan : new Color(2 / 3f, 0, 0);
        btnFace.material.color = TimeModeActive ? new Color(1, 0.5f, 0) : ZenModeActive ? Color.cyan : new Color(2 / 3f, 0, 0);

        if (limit > 0)
        {
            armed = true;
            StartCoroutine(blonk);
        }
        try
        {
            var modSettings = new ModConfig<DoomsdayButtonSettings>("DoomsdayButtonSettings");
            settings = modSettings.Settings;
            modSettings.Settings = settings;
            alterFailure = (!settings.UseExperimentalDetoTP && TwitchPlaysActive) || (!TwitchPlaysActive && settings.ForgivingOutsideofTP);
            
        }
        catch
        {
            Debug.LogFormat("<Doomsday Button #{0}> Settings do NOT work as intended! Using default settings!", moduleID);
            alterFailure = false;
        }
        finally
        {
            Debug.LogFormat("<Doomsday Button #{0}> Deto Altered? {1}", moduleID, alterFailure ? "YES" : "NO");
            if (alterFailure)
            {
                f = ItsFuckedSortOf();
                if (!TwitchPlaysActive)
                    Debug.LogFormat("[Doomsday Button #{0}] Detonation handling has been altered outside of TP. Unless you are not running a mission, this may be considered as advantageous.", moduleID);
            }
            interactable = true;
        }
    }
    private string Clock(int t)
    {
        string d = string.Empty;
        if (t > 86400)
            d = (t / 86400).ToString() + ":";
        if (t > 3600)
            d += (((t % 86400) / 3600) < 10 ? "0" : "") + ((t % 86400) / 3600).ToString() + ":";
        d += ((t % 3600) / 60 < 10 ? "0" : "") + ((t % 3600) / 60).ToString() + ":" + (t % 60 < 10 ? "0" : "") + (t % 60).ToString();
        return d;
    }

    private IEnumerator MoveLid(bool up)
    {
        flip[up ? 0 : 1] = true;
        for (int i = 0; i < 17; i++)
        {
            lid.transform.Rotate(up ? -6 : 6, 0, 0);
            yield return null;
        }
        flip[up ? 0 : 1] = false;
    }

    private IEnumerator Blink()
    {
        while (!moduleSolved)
        {
            led.material = cols[1];
            yield return new WaitForSeconds(0.25f);
            led.material = cols[0];
            yield return new WaitForSeconds(0.25f); 
        }
        led.material = cols[2];
    }

    private IEnumerator Disarm()
    {
        while (!moduleSolved)
        {
            if (info.GetSolvedModuleNames().Count() > solveCount)
            {
                if (armed)
                {
                    armed = false;
                    StopCoroutine(blonk);
                    led.material = cols[0];
                }
                solveCount = info.GetSolvedModuleNames().Count();
            }
            else if ((int)info.GetTime() < presstime &&
                ((!TimeModeActive || info.GetSolvedModuleNames().Count(x => !ignoredMods.Contains(x)) >= info.GetSolvableModuleNames().Count(x => !ignoredMods.Contains(x)))
                && !ZenModeActive))
                StartCoroutine(f);
            yield return null;
        }
    }
    private IEnumerator DisarmInZen()
    {
        Debug.LogFormat("[Doomsday Button #{0}] Target time will respect countdown timer. Zen Mode detected.", moduleID, Clock(presstime));
        while (!moduleSolved)
        {
            if (info.GetSolvedModuleNames().Count() > solveCount)
            {
                if (armed)
                {
                    armed = false;
                    StopCoroutine(blonk);
                    led.material = cols[0];
                }
                solveCount = info.GetSolvedModuleNames().Count();
            }
            presstime = Mathf.FloorToInt(info.GetTime());
            clock.text = Clock(presstime);
            yield return null;
        }
    }

    private IEnumerator RedLight()
    {
        module.HandleStrike();
        if (armed)
            StopCoroutine(blonk);
        led.material = cols[1];
        yield return new WaitForSeconds(1);
        if (armed)
            StartCoroutine(blonk);
        else
            led.material = cols[0];
    }

    private void Press()
    {
        button.AddInteractionPunch(1.5f);
        Audio.PlaySoundAtTransform("Press", button.transform);
        if (ZenModeActive)
        {
            Debug.LogFormat("[Doomsday Button #{0}] Button pressed at correct time.", moduleID);
            if (armed)
            {
                StartCoroutine(RedLight());
                Debug.LogFormat("[Doomsday Button #{0}] Module is armed. Sending strike.", moduleID);
            }
            else
                StartCoroutine(blonk);
            armed = true;
            Debug.LogFormat("[Doomsday Button #{0}] Zen Mode is active. Target time will still respect countdown timer.", moduleID);
            if (solveCount >= limit)
            {
                moduleSolved = true;
                module.HandlePass();
                StopCoroutine(blonk);
                led.material = cols[2];
                clock.text = "GOOD GAME";
                StartCoroutine(FadeToColor(Color.red));
                StartCoroutine(FadeToColorBtn(new Color(2 / 3f, 0, 0)));
                Debug.LogFormat("[Doomsday Button #{0}] Module solved.", moduleID);
            }
            return;
        }
        var timePressed = (int)info.GetTime();
        if (timePressed > presstime)
        {
            StartCoroutine(RedLight());
            Debug.LogFormat("[Doomsday Button #{0}] Too early. ({1})", moduleID, Clock((int)info.GetTime()));
        }
        else if (TimeModeActive && timePressed < presstime)
        {
            StartCoroutine(RedLight());
            Debug.LogFormat("[Doomsday Button #{0}] Too late. Get time back but don't run out of modules. ({1})", moduleID, Clock((int)info.GetTime()));
        }
        else
        {
            Debug.LogFormat("[Doomsday Button #{0}] Button pressed at correct time.", moduleID);
            if (armed)
            {
                StartCoroutine(RedLight());
                Debug.LogFormat("[Doomsday Button #{0}] Module is armed. Sending strike.", moduleID);
            }
            else
                StartCoroutine(blonk);
            armed = true;
            int time = (int)info.GetTime();
            if (solveCount >= limit || time < 60)
            {
                moduleSolved = true;
                module.HandlePass();
                StopCoroutine(blonk);
                led.material = cols[2];
                clock.text = "GOOD GAME";
                StartCoroutine(FadeToColor(Color.red));
                StartCoroutine(FadeToColorBtn(new Color(2 / 3f, 0, 0)));
                Debug.LogFormat("[Doomsday Button #{0}] Module solved.", moduleID);
            }
            else
            {
                presstime = Mathf.Max(0, Random.Range((time / 10) * 7, Mathf.Min(time - 19, (time / 10) * 9)));
                clock.text = Clock(presstime);
                Debug.LogFormat("[Doomsday Button #{0}] New target generated: {1}", moduleID, Clock(presstime));
            }
        }
    }
    private IEnumerator FadeToColorBtn(Color newColor, float speed = 1f)
    {
        var prevColorBtn = storedBtnRenderer.material.color;
        var prevColor = btnFace.material.color;
        for (float x = 0; x < 1f; x += Time.deltaTime * speed)
        {
            storedBtnRenderer.material.color = prevColor * (1 - x) + newColor * x;
            btnFace.material.color = prevColor * (1 - x) + newColor * x;
            yield return null;
        }
        btnFace.material.color = newColor;
        storedBtnRenderer.material.color = newColor;
    }

    private IEnumerator FadeToColor(Color expectedColor, float speed = 1f)
    {
        var prevColor = clock.color;
        for (float x = 0; x < 1f; x += Time.deltaTime * speed)
        {
            clock.color = prevColor * (1 - x) + expectedColor * x;
            yield return null;
        }
        clock.color = expectedColor;
    }

    private IEnumerator ItsFuckedSortOf()
    {
        interactable = false;
        Debug.LogFormat("[Doomsday Button #{0}] Target missed. But it's not over yet.", moduleID);
        StartCoroutine(FadeToColor(Color.red, 5f));
        StartCoroutine(FadeToColorBtn(new Color(2 / 3f, 0, 0), 5f));
        clock.text = "YOU";
        if (flip[0])
            StopCoroutine(move[0]);
        if (flip[1])
            StopCoroutine(move[1]);
        lid.transform.localEulerAngles = new Vector3(90, 0, 0);
        if (armed)
            StopCoroutine(blonk);
        led.material = cols[1];
        Audio.PlaySoundAtTransform("GameOver", transform);
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.5f);
            clock.text = i % 2 == 0 ? string.Empty : new string[] { "FAILED", "SPEAKING", "ISN'T", "PLEASED", "" }[i / 2];
        }
        yield return new WaitForSeconds(0.5f);
        module.HandleStrike();
        interactable = true;
        int time = (int)info.GetTime();
        presstime = Mathf.Max(0, Random.Range((time / 10) * 7, Mathf.Min(time - 19, (time / 10) * 9)));
        clock.text = Clock(presstime);
        Debug.LogFormat("[Doomsday Button #{0}] New target generated: {1}", moduleID, Clock(presstime));
        StartCoroutine(FadeToColor(TimeModeActive ? new Color(1, 0.5f, 0) : Color.red, 1f));
        StartCoroutine(FadeToColorBtn(TimeModeActive ? new Color(1, 0.5f, 0) : new Color(2 / 3f, 0, 0), 1f));
        if (armed)
            StartCoroutine(blonk);
        f = ItsFuckedSortOf();
        StartCoroutine(Disarm());
        yield break;
    }

    private IEnumerator ItsFuckedForReal()
    {
        interactable = false;
        gameover = true;
        TwitchHelpMessage = "GAME OVER. YOU FAILED TO ACCOUNT FOR THIS MODULE. " + TwitchHelpMessage;
        Debug.LogFormat("[Doomsday Button #{0}] Target missed. Game Over.", moduleID);
        StartCoroutine(FadeToColor(Color.red, 5f));
        StartCoroutine(FadeToColorBtn(new Color(2 / 3f, 0, 0), 5f));
        clock.text = "GAME OVER";
        if (flip[0])
            StopCoroutine(move[0]);
        if (flip[1])
            StopCoroutine(move[1]);
        lid.transform.localEulerAngles = new Vector3(90, 0, 0);
        if (armed)
            StopCoroutine(blonk);
        led.material = cols[1];
        moduleSolved = true;
        Audio.PlaySoundAtTransform("GameOver", transform);
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.5f);
            clock.text = i % 2 == 0 ? string.Empty : new string[] { "YOU", "CANNOT", "BEAT", "US", "GAME OVER" }[i / 2];
        }
        if (TwitchPlaysActive && !Application.isEditor)
        {
            TPDetonate();
        }
        else
        while (true)
        {
            module.HandleStrike();
            for (int i = 0; i < 5; i++)
            {
                modselect.AddInteractionPunch(4);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    // TP Handler Begins Here.
    void TPDetonate()
    {
        var TBomb = GetComponentInParent<KMBomb>();
        if (TBomb != null)
        {
            var TScript = TBomb.GetComponent("Bomb");
            if (TScript != null)
            {
                TScript.CallMethod<object>("GetTimer").CallMethod("SubtractTime", info.GetTime());
            }
            else
                Debug.Log("TScript = null");
        }
        else
            Debug.Log("TBomb = null");
    }

    bool TwitchPlaysActive;
    bool TwitchPlaysSkipTimeAllowed = true;
    bool TimeModeActive;
    bool ZenModeActive;
    string TwitchHelpMessage = "Submit at the specific time with \"!{0} submit #:##\" Time ranges can extend up to to DD:HH:MM:SS, while allowing for MM:SS, where MM can exceed 99 minutes.";
    IEnumerator ProcessTwitchCommand(string cmd)
    {
        if (gameover && !alterFailure)
        {
            yield return "sendtochat Your team left Doomsday Button unhandled for too long. Your team will have to pay the price.";
            //yield return "multiple strikes";
            //yield return "detonate";
            yield break;
        }
        Match submitMatch = Regex.Match(cmd, @"^(press|submit)\s\d+(:\d\d){1,3}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
            submitMatchAny = Regex.Match(cmd, @"^(press|submit)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (submitMatchAny.Success)
        {
            if (!ZenModeActive)
                yield return "antitroll I can't allow you to blindly press a button any time. The timer doesn't tick up, and Speaking would not be satsfied about this.";
            yield return null;
            button.OnInteract();
        }
        else if (submitMatch.Success)
        {
            var timeMatch = Regex.Match(cmd, @"\d+(:\d\d){1,3}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Value;
            Debug.LogFormat("<Doomsday Button #{0}> DEBUG: Detected time range {1}.", moduleID, timeMatch);
            var timeMultipliers = new[] { 1, 60, 3600, 86400, 604800 };
            int timeExpected = 0;
            var splittedValues = timeMatch.Split(':');
            for (var x = 0; x < splittedValues.Length; x++)
            {
                int detectedValue;
                var curString = splittedValues[splittedValues.Length - 1 - x];
                if (!int.TryParse(curString, out detectedValue))
                {
                    yield return string.Format("sendtochaterror The module has detected an uncalculable value, \"{0}.\" I am NOT calculating that.", curString);
                    yield break;
                }
                timeExpected += detectedValue * timeMultipliers[x];
            }
            if (timeExpected < 0)
            {
                yield return string.Format("sendtochaterror The module has detected a time that is below 0 seconds. I am NOT going to press it at that time.");
                yield break;
            }
            yield return null;
            //modselect.OnFocus();
            yield return "skiptime " + (timeExpected + (ZenModeActive ? -5 : 5)).ToString();
            if (Mathf.Abs(timeExpected - Mathf.FloorToInt(info.GetTime())) >= 30) yield return "waiting music";
            while (timeExpected != Mathf.FloorToInt(info.GetTime()))
            {
                if ((timeExpected < Mathf.FloorToInt(info.GetTime()) && ZenModeActive) || (timeExpected > Mathf.FloorToInt(info.GetTime()) && !ZenModeActive))
                {
                    yield return string.Format("sendtochat A sudden time change caused the time range to no longer be reached. The press has been canceled because of this.");
                    yield break;
                }
                yield return "trycancel The button press has been canceled!";
            }
            button.OnInteract();
            //modselect.OnDefocus();
            yield return "end waiting music";
        }

        yield break;
    }
}
