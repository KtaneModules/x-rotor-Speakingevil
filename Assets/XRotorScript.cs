using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class XRotorScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public List<KMSelectable> buttons;
    public Renderer[] blabels;
    public Texture2D[] bsymbols;
    public GameObject[] scansymbols;
    public Transform scanpos;
    public Transform[] pivots;

    private int[][] choosesymb = new int[2][] { new int[5], new int[5]};
    private List<int> ans = new List<int> { 0, 1, 2, 3, 4 };
    private List<int> sub = new List<int> { };
    private int[] turn = new int[5];
    private bool[] pressed = new bool[5];

    private static int moduleIDCounter;
    private int moduleID;

    private void Start()
    {
        moduleID = ++moduleIDCounter;
        for (int i = 0; i < 64; i++)
            scansymbols[i].SetActive(false);
        ans = ans.Shuffle();
        int[][] indices = new int[2][];
        for (int i = 0; i < 2; i++)
            indices[i] = Enumerable.Range(0, 8).ToArray().Shuffle().Take(5).ToArray();
        for (int i = 0; i < 5; i++)
        {
            choosesymb[0][i] = (indices[0][i] * 8) + indices[1][i];
            blabels[i].material.mainTexture = bsymbols[choosesymb[0][i]];
        }
        List<int> available = Enumerable.Range(0, 64).ToList();
        for (int i = 0; i < 64; i++)
            if (choosesymb[0].Contains(i) || (!indices[0].Contains(i / 8) && !indices[1].Contains(i % 8)))
                available.Remove(i);
        for(int i = 0; i < 5; i++)
        {
            int ch = Random.Range(0, 5);
            if(Random.Range(0, 2) == 0)
            {
                while (ch == i || !available.Contains((indices[0][i] * 8) + indices[1][ch]) || (i < 4 && available.Count(x => x / 8 == ch) < 2))
                    ch = Random.Range(0, 5);
                choosesymb[1][i] = (indices[0][i] * 8) + indices[1][ch];
                turn[i] = indices[1][ch] > indices[1][i] ? 2 : 0;
            }
            else
            {
                while (ch == i || !available.Contains((indices[0][ch] * 8) + indices[1][i]) || (i < 4 && available.Count(x => x % 8 == ch) < 2))
                    ch = Random.Range(0, 5);
                choosesymb[1][i] = (indices[0][ch] * 8) + indices[1][i];
                turn[i] = indices[0][ch] > indices[0][i] ? 3 : 1;
            }
            available.Remove(choosesymb[1][i]);
        }
        Debug.LogFormat("[X-Rotor #{0}] The button symbols are: {1}", moduleID, string.Join(", ", Enumerable.Range(0, 5).Select(x => "ABCDEFGH"[indices[1][x]] + (indices[0][x] + 1).ToString()).ToArray()));
        Debug.LogFormat("[X-Rotor #{0}] The scanned symbols are decoded to: {1}", moduleID, string.Join(", ", ans.Select(x => new string[] { "Right", "Down", "Left", "Up"}[turn[x]] + " from " + "ABCDEFGH"[choosesymb[1][x] % 8] + ((choosesymb[1][x] / 8) + 1).ToString()).ToArray()));
        Debug.LogFormat("[X-Rotor #{0}] Press the buttons in this order: {1}", moduleID, string.Join(", ", ans.Select(x => "ABCDEFGH"[indices[1][x]] + (indices[0][x] + 1).ToString()).ToArray()));
        IEnumerator c = Display(ans.Select(x => choosesymb[1][x]).ToArray(), ans.Select(x => turn[x]).ToArray());
        foreach (KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if (!pressed[b])
                {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                    button.AddInteractionPunch(0.5f);
                    pressed[b] = true;
                    sub.Add(b);
                    if(pressed.All(x => x))
                    {
                        Debug.LogFormat("[X-Rotor #{0}] The buttons were pressed in the order: {1}", moduleID, string.Join(", ", sub.Select(x => "ABCDEFGH"[indices[1][x]] + (indices[0][x] + 1).ToString()).ToArray()));
                        if (ans.SequenceEqual(sub))
                        {
                            Audio.PlaySoundAtTransform("Solve", transform);
                            module.HandlePass();
                            StopCoroutine(c);
                            for (int i = 0; i < 5; i++)
                                scansymbols[choosesymb[1][i]].SetActive(false);
                        }
                        else
                        {
                            module.HandleStrike();
                            pressed = new bool[5];
                            sub.Clear();
                        }
                    }
                }
                return false;
            };
        }
        StartCoroutine(c);
    }

    private IEnumerator Display(int[] symb, int[] rot)
    {
        for(int i = 0; i < 5; i++)
        {
            scansymbols[symb[i]].SetActive(true);
            int d = rot[i] < 2 ? 1 : -1;
            scanpos.RotateAround(pivots[rot[i] % 2].position, transform.up, -60 * d);
            for(int j = 0; j < 60; j++)
            {
                scanpos.RotateAround(pivots[rot[i] % 2].position, transform.up,  2 * d);
                yield return new WaitForSeconds(0.05f);
            }
            scansymbols[symb[i]].SetActive(false);
            scanpos.RotateAround(pivots[rot[i] % 2].position, transform.up, -60 * d);
            if(i == 4)
            {
                i = -1;
                yield return new WaitForSeconds(2);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <1-5> [Presses buttons in reading order] | !{0} reset [Undoes presses]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Replace(" ", "");
        if(command.ToLowerInvariant() == "reset")
        {
            yield return null;
            pressed = new bool[5];
            sub.Clear();
            yield break;
        }
        for(int i = 0; i < command.Length; i++)
        {
            if (!"12345".Contains(command[i]))
            {
                yield return "sendtochaterror There is no button \"" + command[i] + "\"";
                yield break;
            }
            if (pressed[command[i] - '1'] || command.Count(x => command[i] == x) > 1)
            {
                yield return "sendtochaterror Button " + command[i] + " cannot be pressed more than once.";
                yield break;
            }
        }
        int[] press = command.Select(x => x - '1').ToArray();
        for(int i = 0; i < command.Length; i++)
        {
            yield return null;
            buttons[press[i]].OnInteract();
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        pressed = new bool[5];
        sub.Clear();
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            buttons[ans[i]].OnInteract();
        }
    }
}
