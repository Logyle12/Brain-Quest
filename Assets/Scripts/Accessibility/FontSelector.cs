using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FontSelector : MonoBehaviour
{
    // The label showing the current font name
    private TextMeshProUGUI text;

    // Currently selected index into data
    private int index = 0;
    // Used if FontManager isn't around yet to load a real saved index
    public int defaultIndex = 0;

    // Display names for each font, filled in from the Inspector
    public List<string> data = new();
    void Start()
    {
        // Grab the text component this selector displays its current choice on
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();

        // Start on whatever font's already active from last session, not always index 0
        index = (FontManager.Instance != null) ? FontManager.Instance.CurrentFontIndex : defaultIndex;
        // Show that choice right away
        text.text = data[index];

        // Hook up the left arrow button
        transform.Find("Previous").GetComponent<Button>().onClick.AddListener(OnLeftClicked);
        // Hook up the right arrow button
        transform.Find("Next").GetComponent<Button>().onClick.AddListener(OnRightClicked);
    }

    // Lets other scripts read or set the index directly, keeping the label in sync
    public int indexValue
    {
        get 
        {

            // Just return the current index
            return index;
        
        }

        set 
        {
            // Update the index and refresh the label to match
            index = value;
            text.text = data[index];
        
        }
    
    }

    // Just the display string at the current index
    public string selectedValue 
    {
        get 
        {

            // Look up the current index in the data list
            return data[index];
        }
    
    }

    void OnLeftClicked() 
    {

        // Loop to the last option instead of stopping dead at 0
        if (index == 0)
        {
            index = data.Count - 1;

        }

        else 
        {
            // Otherwise just step back one
            index--;      
        }

        // Update the label to match the new index
        text.text = data[index];
        // Tell FontManager about the change, ?. so this doesn't break if it isn't loaded yet
        FontManager.Instance?.SetFont(index);
    }

    void OnRightClicked()
    {
        // Loop back to the first option once we hit the end
        if (index + 1 >= data.Count)
        {
            index = 0;

        }

        else
        {
            // Otherwise just step forward one
            index++;
        }

        // Update the label to match the new index
        text.text = data[index];
        // Tell FontManager about the change
        FontManager.Instance?.SetFont(index);

    }

    // Same as indexValue's getter, just as a method - leftover/duplicate but harmless
    public int getIndexValue() 
    {

        // Just return the current index
        return index;
    
    }

}