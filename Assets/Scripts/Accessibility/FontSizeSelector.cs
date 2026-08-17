using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FontSizeSelector : MonoBehaviour
{
    // Start is called before the first frame update
    private TextMeshProUGUI text;

    private int index = 0;
    public int defaultIndex = 1; // "Default (100%)" - the XAG 101 baseline, not index 0

    // Display labels only - e.g. "Small", "Default", "Large", "Maximum".
    // The actual px/multiplier values live on FontManager, same split as
    // FontSelector.cs (this holds names, FontManager holds the TMP_FontAsset refs).
    public List<string> data = new();

    void Start()
    {
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();

        // If this selector is choosing size, start on whatever size is
        // already active (from a previous session) instead of always
        // defaulting back to index 1.
        index = (FontManager.Instance != null) ? FontManager.Instance.CurrentSizeIndex : defaultIndex;
        text.text = data[index];

        transform.Find("Previous").GetComponent<Button>().onClick.AddListener(OnLeftClicked);
        transform.Find("Next").GetComponent<Button>().onClick.AddListener(OnRightClicked);
    }

    public int indexValue
    {
        get
        {
            return index;
        }

        set
        {
            index = value;
            text.text = data[index];
        }
    }

    public string selectedValue
    {
        get
        {
            return data[index];
        }
    }

    void OnLeftClicked()
    {
        if (index == 0)
        {
            index = data.Count - 1;
        }
        else
        {
            index--;
        }

        text.text = data[index];
        FontManager.Instance?.SetSize(index);
    }

    void OnRightClicked()
    {
        if (index + 1 >= data.Count)
        {
            index = 0;
        }
        else
        {
            index++;
        }

        text.text = data[index];
        FontManager.Instance?.SetSize(index);
    }

    public int getIndexValue()
    {
        return index;
    }
}