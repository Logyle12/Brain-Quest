using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FontSelector : MonoBehaviour
{
    // Start is called before the first frame update
    private TextMeshProUGUI text;

    private int index = 0;
    public int defaultIndex = 0;

    public List<string> data = new();
    void Start()
    {
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();

        // If this selector is choosing the font, start on whatever font is
        // already active (from a previous session) instead of always
        // defaulting back to index 0.
        index = (FontManager.Instance != null) ? FontManager.Instance.CurrentFontIndex : defaultIndex;
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
        FontManager.Instance?.SetFont(index);
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
        FontManager.Instance?.SetFont(index);

    }

    public int getIndexValue() 
    {

        return index;
    
    }

}