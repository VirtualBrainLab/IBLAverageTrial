using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] private Slider indexSlider;

    [SerializeField] private UpdateTrialPosPanel trialPositionPanel;

    public bool standaloneMode = true;

    public float brainScale { get { return _brainScale; } }

    private Dictionary<Renderer, Material> rendererDict;

    [SerializeField] private Camera mainCamera;
    private IBLTask task;

    public bool forceUpdate;

    private void Awake()
    {
        _materialsTransparent = _useTransparentMaterials;
        rendererDict = new Dictionary<Renderer, Material>();

        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>())
            if (!rendererDict.ContainsKey(renderer))
                rendererDict.Add(renderer, renderer.material);
        foreach (Renderer renderer in mouse.GetComponentsInChildren<Renderer>())
            if (!rendererDict.ContainsKey(renderer))
                rendererDict.Add(renderer, renderer.material);

        //if (standaloneMode)
        neuronScaleMult = 1f;
        //else
        //    neuronScaleMult = 10f;
    }

    private void Start()
    {
        modelControl.LateStart(true);
        LateStart();

        task = _expManager.GetIBLTask();
        SetTrialType(0);
    }
    

    private async void LateStart()
    {
        await modelControl.GetDefaultLoadedTask();

        foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
            node.SetNodeModelVisibility_Full(true);

        brainAreasT.localScale = Vector3.one / 2f;
        _brainScale = 1f;
    }

    public void UpdateIndex(float indexPercentage)
    {
        //task.SetTimeIndex(Mathf.RoundToInt(indexSlider.value));
        task.SetTimeIndex(Mathf.RoundToInt(indexPercentage * (trialDatasetType ? 250 : 100)));
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
        brainModelT.localScale = new Vector3(_brainScale, _brainScale, _brainScale);

        //if (Input.GetKeyDown(KeyCode.T))
        //    _useTransparentMaterials = !_useTransparentMaterials;

        //if (Input.GetKeyDown(KeyCode.Equals))
        //    _brainScale++;

        //if (Input.GetKeyDown(KeyCode.Minus))
        //    _brainScale--;

        if (!standaloneMode)
        {
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


            if (Input.GetKeyDown(KeyCode.R))
                Launch();
        }
    }



    /// ANIMATION
    /// 

    private bool launched;

    public void Launch()
    {
        Debug.Log("Launched");
        if (!launched)
        {
            StartCoroutine(DropRig());
            launched = true;
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
            _brainScale = _brainScale * 1.0025f;
        }

        yield return new WaitForSecondsRealtime(20f);
        _expManager.DisableStimulus();

        while (Elapsed() < 160f)
        {
            if (Elapsed() > 100f)
                floor.SetActive(false);

            yield return new WaitForSecondsRealtime(0.01f);
            _brainScale = _brainScale * 1.0005f;
        }

        Debug.Log("turning down the lights");
        _expManager.Stop();
        nemanager.RemoveAllNeurons();
        rig.SetActive(false);
        mouse.SetActive(false);
        brain.SetActive(false);
    }

    private float Elapsed()
    {
        return Time.realtimeSinceStartup - startTime;
    }

    #region standalone features
    public int trialType { get; private set; }
    public void SetTrialType(int trialType)
    {
        this.trialType = trialType;
        UpdateTrialUI();
    }

    private void UpdateTrialUI()
    {
        if (trialPositionPanel == null)
            return;

        if (trialDatasetType)
        {
            trialPositionPanel.UpdateTextPositions(IBLTask.eventIdxsByType[trialType][0],
                IBLTask.eventIdxsByType[trialType][1],
                IBLTask.eventIdxsByType[trialType][2],
                250);
        }
        else
        {
            // this depends on what data is being displayed
            if (trialDatasetIndex == 1)
                trialPositionPanel.UpdateTextPositions(50,
                    -1,
                    -1,
                    100);
            else if (trialDatasetIndex == 2)
                trialPositionPanel.UpdateTextPositions(-1,
                    50,
                    -1,
                    100);
            else if (trialDatasetIndex == 3)
                trialPositionPanel.UpdateTextPositions(-1,
                    -1,
                    50,
                    100);
        }
        forceUpdate = true;
    }

    public bool trialDatasetType { get; private set; }

    public void SetDatasetType(bool newType)
    {
        trialDatasetType = newType;
    }

    public int trialDatasetIndex { get; private set; }

    public void SetDatasetIndex(int newIndex)
    {
        trialDatasetIndex = newIndex;
        UpdateTrialUI();
    }

    #endregion

    #region baselining and scaling

    public bool useBaseline { get; private set; }
    public void SetBaseline(bool baseline)
    {
        useBaseline = baseline;
        forceUpdate = true;
    }

    public float neuronScaleMult { get; private set; }

    public void SetNeuronScaleMult(float scale)
    {
        neuronScaleMult = scale;
        forceUpdate = true;
    }

    #endregion
}
