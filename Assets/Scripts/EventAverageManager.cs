using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventAverageManager : MonoBehaviour
{
    [SerializeField] private CCFModelControl modelControl;

    [SerializeField] private float _brainScale = 1f;
    [SerializeField] private bool _useTransparentMaterials = false;
    [SerializeField] private float _rigTransparency = 1f;

    [SerializeField] private Material transparentMaterial;
    private bool _materialsTransparent;
    public bool MaterialsTransparent { get { return _materialsTransparent; } }

    [SerializeField] private GameObject rig;
    [SerializeField] private GameObject mouse;
    [SerializeField] private GameObject brain;
    [SerializeField] private GameObject floor;

    [SerializeField] private Transform brainModelT;
    [SerializeField] private Transform brainAreasT;

    [SerializeField] private LayerMask transparentLayerMask;
    [SerializeField] private LayerMask visibleLayerMask;

    [SerializeField] private ExperimentManager _expManager;
    [SerializeField] private Animator _rigAnimator;
    [SerializeField] private NeuronEntityManager nemanager;

    public float BrainScale { get { return _brainScale; } }

    private Dictionary<Renderer, Material> rendererDict;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        rendererDict = new Dictionary<Renderer, Material>();
        _materialsTransparent = false;

        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>())
            if (!rendererDict.ContainsKey(renderer))
                rendererDict.Add(renderer, renderer.material);
        foreach (Renderer renderer in mouse.GetComponentsInChildren<Renderer>())
            if (!rendererDict.ContainsKey(renderer))
                rendererDict.Add(renderer, renderer.material);
    }

    private void Start()
    {
        modelControl.LateStart(true);
        LateStart();
    }

    

    private async void LateStart()
    {
        await modelControl.GetDefaultLoadedTask();

        foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
            node.SetNodeModelVisibility_Full(true);

        brainAreasT.localScale = Vector3.one / 2f;
    }

    public void ReplaceMaterials()
    {
        _materialsTransparent = true;
        foreach (Renderer renderer in rendererDict.Keys)
        {
            //Color color = renderer.material.color;
            //renderer.material = transparentMaterial;
            //renderer.material.color = color;
            renderer.enabled = false;
        }
        // make the camera see the brain, or not
        mainCamera.cullingMask = transparentLayerMask.value;
    }

    public void RecoverMaterials()
    {
        _materialsTransparent = false;
        foreach (Renderer renderer in rendererDict.Keys)
            renderer.enabled = true;
        //renderer.material = rendererDict[renderer];

        mainCamera.cullingMask = visibleLayerMask.value;
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.T))
        //    _useTransparentMaterials = !_useTransparentMaterials;

        //if (Input.GetKeyDown(KeyCode.Equals))
        //    _brainScale++;

        //if (Input.GetKeyDown(KeyCode.Minus))
        //    _brainScale--;

        if (_useTransparentMaterials && !_materialsTransparent)
            ReplaceMaterials();

        if (!_useTransparentMaterials && _materialsTransparent)
            RecoverMaterials();

        if (_materialsTransparent)
            foreach (Renderer renderer in rendererDict.Keys)
            {
                Color col = renderer.material.color;
                col.a = _rigTransparency;
                renderer.material.color = col;
            }

        brainModelT.localScale = new Vector3(_brainScale, _brainScale, _brainScale);


        if (Input.GetKeyDown(KeyCode.R))
            Launch();

        // If the distance between the player and the brain is < some value, blank out the mouse, then expand the brain slowly
    }



    /// ANIMATION
    /// 

    private bool loaded = false;

    public void DoneLoadingCallback()
    {
        Debug.Log("Done loading");
        loaded = true;
    }

    public void Launch()
    {
        if (loaded)
        {
            StartCoroutine(DropRig());
        }
    }

    private float startTime;

    private IEnumerator DropRig()
    {
        startTime = Time.realtimeSinceStartup;

        yield return null;
        Debug.Log("Waiting to start animation");
        yield return new WaitForSeconds(3f);

        //_rigAnimator.speed = 0.1f;
        _rigAnimator.SetTrigger("Drop");

        Debug.Log("Waiting to finish animation");
        yield return new WaitForSeconds(10f);

        Debug.Log("Starting experiment");
        Time.timeScale = 0.125f;
        _expManager.Play();

        yield return new WaitForSecondsRealtime(45f);
        _useTransparentMaterials = true;

        while (Elapsed() < 70f)
        {
            yield return new WaitForSecondsRealtime(0.01f);
            _brainScale = _brainScale * 1.003f;
        }

        yield return new WaitForSecondsRealtime(20f);
        _expManager.DisableStimulus();

        while (Elapsed() < 130f)
        {
            if (Elapsed() > 110f)
                floor.SetActive(false);

            yield return new WaitForSecondsRealtime(0.01f);
            _brainScale = _brainScale * 1.0025f;
        }

        yield return new WaitForSecondsRealtime(30f);

        Debug.Log("turning down the lights");
        _expManager.Stop();
        nemanager.RemoveAllNeurons();
        rig.SetActive(false);
        mouse.SetActive(false);
        brain.SetActive(false);
        floor.SetActive(false);
    }

    private float Elapsed()
    {
        return Time.realtimeSinceStartup - startTime;
    }
}
