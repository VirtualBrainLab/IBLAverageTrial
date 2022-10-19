using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventAverageManager : MonoBehaviour
{
    [SerializeField] private float _brainScale = 1f;
    [SerializeField] private bool _useTransparentMaterials = false;
    [SerializeField] private float _rigTransparency = 1f;

    [SerializeField] private Material transparentMaterial;
    private bool materialsTransparent;

    [SerializeField] private GameObject rig;
    [SerializeField] private GameObject mouse;

    [SerializeField] private Transform brain;

    public float BrainScale { get { return _brainScale; } }

    private Dictionary<Renderer, Material> rendererDict;

    private void Awake()
    {
        rendererDict = new Dictionary<Renderer, Material>();
        materialsTransparent = false;

        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>())
            if (!rendererDict.ContainsKey(renderer))
                rendererDict.Add(renderer, renderer.material);
        foreach (Renderer renderer in mouse.GetComponentsInChildren<Renderer>())
            if (!rendererDict.ContainsKey(renderer))
                rendererDict.Add(renderer, renderer.material);
    }

    public void ReplaceMaterials()
    {
        materialsTransparent = true;
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
        materialsTransparent = false;
        foreach (Renderer renderer in rendererDict.Keys)
            renderer.enabled = true;
            //renderer.material = rendererDict[renderer];

        
    }

    private void Update()
    {
        if (_useTransparentMaterials && !materialsTransparent)
            ReplaceMaterials();

        if (!_useTransparentMaterials && materialsTransparent)
            RecoverMaterials();

        if (materialsTransparent)
            foreach (Renderer renderer in rendererDict.Keys)
            {
                Color col = renderer.material.color;
                col.a = _rigTransparency;
                renderer.material.color = col;
            }

        brain.localScale = new Vector3(_brainScale, _brainScale, _brainScale);



        // If the distance between the player and the brain is < some value, blank out the mouse, then expand the brain slowly
    }
}
