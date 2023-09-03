using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class catchphraseScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colourblind;
    public KMSelectable[] panels;
    public KMSelectable[] keypads;
    public KMSelectable clearButton;
    public KMSelectable submitButton;
    public GameObject winDisplay;
    public TextMesh winText;

    public int[] firstPressTime;
    public int[] secondPressTime;
    public int[] thirdPressTime;
    public int[] fourthPressTime;
    private int[] pressTimes = new int[4];
    private float timeOfPress = 0f;
    private int timeOfPressInt = 0;

    public string[] decoyLetters;
    public TextMesh[] potentialNumbers;
    private List<int> selectedIndices = new List<int>();
    private List<int> selectedDecoyIndices = new List<int>();
    private TextMesh[] selectedNumbers = new TextMesh[5];
    private int[] selectedNumberValues = new int[5];
    private int correctAnswer = 0;
    private string correctAnswerString = "";
    public Color[] fontColours;

    private List<string> selectedColours = new List<string>();
    private bool colourblindEnabled;

    public List<KMSelectable> correctPanels1 = new List<KMSelectable>();
    private bool stage1Correct;
    private string stage1Colour = "";
    private string stage1Position = "";

    public List<KMSelectable> correctPanels2 = new List<KMSelectable>();
    private bool stage2Correct;

    private List<KMSelectable> remainingPanels = new List<KMSelectable>();
    public List<KMSelectable> correctPanels3 = new List<KMSelectable>();
    private bool stage3Correct;

    public Material[] panelColourOptions;
    public Renderer[] panelRenderers;

    public TextMesh answerBox;

    private List<int> serialNumberInts = new List<int>();
    private int serialLettersSum = 0;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool shrinking;
    private int stage = 1;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable panel in panels)
        {
            KMSelectable pressedPanel = panel;
            panel.OnInteract += delegate () { PanelPress(pressedPanel); return false; };
        }
        foreach (KMSelectable keypad in keypads)
        {
            KMSelectable pressedPad = keypad;
            keypad.OnInteract += delegate () { KeyPress(pressedPad); return false; };
        }
        clearButton.OnInteract += delegate () { ClearPress(); return false; };
        submitButton.OnInteract += delegate () { SubmitPress(); return false; };
    }

    void Start()
    {
        colourblindEnabled = Colourblind.ColorblindModeActive;
        answerBox.text = "";
        SetNumbers();
        GetTimingRow();
        PickPanelColours();
        GetPanel1();
    }

    void SetNumbers()
    {
        for(int i = 0; i <= 9; i++)
        {
            int index = UnityEngine.Random.Range(0,24);
            while(selectedDecoyIndices.Contains(index))
            {
                index = UnityEngine.Random.Range(0,24);
            }
            selectedDecoyIndices.Add(index);
            potentialNumbers[i].text = decoyLetters[index];
        }
        for(int i = 0; i <= 4; i++)
        {
            int index = UnityEngine.Random.Range(0,8);
            while(selectedIndices.Contains(index))
            {
                index = UnityEngine.Random.Range(0,8);
            }
            selectedIndices.Add(index);
            selectedNumbers[i] = potentialNumbers[index];
        }
        for(int i = 0; i <= 4; i++)
        {
            int index = UnityEngine.Random.Range(2,10);
            selectedNumberValues[i] = index;
            selectedNumbers[i].text = index.ToString();
        }
        foreach(TextMesh number in potentialNumbers)
        {
            int index = UnityEngine.Random.Range(0,7);
            number.color = fontColours[index];
        }
        correctAnswer = selectedNumberValues[0] * selectedNumberValues[1] * selectedNumberValues[2] * selectedNumberValues[3] * selectedNumberValues[4];
        correctAnswerString = correctAnswer.ToString();
        winText.text = correctAnswerString + "!";
        winDisplay.SetActive(false);
        Debug.LogFormat("[Catchphrase #{0}] Your hidden numbers are {1}, {2}, {3} {4} & {5}.", moduleId, selectedNumberValues[0], selectedNumberValues[1], selectedNumberValues[2], selectedNumberValues[3], selectedNumberValues[4]);
        Debug.LogFormat("[Catchphrase #{0}] The product of your hidden numbers is {1}.", moduleId, correctAnswerString);
    }

    void GetTimingRow()
    {
        foreach (char c in Bomb.GetSerialNumberLetters())
        {
            serialNumberInts.Add(c >= '0' && c <= '9' ? (c - '0') : ((c - 'A' + 1) % 10));
        }
        serialLettersSum = (serialNumberInts.Sum() + Bomb.GetOnIndicators().Count()) % 10;
        pressTimes[0] = firstPressTime[serialLettersSum];
        pressTimes[1] = secondPressTime[serialLettersSum];
        pressTimes[2] = thirdPressTime[serialLettersSum];
        pressTimes[3] = fourthPressTime[serialLettersSum];
    }

    void PickPanelColours()
    {
        foreach(Renderer panel in panelRenderers)
        {
            int index = UnityEngine.Random.Range(0,6);
            panel.material = panelColourOptions[index];
            panel.GetComponent<panels>().colour = panelColourOptions[index].name;
            if (colourblindEnabled)
                panel.GetComponent<panels>().colourblindText.text = panelColourOptions[index].name[0].ToString();
            selectedColours.Add(panelColourOptions[index].name);
        }
        Debug.LogFormat("[Catchphrase #{0}] Your panel colours in reading order are {1}.", moduleId, string.Join(", ", selectedColours.Select((x) => x).ToArray()));
    }

    void GetPanel1()
    {
        if(selectedColours.Distinct().Count() == 4)
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "red")
                {
                    correctPanels1.Add(panels[i]);
                }
            }
            if(correctPanels1.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TL")
                    {
                        correctPanels1.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels1.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The first correct panel is {1} ({2}).", moduleId, correctPanels1[0].GetComponent<panels>().colour, string.Join(", ", correctPanels1.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the first panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[0]);
            return;
        }
        else if(Bomb.GetPortPlates().Any(x => x.Length == 0))
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "blue")
                {
                    correctPanels1.Add(panels[i]);
                }
            }
            if(correctPanels1.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "BR")
                    {
                        correctPanels1.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels1.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The first correct panel is {1} ({2}).", moduleId, correctPanels1[0].GetComponent<panels>().colour, string.Join(", ", correctPanels1.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the first panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[0]);
            return;
        }
        else if(selectedColours.Contains("green"))
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "purple")
                {
                    correctPanels1.Add(panels[i]);
                }
            }
            if(correctPanels1.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "BL")
                    {
                        correctPanels1.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels1.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The first correct panel is {1} ({2}).", moduleId, correctPanels1[0].GetComponent<panels>().colour, string.Join(", ", correctPanels1.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the first panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[0]);
            return;
        }
        else if(Bomb.GetBatteryCount() > 4)
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "green")
                {
                    correctPanels1.Add(panels[i]);
                }
            }
            if(correctPanels1.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TR")
                    {
                        correctPanels1.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels1.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The first correct panel is {1} ({2}).", moduleId, correctPanels1[0].GetComponent<panels>().colour, string.Join(", ", correctPanels1.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the first panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[0]);
            return;
        }
        else
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "yellow")
                {
                    correctPanels1.Add(panels[i]);
                }
            }
            if(correctPanels1.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().colour == "orange")
                    {
                        correctPanels1.Add(panels[i]);
                    }
                }
            }
            if(correctPanels1.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TL")
                    {
                        correctPanels1.Add(panels[i]);
                    }
                }
            }
        }
        Debug.LogFormat("[Catchphrase #{0}] The first correct panel is {1} ({2}).", moduleId, correctPanels1[0].GetComponent<panels>().colour, string.Join(", ", correctPanels1.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
        Debug.LogFormat("[Catchphrase #{0}] Press the first panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[0]);
    }

    void GetPanel2()
    {
        if(stage1Colour == "red" || stage1Colour == "green")
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "blue")
                {
                    correctPanels2.Add(panels[i]);
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TR")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "BR")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels2.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The second correct panel is {1} ({2}).", moduleId, correctPanels2[0].GetComponent<panels>().colour, string.Join(", ", correctPanels2.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the second panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[1]);
            return;
        }
        else if(stage1Position == "TL")
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "orange")
                {
                    correctPanels2.Add(panels[i]);
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "BL")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels2.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The second correct panel is {1} ({2}).", moduleId, correctPanels2[0].GetComponent<panels>().colour, string.Join(", ", correctPanels2.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the second panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[1]);
            return;
        }
        else if(stage1Colour == "purple" || stage1Position == "BR")
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "yellow")
                {
                    correctPanels2.Add(panels[i]);
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TR")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TL")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
        }

        if(correctPanels2.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The second correct panel is {1} ({2}).", moduleId, correctPanels2[0].GetComponent<panels>().colour, string.Join(", ", correctPanels2.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the second panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[1]);
            return;
        }
        else
        {
            for(int i = 0; i <= 3; i++)
            {
                if(panels[i].GetComponent<panels>().colour == "green")
                {
                    correctPanels2.Add(panels[i]);
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().colour == "red")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "BR")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
            if(correctPanels2.Count() == 0)
            {
                for(int i = 0; i <= 3; i++)
                {
                    if(panels[i].GetComponent<panels>().position == "TL")
                    {
                        correctPanels2.Add(panels[i]);
                    }
                }
            }
        }
          Debug.LogFormat("[Catchphrase #{0}] The second correct panel is {1} ({2}).", moduleId, correctPanels2[0].GetComponent<panels>().colour, string.Join(", ", correctPanels2.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
          Debug.LogFormat("[Catchphrase #{0}] Press the second panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[1]);
    }


    void GetPanel3()
    {
        for(int i = 0; i <= 3; i++)
        {
            if(panels[i].GetComponent<panels>().colour != "")
            {
                remainingPanels.Add(panels[i]);
            }
        }

        if((remainingPanels[0].GetComponent<panels>().position == "TR" || remainingPanels[0].GetComponent<panels>().position == "TL") && (remainingPanels[1].GetComponent<panels>().position == "TR" || remainingPanels[1].GetComponent<panels>().position == "TL") && (remainingPanels[0].GetComponent<panels>().colour == remainingPanels[1].GetComponent<panels>().colour))
        {
            for(int i = 0; i <= 1; i++)
            {
                if(remainingPanels[i].GetComponent<panels>().position == "TL")
                {
                    correctPanels3.Add(remainingPanels[i]);
                }
            }
        }

        if(correctPanels3.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The third correct panel is {1} ({2}).", moduleId, correctPanels3[0].GetComponent<panels>().colour, string.Join(", ", correctPanels3.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the third panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[2]);
            return;
        }
        else if((remainingPanels[0].GetComponent<panels>().position == "TL" || remainingPanels[0].GetComponent<panels>().position == "BL") && (remainingPanels[1].GetComponent<panels>().position == "TL" || remainingPanels[1].GetComponent<panels>().position == "BL") && (remainingPanels[0].GetComponent<panels>().colour != remainingPanels[1].GetComponent<panels>().colour))
        {
            for(int i = 0; i <= 1; i++)
            {
                if(remainingPanels[i].GetComponent<panels>().position == "BL")
                {
                    correctPanels3.Add(remainingPanels[i]);
                }
            }
        }

        if(correctPanels3.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The third correct panel is {1} ({2}).", moduleId, correctPanels3[0].GetComponent<panels>().colour, string.Join(", ", correctPanels3.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the third panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[2]);
            return;
        }
        else if((remainingPanels[0].GetComponent<panels>().position == "BL" || remainingPanels[0].GetComponent<panels>().position == "BR") && (remainingPanels[1].GetComponent<panels>().position == "BL" || remainingPanels[1].GetComponent<panels>().position == "BR"))
        {
            for(int i = 0; i <= 1; i++)
            {
                if(remainingPanels[i].GetComponent<panels>().position == "BR")
                {
                    correctPanels3.Add(remainingPanels[i]);
                }
            }
        }

        if(correctPanels3.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The third correct panel is {1} ({2}).", moduleId, correctPanels3[0].GetComponent<panels>().colour, string.Join(", ", correctPanels3.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the third panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[2]);
            return;
        }
        else if((remainingPanels[0].GetComponent<panels>().position == "BR" || remainingPanels[0].GetComponent<panels>().position == "TR") && (remainingPanels[1].GetComponent<panels>().position == "BR" || remainingPanels[1].GetComponent<panels>().position == "TR"))
        {
            for(int i = 0; i <= 1; i++)
            {
                if(remainingPanels[i].GetComponent<panels>().position == "TR")
                {
                    correctPanels3.Add(remainingPanels[i]);
                }
            }
        }

        if(correctPanels3.Count() > 0)
        {
            Debug.LogFormat("[Catchphrase #{0}] The third correct panel is {1} ({2}).", moduleId, correctPanels3[0].GetComponent<panels>().colour, string.Join(", ", correctPanels3.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the third panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[2]);
            return;
        }
        else
        {
            for(int i = 0; i <= 1; i++)
            {
                if(remainingPanels[i].GetComponent<panels>().colour == "red")
                {
                    correctPanels3.Add(remainingPanels[i]);
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().position == "TL")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().colour == "blue")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().position == "BL")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().colour == "green")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().position == "BR")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().colour == "orange")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().position == "TR")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().colour == "yellow")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            if(correctPanels3.Count() == 0)
            {
                for(int i = 0; i <= 1; i++)
                {
                    if(remainingPanels[i].GetComponent<panels>().colour == "purple")
                    {
                        correctPanels3.Add(remainingPanels[i]);
                    }
                }
            }
            Debug.LogFormat("[Catchphrase #{0}] The third correct panel is {1} ({2}).", moduleId, correctPanels3[0].GetComponent<panels>().colour, string.Join(", ", correctPanels3.Select((x) => x.GetComponent<panels>().logPosition).ToArray()));
            Debug.LogFormat("[Catchphrase #{0}] Press the third panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[2]);
        }
    }

    void PanelPress(KMSelectable panel)
    {
        if(moduleSolved || shrinking)
        {
            return;
        }
        submitButton.AddInteractionPunch();
        timeOfPress = Bomb.GetTime();
        timeOfPressInt = (Mathf.FloorToInt(timeOfPress)) % 10;
        if(timeOfPressInt != pressTimes[stage-1])
        {
            Debug.LogFormat("[Catchphrase #{0}] Strike! You pressed the {1} panel when the last digit of the bomb timer was {2}. That is not correct.", moduleId, panel.GetComponent<panels>().logPosition, timeOfPressInt);
            GetComponent<KMBombModule>().HandleStrike();
            return;
        }

        if(stage == 1)
        {
            for(int i = 0; i < correctPanels1.Count(); i++)
            {
                if(panel == correctPanels1[i])
                {
                    stage1Correct = true;
                    break;
                }
            }
            if(stage1Correct)
            {
                Audio.PlaySoundAtTransform("reveal", transform);
                Debug.LogFormat("[Catchphrase #{0}] You pressed the {1} panel. That is correct.", moduleId, panel.GetComponent<panels>().logPosition);
                shrinking = true;
                panel.GetComponentInParent<Animator>().SetBool("shrink", true);
                stage++;
                stage1Colour = panel.GetComponent<panels>().colour;
                stage1Position = panel.GetComponent<panels>().position;
                panel.GetComponent<panels>().colour = "";
                panel.GetComponent<panels>().position = "";
                GetPanel2();
                StartCoroutine(StopShrink());
            }
            else
            {
                Debug.LogFormat("[Catchphrase #{0}] Strike! You pressed the {1} panel. That is not correct.", moduleId, panel.GetComponent<panels>().logPosition);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
        else if(stage == 2)
        {
            for(int i = 0; i < correctPanels2.Count(); i++)
            {
                if(panel == correctPanels2[i])
                {
                    stage2Correct = true;
                    break;
                }
            }
            if(stage2Correct)
            {
                Audio.PlaySoundAtTransform("reveal", transform);
                Debug.LogFormat("[Catchphrase #{0}] You pressed the {1} panel. That is correct.", moduleId, panel.GetComponent<panels>().logPosition);
                shrinking = true;
                panel.GetComponentInParent<Animator>().SetBool("shrink", true);
                stage++;
                panel.GetComponent<panels>().colour = "";
                panel.GetComponent<panels>().position = "";
                GetPanel3();
                StartCoroutine(StopShrink());
            }
            else
            {
                Debug.LogFormat("[Catchphrase #{0}] Strike! You pressed the {1} panel. That is not correct.", moduleId, panel.GetComponent<panels>().logPosition);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }

        else if(stage == 3)
        {
            for(int i = 0; i < correctPanels3.Count(); i++)
            {
                if(panel == correctPanels3[i])
                {
                    stage3Correct = true;
                    break;
                }
            }
            if(stage3Correct)
            {
                Audio.PlaySoundAtTransform("reveal", transform);
                Debug.LogFormat("[Catchphrase #{0}] You pressed the {1} panel. That is correct.", moduleId, panel.GetComponent<panels>().logPosition);
                shrinking = true;
                panel.GetComponentInParent<Animator>().SetBool("shrink", true);
                stage++;
                panel.GetComponent<panels>().colour = "";
                panel.GetComponent<panels>().position = "";
                StartCoroutine(StopShrink());
                Debug.LogFormat("[Catchphrase #{0}] Press the final panel when the last digit of the bomb timer is {1}.", moduleId, pressTimes[3]);
            }
            else
            {
                Debug.LogFormat("[Catchphrase #{0}] Strike! You pressed the {1} panel. That is not correct.", moduleId, panel.GetComponent<panels>().logPosition);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }

        else if(stage == 4)
        {
            Audio.PlaySoundAtTransform("reveal", transform);
            Debug.LogFormat("[Catchphrase #{0}] You pressed the {1} panel. That is correct.", moduleId, panel.GetComponent<panels>().logPosition);
            shrinking = true;
            panel.GetComponentInParent<Animator>().SetBool("shrink", true);
            stage++;
            panel.GetComponent<panels>().colour = "";
            panel.GetComponent<panels>().position = "";
            StartCoroutine(StopShrink());
        }

        else
        {
            return;
        }
    }

    void KeyPress(KMSelectable keypad)
    {
        if(moduleSolved)
        {
            return;
        }
        keypad.AddInteractionPunch(0.5f);
        Audio.PlaySoundAtTransform("keyStroke", transform);
        if(answerBox.text.Length < 5)
        {
            answerBox.text += keypad.GetComponentInChildren<TextMesh>().text;
        }
        else
        {
            return;
        }
    }

    void ClearPress()
    {
        if(moduleSolved)
        {
            return;
        }
        clearButton.AddInteractionPunch();
        Audio.PlaySoundAtTransform("keyStroke", transform);
        answerBox.text = "";
    }

    void SubmitPress()
    {
        if(moduleSolved)
        {
            return;
        }
        submitButton.AddInteractionPunch();
        Audio.PlaySoundAtTransform("keyStroke", transform);
        if(answerBox.text == correctAnswerString)
        {
            Audio.PlaySoundAtTransform("right", transform);
            GetComponent<KMBombModule>().HandlePass();
            foreach(KMSelectable panel in panels)
            {
                panel.GetComponentInParent<Animator>().SetBool("shrink", true);
            }
            winDisplay.SetActive(true);
            moduleSolved = true;
            Debug.LogFormat("[Catchphrase #{0}] You entered {1}. That is correct. Module disarmed.", moduleId, correctAnswerString);
        }
        else
        {
            Debug.LogFormat("[Catchphrase #{0}] Strike! You entered {1}. That is incorrect.", moduleId, answerBox.text);
            GetComponent<KMBombModule>().HandleStrike();
            answerBox.text = "";
        }
    }

    IEnumerator StopShrink()
    {
        yield return new WaitForSeconds(4f);
        shrinking = false;
    }

	bool InRange(int number, int min, int max)
	{
		return max >= number && number >= min;
	}

	public string TwitchHelpMessage = "Press a panel at a specific digit using !{0} panel 2 at 8. Panels are in english reading order. Submit a number using !{0} submit 480. Toggle colourblind mode using !{0} colourblind.";

	public IEnumerator ProcessTwitchCommand(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		int panelPosition = 0;
		int timerDigit = 0;
		int result = 0;
        if (commands.Length == 1 && commands[0] == "colourblind")
        {
            yield return null;

            colourblindEnabled = !colourblindEnabled;
            foreach (Renderer panel in panelRenderers)
            {
                if (colourblindEnabled)
                    panel.GetComponent<panels>().colourblindText.text = panel.GetComponent<panels>().colour[0].ToString();
                else
                    panel.GetComponent<panels>().colourblindText.text = "";
            }
        }
		else if (commands.Length == 2 && commands[0] == "submit" && commands[1].Length <= 5 && int.TryParse(commands[1], out result))
		{
			yield return null;

			foreach (char digit in commands[1])
			{
				keypads[digit != '0' ? digit - '1' : 9].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			submitButton.OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		else if (commands.Length == 4 && commands[0] == "panel" && int.TryParse(commands[1], out panelPosition) && InRange(panelPosition, 1, 4) && commands[2] == "at" && int.TryParse(commands[3], out timerDigit) && InRange(timerDigit, 0, 9))
		{
			yield return null;

			while (Mathf.FloorToInt(Bomb.GetTime()) % 10 != timerDigit)
				yield return "trycancel The panel was not opened due to a request to cancel.";

			panels[panelPosition - 1].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
	}
}
