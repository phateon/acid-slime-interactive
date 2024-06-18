using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu()]
public class CameraSetup : ScriptableObject
{
    [Dropdown("cameras")]
    public String selectedCamera;
    public String preferedCamera;

    public Boolean cameraTest;
    public Boolean cameraHorizontalFlip;
    public Boolean cameraVerticalFlip;

    public Vector2 topLeft;
    public Vector2 topRight;
    public Vector2 lowerLeft;
    public Vector2 lowerRight;

    public float lowSmooth;
    public float highSmooth;

    public Color colorBoost = Color.black;

    protected List<string> cameras = new List<string>();
    public List<string> Cameras { get; private set; }

    public void InitCameras()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log(devices[i].name);
            cameras.Add(devices[i].name);
        }

        if (cameras.Contains(preferedCamera)) { selectedCamera = preferedCamera; }
        else { selectedCamera = cameras[0]; }
    }
}
