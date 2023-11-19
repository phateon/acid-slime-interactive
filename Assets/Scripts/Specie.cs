using System.Linq;
using System.Reflection;
using UnityEngine;
using System;

[Serializable]
public struct Specie
{
    [Header("Shape")]
    public float size;

    [Header("Color")]
    public float red;
    public float green;
    public float blue;
    public float alpha;

    [Header("Direction Settings")]
    public float continuation;
    public float randomness;
    public float attraction;
    public float proximity;
    public float turnSpeed;

    [Header("Environment Settings")]
    public float damping;
    public float gravity;
    public float wind;

    [Header("Sensor Settings")]
    public float sensorAngleSpacing;
    public float sensorOffsetDst;

    [Header("Emitter Settings")]
    public float initialSpeed;
    public float effectDuration;
    public float spawnProbability;
    public float lifespan;

    public Specie Clone()
    {
        return (Specie)this.MemberwiseClone();
    }
    public float[] AsStruct()
    {
        float[] res = new float[0];
        Type type = typeof(Specie);
        FieldInfo[] properties = type.GetFields();
        foreach (FieldInfo property in properties)
        {
            float v = (float)property.GetValue(this);
            res.Append(v);
        }

        return res;
    }
    public Specie Interpolate(ref Specie other, float x)
    {
        Specie y = this.Clone();
        this.Interpolate(ref other, ref y, x);
        return y;
    }
    public Specie Interpolate(ref Specie other, ref Specie target, float x)
    {
        object obj = target;
        Type type = typeof(Specie);
        FieldInfo[] properties = type.GetFields();
        foreach (FieldInfo property in properties)
        {
            float from = (float)property.GetValue(this);
            float to = (float)property.GetValue(other);
            float n_value = Mathf.Lerp(from, to, x);
            property.SetValue(obj, n_value);
        }

        return (Specie) obj;
    }
}