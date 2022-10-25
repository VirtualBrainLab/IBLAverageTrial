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

    [SerializeField] private Transform brainModelT;
    [SerializeField] private Transform brainAreasT;

    public float BrainScale { get { return _brainScale; } }

    private Dictionary<Renderer, Material> rendererDict;

    private void Awake()
    {
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
    }

    public void RecoverMaterials()
    {
        _materialsTransparent = false;
        foreach (Renderer renderer in rendererDict.Keys)
            renderer.enabled = true;
            //renderer.material = rendererDict[renderer];

        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            _useTransparentMaterials = !_useTransparentMaterials;

        if (Input.GetKeyDown(KeyCode.Equals))
            _brainScale++;

        if (Input.GetKeyDown(KeyCode.Minus))
            _brainScale--;

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



        // If the distance between the player and the brain is < some value, blank out the mouse, then expand the brain slowly
    }
}
