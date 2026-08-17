using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonAnimation : MonoBehaviour
{

    // Called when the button is clicked, plays the click animation
    public void buttonClicked() 
    {

        // Plays the "ButtonAnimation" clip on this object's Animation component
        GetComponent<Animation>().Play("ButtonAnimation");

    }

}