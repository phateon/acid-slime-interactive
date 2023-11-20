using UnityEngine;
using System;

[CreateAssetMenu()]
public class Colony : ScriptableObject
{
    [Header("Simulation Settings")]
    public int offset = 10;
    public int numParticle = 50000;
    public Color motionColor = Color.red;

    [Header("Transition Settings")]
    public float duration = 10.0f;
    public float transition = 5.0f;

    [Header("Trail Settings")]
    public float decayRed = 0.25f;
    public float decayGreen = 0.25f;
    public float decayBlue = 0.25f;
    public float decayAlpha = 0.25f;
    public float diffuseRadius = 1.0f;

    [Header("Species")]
    public Specie[] species;

    public Colony Interpolate(Colony b, float x = 0.0f)
    {
        Colony left = this;
        Colony right = b;

        int xl = this.species.Length;
        int xr = b.species.Length;
        int mx = Math.Max(xl, xr);

        if(xl < xr)
        {
            left = b;
            right = this;
            xl = b.species.Length;
            xr = this.species.Length;
            x = 1.0f - x;
        }

        Colony response = (Colony) left.MemberwiseClone();

        response.offset = (int) Mathf.Lerp((float) left.offset, (float) right.offset, x);
        response.decayRed = Mathf.Lerp(left.decayRed, right.decayRed, x);
        response.decayGreen = Mathf.Lerp(left.decayGreen, right.decayGreen, x);
        response.decayBlue = Mathf.Lerp(left.decayBlue, right.decayBlue, x);
        response.decayAlpha = Mathf.Lerp(left.decayAlpha, right.decayAlpha, x);
        response.diffuseRadius = Mathf.Lerp(left.diffuseRadius, right.diffuseRadius, x);
        response.numParticle = (int) Mathf.Lerp((float) left.numParticle, (float) right.numParticle, x);

        response.motionColor.r = Mathf.Lerp(left.motionColor.r, right.motionColor.r, x);
        response.motionColor.g = Mathf.Lerp(left.motionColor.g, right.motionColor.g, x);
        response.motionColor.b = Mathf.Lerp(left.motionColor.b, right.motionColor.b, x);
        response.motionColor.a = Mathf.Lerp(left.motionColor.a, right.motionColor.a, x);

        Specie[] species = new Specie[mx];
        for (int i = 0; i < mx; i++)
        {
            int il = Math.Min(i, xl - 1);
            int ir = Math.Min(i, xr - 1);

            Specie from = left.species[il];
            Specie to = right.species[ir];
            species[il] = from.Interpolate(ref to, ref species[il], x);
        }

        response.species = species;
        return response;
    }
}
