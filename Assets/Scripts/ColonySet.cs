using System;
using UnityEngine;

[CreateAssetMenu()]
public class ColonySet : ScriptableObject
{
    public Colony[] colonies;
    public float fade = 0.0f;
    public float currentColonyTime = 0.0f;
    public string debug = "";

    public int nextColonyIndex = 0;
    public int currentColonyIndex = 0;
    public int currentCycle = 0;
  
    public void Reset() 
    {
        currentColonyIndex = 0;
        currentColonyTime = 0.0f;
        currentCycle = -1;
        UpdateNextColony();
    }

    public Colony GetFirstColony()
    { 
        return colonies[0];
    }

    public bool CurrentColonyFinished() 
    {
        float duration = colonies[currentColonyIndex].duration;
        float transition = colonies[currentColonyIndex].transition;
        return currentColonyTime >= (duration + transition);
    }

    private void UpdateNextColony()
    {
        if(currentColonyIndex == 0) { currentCycle += 1; }
        int nextIndex = currentColonyIndex + 1;

        if(nextIndex >= colonies.Length) { nextIndex = 0; }
        nextColonyIndex = nextIndex;
    }
 
    public void Tick(float deltaTime)
    {
        currentColonyTime += deltaTime;
        if (CurrentColonyFinished()) { Increment(); }
    }

    private void Increment()
    {
        currentColonyIndex = nextColonyIndex;
        currentColonyTime = 0.0f;
        UpdateNextColony();
    }

    public Colony CurrentColonyInterpolation(ref ColonySet nextSet) 
    {
        float duration = colonies[currentColonyIndex].duration;
        float transition = colonies[currentColonyIndex].transition;
        if (currentColonyTime < duration) { return colonies[currentColonyIndex]; }

        fade = (currentColonyTime - duration) / transition;
        if(fade > 1.0f) { fade = 1.0f; }

        Colony next = colonies[nextColonyIndex];
        if(nextSet != null & nextColonyIndex==0) { 
            next = nextSet.GetFirstColony(); }
        if(fade == 1.0f) { 
            return next; }
        
        Colony current = colonies[currentColonyIndex];
        Colony inter = current.Interpolate(next, fade);

        return inter;
    }
}

