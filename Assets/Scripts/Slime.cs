using System;
using System.Collections.Generic;
using UnityEngine;

public class Slime : MonoBehaviour
{
    [Header("Shader")]
    public ComputeShader shader;

    [Header("Camera Input")]
    public CameraSetup cameraSetup;

    /*[Dropdown("cameras")]
    public String selected_camera;
    public Boolean cameraTest;
    public Boolean cameraHorizontalFlip;
    public Boolean cameraVerticalFlip;

    public Vector2 topLeft;
    public Vector2 topRight;
    public Vector2 lowerLeft;
    public Vector2 lowerRight;
    public Vector2 uv;

    public float lowSmooth;
    public float highSmooth;*/
    public float particleThreshold;

    [Header("Buffer")]
    public ComputeBuffer specie_buffer;
    public ComputeBuffer particle_buffer;

    [Header("Textures")]
    public WebCamTexture webcam_texture;

    public Texture2D input_intermediary;
    public Texture2D input_intermediary_pre;

    public RenderTexture trail_texture;
    public RenderTexture trail_texture_diffuse;

    public RenderTexture input_texture;
    public RenderTexture input_texture_pre;

    [Header("Colony Sets")]
    public ColonySet[] colonies;
    public Colony currentColony;
    public int currentColonyIndex = 0;
    public int nextColonyIndex = 0;

    private int max_particles = 1024 * 512;
    private Particle[] particles;

    ///////////////////////////////////////////////////////////////////////////
    protected virtual void Start()
    {
        // Application.targetFrameRate = 30;

        InitCamera();
        InitParticles();
        InitColonies();

        UpdateBuffers();
        UpdateTextures();
    }

    ///////////////////////////////////////////////////////////////////////////
    // Event Handler
    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////////////////////////////////////
    private void OnDestroy()
    {
        ReleaseBuffers();
        ReleaseTextures();
        ReleaseCamera();
    }

    ///////////////////////////////////////////////////////////////////////////
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        currentColony = GetColonyState();
        if (currentColony == null) { return; }

        UpdateCamera();
        UpdateTextures();
        UpdateBuffers();
        UpdateShader();

        // Blit the result texture to the screen
        Graphics.Blit(trail_texture_diffuse, destination);
        Graphics.Blit(trail_texture_diffuse, trail_texture);
    }

    ////////////////////////////////////////////////////////////////
    // Camera Input
    ////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////
    public void InitCamera()
    {
        cameraSetup.InitCameras();
        SwitchCamera();
    }

    ////////////////////////////////////////////////////////////////
    public void UpdateCamera()
    {
        if (webcam_texture.deviceName != cameraSetup.selectedCamera) { SwitchCamera(); }
        if (!webcam_texture.didUpdateThisFrame) { return; }

        // Debug.Log("Camera: " + Time.fixedTime);

        float vertical = cameraSetup.cameraVerticalFlip ? -1.0f : 1.0f;
        float horizontal = cameraSetup.cameraHorizontalFlip ? -1.0f : 1.0f;
        Vector2 origin = new Vector2(vertical, horizontal);
        Vector2 target = new Vector2(.0f, 1.0f);

        Graphics.CopyTexture(input_intermediary, input_intermediary_pre);
        Graphics.CopyTexture(webcam_texture, input_intermediary);
        Graphics.Blit(input_intermediary, input_texture, origin, target);
        Graphics.Blit(input_intermediary_pre, input_texture_pre, origin, target);
    }

    ///////////////////////////////////////////////////////////////////////////
    public void SwitchCamera()
    {
        Debug.Log("Cam Switch: " + cameraSetup.selectedCamera);

        webcam_texture = new WebCamTexture();
        webcam_texture.deviceName = cameraSetup.selectedCamera;
        webcam_texture.Play();

        UpdateCameraTextures();
    }

    ///////////////////////////////////////////////////////////////////////////
    public void UpdateCameraTextures()
    {
        // Debug.Log("Cam Texture: " + Time.fixedTime);

        TextureFormat format = TextureFormat.RGBA32;
        int width = webcam_texture.width;
        int height = webcam_texture.height;

        input_intermediary = new Texture2D(width, height, format, false);
        input_intermediary_pre = new Texture2D(width, height, format, false);
    }

    ///////////////////////////////////////////////////////////////////////////
    public void ReleaseCamera()
    {
        if (webcam_texture == null) return;
        webcam_texture.Stop();
    }   

    ///////////////////////////////////////////////////////////////////////////
    // Textures
    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////////////////////////////////////
    private void UpdateTextures()
    {
        UpdateOrCreateTexture(ref trail_texture, "trail", new[] { 0, 1 });
        UpdateOrCreateTexture(ref trail_texture_diffuse, "trail_diffuse", new[] { 1 });

        UpdateOrCreateTexture(ref input_texture, "input", new[] { 0, 1 });
        UpdateOrCreateTexture(ref input_texture_pre, "input_pre", new[] { 1 });

        // Debug.Log("Texture: " + Time.fixedTime);
    }

    ////////////////////////////////////////////////////////////////
    private void UpdateOrCreateTexture(ref RenderTexture texture, String name, int[] kernel_ids)
    {
        if (texture != null)
        {
            if (texture.width == Screen.width &
                texture.height == Screen.height) { return; }
            texture.Release();
        }

        RenderTextureFormat color = RenderTextureFormat.ARGBFloat;
        RenderTextureReadWrite rw = RenderTextureReadWrite.Linear;
        texture = new RenderTexture(Screen.width, Screen.height, 0, color, rw);
        texture.enableRandomWrite = true;
        texture.Create();

        foreach (int kernel_id in kernel_ids)
            shader.SetTexture(kernel_id, name, texture);
    }

    ///////////////////////////////////////////////////////////////////////////
    private void ReleaseTextures()
    {
        trail_texture.Release();
        trail_texture_diffuse.Release();

        input_texture.Release();
        input_texture_pre.Release();
    }

    ///////////////////////////////////////////////////////////////////////////
    // Buffers
    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////////////////////////////////////
    private void UpdateBuffers()
    {
        CreateOrUpdateBuffer<Particle>(ref particle_buffer, particles, "particles", new[] { 0, 1 });

        if(currentColony == null) { return; }
        CreateOrUpdateBuffer<Specie>(ref specie_buffer, currentColony.species, "species", new[] { 0, 1 });
        specie_buffer.SetData(currentColony.species);
    }

    ///////////////////////////////////////////////////////////////////////////
    private void CreateOrUpdateBuffer<T>(ref ComputeBuffer buffer, T[] data, string name, int[] kernelIndices)
    {
        int count = data.Length;
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        bool createNewBuffer =
            buffer == null ||
            !buffer.IsValid() ||
            buffer.count != count ||
            buffer.stride != stride;

        if (!createNewBuffer) return;
        buffer?.Release();
        buffer = new ComputeBuffer(count, stride);
        buffer.SetData(data);

        foreach (int index in kernelIndices)  
            shader.SetBuffer(index, name, buffer);
    }

    ///////////////////////////////////////////////////////////////////////////
    private void ReleaseBuffers()
    {
        specie_buffer.Release();
        particle_buffer.Release();
    }

    ///////////////////////////////////////////////////////////////////////////
    // Shader
    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////////////////////////////////////   
    private void UpdateShader()
    {
        int numParticles = currentColony.numParticle;
        // int numParticles = 25000;

        shader.SetInt("width", Screen.width);
        shader.SetInt("height", Screen.height);
        shader.SetInt("num_species", currentColony.species.Length);
        shader.SetInt("num_particles", numParticles);

        shader.SetFloat("time", Time.fixedTime);
        shader.SetFloat("delta_time", Time.fixedDeltaTime);

        shader.SetFloat("offset", currentColony.offset);
        shader.SetFloat("decay_red", currentColony.decayRed);
        shader.SetFloat("decay_green", currentColony.decayGreen);
        shader.SetFloat("decay_blue", currentColony.decayBlue);
        shader.SetFloat("decay_alpha", currentColony.decayAlpha);
        shader.SetFloat("diffuse_radius", currentColony.diffuseRadius);

        int diffuseDiameter = (int)currentColony.diffuseRadius * 2 + 1;
        int blurSum = diffuseDiameter * diffuseDiameter;

        shader.SetInt("blur_sum", blurSum);
        shader.SetBool("camera_test", cameraSetup.cameraTest);
        shader.SetVector("top_left", cameraSetup.topLeft);
        shader.SetVector("top_right", cameraSetup.topRight);
        shader.SetVector("lower_left", cameraSetup.lowerLeft);
        shader.SetVector("lower_right", cameraSetup.lowerRight);
     
        shader.SetFloat("low_smooth", cameraSetup.lowSmooth);
        shader.SetFloat("high_smooth", cameraSetup.highSmooth);

        shader.SetVector("color_boost", cameraSetup.colorBoost);

        shader.SetVector("motion_color", currentColony.motionColor);
        shader.SetFloat("creation_threshold", particleThreshold);

        // Dispatch the update kernel of the compute shader
        specie_buffer.SetData(currentColony.species);

        shader.GetKernelThreadGroupSizes(0, out uint x, out _, out uint _);
        int groups = Math.Max(1, Mathf.CeilToInt(numParticles / (float)x));
        shader.Dispatch(0, groups, 1, 1);

        // Dispatch the draw kernel of the compute shader
        shader.GetKernelThreadGroupSizes(1, out uint x_1, out uint y_1, out uint _);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / x_1);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / y_1);
        shader.Dispatch(1, threadGroupsX, threadGroupsY, 1);
    }

    ///////////////////////////////////////////////////////////////////////////
    // Slime Stuff
    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////////////////////////////////////
    private void InitParticles()
    {
        // Create agents with initial positions and angles
        particles = new Particle[max_particles];
        for (int i = 0; i < particles.Length; i++)
        {
            int x = UnityEngine.Random.Range(0, Screen.width);
            int y = UnityEngine.Random.Range(0, Screen.height);

            float d_x = UnityEngine.Random.Range(-10.0f, 10.0f);
            float d_y = UnityEngine.Random.Range(-10.0f, 10.0f);

            Vector4 clr = new(1.0f, 1.0f, 1.0f, 1.0f);
            Vector2 speed = new(d_x, d_y);
            Vector2 pos = new(x, y);

            particles[i] = new Particle()
            {
                pos = pos,
                speed = speed,
                color = clr,
                specie = 0,
                index = i,
                age = 0.0f
            };
        }
    }

    ///////////////////////////////////////////////////////////////////////////
    private void InitColonies()
    {
        int range = ColonyRange();
        currentColonyIndex = UnityEngine.Random.Range(0, range);
        nextColonyIndex = UnityEngine.Random.Range(0, range);
    }

    ///////////////////////////////////////////////////////////////////////////
    private int ColonyRange()
    {
        int range = Mathf.Max(0, colonies.Length);
        return range;
    }

    ///////////////////////////////////////////////////////////////////////////
    private void IncrementColonyIndex()
    {
        int range = ColonyRange();
        colonies[currentColonyIndex].Reset();

        currentColonyIndex = nextColonyIndex;
        colonies[currentColonyIndex].Reset();

        nextColonyIndex = UnityEngine.Random.Range(0, range);
        colonies[nextColonyIndex].Reset();
    }

    ///////////////////////////////////////////////////////////////////////////
    private Colony GetColonyState()
    {
        if (colonies.Length == 0) { return null; }

        ColonySet current = colonies[currentColonyIndex];
        ColonySet next = colonies[nextColonyIndex];

        current.Tick(Time.deltaTime);
        Colony colony = current.CurrentColonyInterpolation(ref next);
        if (current.currentCycle > 0) { IncrementColonyIndex(); }

        return colony;
    }
}
