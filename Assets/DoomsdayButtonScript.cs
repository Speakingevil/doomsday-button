using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoomsdayButtonScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;

    public GameObject lid;
    public KMSelectable modselect;
    public KMSelectable button;
    public Renderer led;
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

    private void Awake()
    {
        f = ItsFucked();
        moduleID = ++moduleIDCounter;
        modselect.OnFocus = delegate () { if (!moduleSolved && !flip[0]) { move[0] = MoveLid(true); StartCoroutine(move[0]); } };
        modselect.OnDefocus = delegate () { if (!moduleSolved && !flip[1]) { move[1] = MoveLid(false); StartCoroutine(move[1]); } };
        button.OnInteract = delegate () { if(!moduleSolved) Press(); return false; };
        info.OnBombExploded = delegate () {if (gameover) StopCoroutine(f); };
        blonk = Blink();
        matstore.SetActive(false);
        module.OnActivate = Activate;
    }

    private void Activate()
    {
        int time = (int)info.GetTime();
        presstime = Random.Range((time / 10) * 7, Mathf.Min(time - 19, (time / 10) * 9));
        clock.text = Clock(presstime);
        limit = info.GetSolvableModuleNames().Where(x => x != "Doomsday Button").Count();
        Debug.LogFormat("[Doomsday Button #{0}] Target time generated: {1}", moduleID, Clock(presstime));
        StartCoroutine(Disarm());
        if (limit > 0)
        {
            armed = true;
            StartCoroutine(blonk);
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
            else if ((int)info.GetTime() < presstime)
                StartCoroutine(f);
            yield return new WaitForSeconds(0.5f);
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
        if ((int)info.GetTime() > presstime)
        {
            StartCoroutine(RedLight());
            Debug.LogFormat("[Doomsday Button #{0}] Too early. ({1})", moduleID, Clock((int)info.GetTime()));
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
                Debug.LogFormat("[Doomsday Button #{0}] Module solved.", moduleID);
            }
            else
            {
                presstime = Random.Range((time / 10) * 7, Mathf.Min(time - 19, (time / 10) * 9));
                clock.text = Clock(presstime);
                Debug.LogFormat("[Doomsday Button #{0}] New target generated: {1}", moduleID, Clock(presstime));
            }
        }
    }

    private IEnumerator ItsFucked()
    {
        gameover = true;
        Debug.LogFormat("[Doomsday Button #{0}] Target missed. Game Over.", moduleID);
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
            clock.text = i % 2 == 0 ? string.Empty : new string[] { "YOU", "CANNOT", "BEAT", "US", "GAME OVER"}[i / 2];
        }
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
}
