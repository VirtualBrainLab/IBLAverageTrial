using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms; // used to access Translation
using Unity.Rendering; // used to access RenderMesh/Bounds
using Unity.Collections;
using Unity.Mathematics;

/**
 * NeuronEntityManager is the MonoBehaviour class that handles all Neuron Entity interaction
 *
 * It interfaces with the NeuronEntitySystem which handles spiking and rendering
 * It also interfaces with the various Load() functions
 *
 * The main input call to this function is AddNeurons() which handles creating and adding new entities to
 * the ECS system
 */
public class NeuronEntityManager : MonoBehaviour
{
    // Manager access
    EntityManager eManager;
    [SerializeField] private CCFModelControl ccfmodelcontrol;
    [SerializeField] private VolumeDatasetManager vdmanager;

    // Expose mesh and materials
    [SerializeField] private GameObject neuronRoot;
    [SerializeField] private Mesh neuronMesh;
    [SerializeField] private Material neuronMaterial;
    [SerializeField] private Material neuronDataMaterial;
    [SerializeField] private float replayScale = 0.125f;
    [SerializeField] private float neuronScale = 0.015f;
    [SerializeField] private Utils util;

    private CCFAnnotationDataset annotationDataset;

    private static float3 nRootPos = new float3(5.7f, 4f, -6.6f);

    // Local tracking
    private float _currentNeuronScale;
    private bool _useScaleForSpiking;

    List<Entity> neurons;
    private float _currentMaxFiringRate;

    // Rendering
    private RenderMesh _neuronRenderMesh;
    private RenderMesh _neuronDataRenderMesh;
    private float4 neuronDefaultColor = new float4(0.15f, 0.63f, 0f, 0.4f);

    // Tracking
    // set mVN to limit the number of neurons on-screen. Neurons will be only removed if they are not visible
    // i.e. their probeDistanceComponent alpha is set to 0
    //[SerializeField] private int maxVisibleNeurons = 1000;

    void Awake()
    {
        neurons = new List<Entity>();

        eManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _neuronRenderMesh = new RenderMesh
        {
            castShadows = UnityEngine.Rendering.ShadowCastingMode.Off,
            mesh = neuronMesh,
            material = neuronMaterial,
            layer = 11,
            layerMask = 11
        };

        _neuronDataRenderMesh = new RenderMesh
        {
            castShadows = UnityEngine.Rendering.ShadowCastingMode.Off,
            mesh = neuronMesh,
            material = neuronDataMaterial,
            layer = 11,
            layerMask = 11
        };
    }

    private void Start()
    {
        _currentMaxFiringRate = 100f;
    }

    public bool UseScaleForSpiking()
    {
        return _useScaleForSpiking;
    }

    public float GetMaxFiringRate()
    {
        return _currentMaxFiringRate;
    }

    public float GetNeuronScale()
    {
        return _currentNeuronScale;
    }

    /// <summary>
    /// Basic function to create neurons and add all the required components for debug testing
    /// </summary>
    /// <param name="mlapdv"></param>
    /// <returns></returns>
    public List<Entity> AddNeurons(List<float3> mlapdv)
    {
        EntityArchetype neuronArchetype = eManager.CreateArchetype(
            typeof(Translation),
            typeof(Scale),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(MaterialColor),
            typeof(SpikingComponent),
            typeof(SpikingColorComponent),
            typeof(SpikingRandomComponent)
            );

        NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, mlapdv.Count, Allocator.Temp);
        List<Entity> returnList = new List<Entity>();

        _currentNeuronScale = neuronScale;

        for (int i = 0; i < mlapdv.Count; i++)
        {
            Entity neuron = newNeurons[i];
            eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
            eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
            eManager.SetComponentData(neuron, new MaterialColor { Value = neuronDefaultColor });
            //eManager.SetComponentData(neuron, new RenderBounds { Value = boundEdges });
            eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

            // Add the spiking component
            eManager.SetComponentData(neuron, new SpikingComponent { spiking = 0f });
            eManager.SetComponentData(neuron, new SpikingColorComponent { color = neuronDefaultColor });
            eManager.SetComponentData(neuron, NewSpikingRandomComponent());

            neurons.Add(neuron);
            returnList.Add(neuron);
        }

        newNeurons.Dispose();
        return returnList;
    }

    /**
     * Add neurons while keeping track of their mlapdv coordinates, option to set neuron color based on their mlapdv coordinate
     */
    //public List<Entity> AddNeurons(List<float3> mlapdvCoords, List<int3> apdvlr)
    //{
    //    EntityArchetype neuronArchetype = eManager.CreateArchetype(
    //        typeof(Translation),
    //        typeof(Scale),
    //        typeof(LocalToWorld),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(MaterialColor),
    //        typeof(SpikingComponent),
    //        typeof(SpikingColorComponent),
    //        typeof(SpikingRandomComponent),
    //        typeof(SimulatedNeuronComponent)
    //        );

    //    NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, mlapdvCoords.Count, Allocator.Temp);
    //    List<Entity> returnList = new List<Entity>();

    //    _currentNeuronScale = neuronScale;

    //    for (int i = 0; i < mlapdvCoords.Count; i++)
    //    {
    //        // Calculate what area we are in and get the color
    //        float annotation = vdmanager.GetAnnotationDataset().ValueAtIndex(apdvlr[i].x, apdvlr[i].y, apdvlr[i].z);
    //        Color neuronColor = ccfmodelcontrol.GetCCFAreaColorMinDepth((int)annotation);
    //        //Debug.Log("Neuron at " + apdvlr[i] + "in: " + annotation + " with color " + neuronColor);
    //        float4 color = new float4(neuronColor.r, neuronColor.g, neuronColor.b, 0.4f);

    //        Entity neuron = newNeurons[i];
    //        eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdvCoords[i]) });
    //        eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
    //        eManager.SetComponentData(neuron, new MaterialColor { Value = color });
    //        eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

    //        // Add the spiking component
    //        eManager.SetComponentData(neuron, new SpikingComponent { spiking = 0f });
    //        eManager.SetComponentData(neuron, new SpikingColorComponent { color = color });
    //        eManager.SetComponentData(neuron, NewSpikingRandomComponent());

    //        // Track the coordinates
    //        eManager.SetComponentData(neuron, new SimulatedNeuronComponent { apdvlr = mlapdvCoords[i] });

    //        neurons.Add(neuron);
    //        returnList.Add(neuron);
    //    }

    //    newNeurons.Dispose();
    //    return returnList;
    //}

    /**
     * Add neurons with receptive field data
     */
    //public List<Entity> AddNeurons(List<float3> mlapdvCoords, List<int3> apdvlr, List<float3> rfData)
    //{
    //    EntityArchetype neuronArchetype = eManager.CreateArchetype(
    //        typeof(Translation),
    //        typeof(Scale),
    //        typeof(LocalToWorld),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(MaterialColor),
    //        typeof(SpikingComponent),
    //        typeof(SpikingColorComponent),
    //        typeof(SpikingRandomComponent),
    //        typeof(SimulatedNeuronComponent),
    //        typeof(SimRFComponent)
    //        );

    //    NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, mlapdvCoords.Count, Allocator.Temp);
    //    List<Entity> returnList = new List<Entity>();

    //    _currentNeuronScale = neuronScale;

    //    for (int i = 0; i < mlapdvCoords.Count; i++)
    //    {
    //        // Calculate what area we are in and get the color
    //        float annotation = vdmanager.GetAnnotationDataset().ValueAtIndex(apdvlr[i].x, apdvlr[i].y, apdvlr[i].z);
    //        Color neuronColor = ccfmodelcontrol.GetCCFAreaColorMinDepth((int)annotation);
    //        //Debug.Log("Neuron at " + apdvlr[i] + "in: " + annotation + " with color " + neuronColor);
    //        float4 color = new float4(neuronColor.r, neuronColor.g, neuronColor.b, 0.4f);

    //        Entity neuron = newNeurons[i];
    //        eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdvCoords[i]) });
    //        eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
    //        eManager.SetComponentData(neuron, new MaterialColor { Value = color });
    //        eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

    //        // Add the RF data
    //        eManager.SetComponentData(neuron, new SimRFComponent { rf_x = rfData[i].x, rf_y = rfData[i].y, rf_sigma = rfData[i].z });

    //        // Add the spiking component
    //        eManager.SetComponentData(neuron, new SpikingComponent { spiking = 0f });
    //        eManager.SetComponentData(neuron, new SpikingColorComponent { color = color });
    //        eManager.SetComponentData(neuron, NewSpikingRandomComponent());

    //        // Track the coordinates
    //        eManager.SetComponentData(neuron, new SimulatedNeuronComponent { apdvlr = mlapdvCoords[i] });

    //        neurons.Add(neuron);
    //        returnList.Add(neuron);
    //    }

    //    newNeurons.Dispose();
    //    return returnList;
    //}

    //public List<Entity> AddNeurons(List<float3> mlapdv, List<IBLGLMComponent> data)
    //{
    //    EntityArchetype neuronArchetype = eManager.CreateArchetype(
    //        typeof(IBLGLMComponent),
    //        typeof(Translation),
    //        typeof(Scale),
    //        typeof(LocalToWorld),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(MaterialColor),
    //        typeof(SpikingComponent),
    //        typeof(SpikingColorComponent),
    //        typeof(SpikingRandomComponent)
    //        );

    //    NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, mlapdv.Count, Allocator.Temp);
    //    List<Entity> returnList = new List<Entity>();

    //    for (int i = 0; i < mlapdv.Count; i++)
    //    {
    //        Entity neuron = newNeurons[i];
    //        eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
    //        eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
    //        eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

    //        // Add the spiking component
    //        eManager.SetComponentData(neuron, new SpikingComponent { spiking = 0f });
    //        eManager.SetComponentData(neuron, new SpikingColorComponent { color = neuronDefaultColor });
    //        eManager.SetComponentData(neuron, NewSpikingRandomComponent());

    //        // Add the NeuronDataComponent
    //        eManager.SetComponentData(neuron, data[i]);

    //        neurons.Add(neuron);
    //        returnList.Add(neuron);
    //    }

    //    newNeurons.Dispose();
    //    return returnList;
    //}

    //public List<Entity> AddNeurons(List<float3> mlapdv, List<Color> data)
    //{
    //    EntityArchetype neuronArchetype = eManager.CreateArchetype(
    //        typeof(Translation),
    //        typeof(Scale),
    //        typeof(LocalToWorld),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(MaterialColor),
    //        typeof(SpikingComponent),
    //        typeof(SpikingColorComponent)
    //        );

    //    NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, mlapdv.Count, Allocator.Temp);
    //    List<Entity> returnList = new List<Entity>();

    //    for (int i = 0; i < mlapdv.Count; i++)
    //    {
    //        Entity neuron = newNeurons[i];
    //        float4 color = util.Color2float4(data[i]);

    //        // Add the required position and render components
    //        eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
    //        eManager.SetComponentData(neuron, new Scale { Value = replayScale });
    //        eManager.SetComponentData(neuron, new MaterialColor { Value = color });
    //        eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

    //        // Add the spiking component
    //        eManager.SetComponentData(neuron, new SpikingComponent { spiking = 0f });
    //        eManager.SetComponentData(neuron, new SpikingColorComponent { color = color });

    //        neurons.Add(neuron);
    //        returnList.Add(neuron);
    //    }

    //    newNeurons.Dispose();
    //    return returnList;
    //}

    //public List<Entity> AddNeurons(List<float3> mlapdv, List<Color> data, List<int> labData)
    //{
    //    EntityArchetype neuronArchetype = eManager.CreateArchetype(
    //        typeof(Translation),
    //        typeof(Scale),
    //        typeof(LocalToWorld),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(MaterialColor),
    //        typeof(PAComponent)
    //        );

    //    NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, mlapdv.Count, Allocator.Temp);
    //    List<Entity> returnList = new List<Entity>();

    //    for (int i = 0; i < mlapdv.Count; i++)
    //    {
    //        Entity neuron = newNeurons[i];
    //        // Add the required position and render components
    //        eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
    //        eManager.SetComponentData(neuron, new Scale { Value = replayScale });
    //        eManager.SetComponentData(neuron, new MaterialColor { Value = util.Color2float4(data[i]) });
    //        eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

    //        // Add the ProbeAnimation component (keeps track of the lab)
    //        eManager.SetComponentData(neuron, new PAComponent { lab = labData[i] });

    //        neurons.Add(neuron);
    //        returnList.Add(neuron);
    //    }

    //    newNeurons.Dispose();
    //    return returnList;
    //}

    public List<Entity> AddNeurons(List<float3> mlapdv, List<IBLEventAverageComponent> eventAverage, List<float4> colors)
        {
            EntityArchetype neuronArchetype = eManager.CreateArchetype(
                typeof(Translation),
                typeof(Scale),
                typeof(LocalToWorld),
                typeof(RenderMesh),
                typeof(RenderBounds),
                typeof(MaterialColor),
                typeof(IBLEventAverageComponent),
                typeof(PositionComponent)
                );

            int n = mlapdv.Count;

            NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, n, Allocator.Temp);
            List<Entity> returnList = new List<Entity>();

            for (int i = 0; i < n; i++)
            {
                Entity neuron = newNeurons[i];
                eManager.SetComponentData(neuron, new PositionComponent { position = new float3(mlapdv[i]) });
                eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
                eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
                eManager.SetComponentData(neuron, new MaterialColor { Value = colors[i] });
                eManager.SetSharedComponentData(neuron, _neuronRenderMesh);

                eManager.SetComponentData(neuron, eventAverage[i]);

                neurons.Add(neuron);
                returnList.Add(neuron);
            }

            newNeurons.Dispose();
            return returnList;
        }

    public List<Entity> AddNeurons(List<float3> mlapdv, List<IBLEventAverageComponent> eventAverage, float4[] zeroColorVals, float4[] maxColorVals)
    {
        EntityArchetype neuronArchetype = eManager.CreateArchetype(
            typeof(Translation),
            typeof(Scale),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(MaterialColor),
            typeof(LerpColorComponent),
            typeof(IBLEventAverageComponent),
            typeof(PositionComponent)
            );

        int n = mlapdv.Count;

        NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, n, Allocator.Temp);
        List<Entity> returnList = new List<Entity>();

        for (int i = 0; i < n; i++)
        {
            Entity neuron = newNeurons[i];
            eManager.SetComponentData(neuron, new PositionComponent { position = new float3(mlapdv[i]) });
            eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
            eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
            eManager.SetSharedComponentData(neuron, _neuronRenderMesh);
            eManager.SetComponentData(neuron, new MaterialColor { Value = maxColorVals[i] });
            eManager.SetComponentData(neuron, eventAverage[i]);
            eManager.SetComponentData(neuron, new LerpColorComponent { zeroColor = zeroColorVals[i], maxColor = maxColorVals[i]} );

            neurons.Add(neuron);
            returnList.Add(neuron);
        }

        newNeurons.Dispose();
        return returnList;
    }

    //public List<Entity> AddNeurons(List<float3> mlapdv, float4[] colors, float[] uCoord)
    //{
    //    Debug.Log(string.Format("Creating {0} neurons",mlapdv.Count));

    //    EntityArchetype neuronArchetype = eManager.CreateArchetype(
    //        typeof(Translation),
    //        typeof(Scale),
    //        typeof(LocalToWorld),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(MaterialColor),
    //        typeof(MaterialUCoord),
    //        typeof(MaterialVCoord),
    //        typeof(PositionComponent),
    //        typeof(MaterialBrainScale)
    //        );

    //    int n = mlapdv.Count;

    //    NativeArray<Entity> newNeurons = eManager.CreateEntity(neuronArchetype, n, Allocator.Temp);
    //    List<Entity> returnList = new List<Entity>();

    //    for (int i = 0; i < n; i++)
    //    {
    //        Entity neuron = newNeurons[i];
    //        eManager.SetComponentData(neuron, new PositionComponent { position = new float3(mlapdv[i]) });
    //        eManager.SetComponentData(neuron, new Translation { Value = CCF2Transform(mlapdv[i]) });
    //        eManager.SetComponentData(neuron, new Scale { Value = neuronScale });
    //        eManager.SetSharedComponentData(neuron, _neuronDataRenderMesh);
    //        eManager.SetComponentData(neuron, new MaterialColor { Value = colors[i] });
    //        eManager.SetComponentData(neuron, new MaterialUCoord { Value = uCoord[i] });
    //        eManager.SetComponentData(neuron, new MaterialVCoord { Value = 0f });
    //        eManager.SetComponentData(neuron, new MaterialBrainScale { Value = 1f });

    //        neurons.Add(neuron);
    //        returnList.Add(neuron);
    //    }
    //    Debug.Log("Created neurons");

    //    newNeurons.Dispose();
    //    return returnList;
    //}

    private SpikingRandomComponent NewSpikingRandomComponent()
    {
        return new SpikingRandomComponent { rand = Unity.Mathematics.Random.CreateFromIndex((uint)neurons.Count) };
    }

    void RemoveNeuron(Entity neuron)
    {
        eManager.DestroyEntity(neuron);
        neurons.Remove(neuron);
    }
    void RemoveNeurons(List<Entity> removeNeurons)
    {
        foreach (Entity neuron in removeNeurons)
        {
            RemoveNeuron(neuron);
        }
    }
    void RemoveNeurons(EntityQuery neuronQuery)
    {
        eManager.DestroyEntity(neuronQuery);
        foreach(Entity entity in neuronQuery.ToEntityArray(Allocator.Temp))
        {
            neurons.Remove(entity);
        }
    }

    public void RemoveAllNeurons()
    {
        eManager.DestroyAndResetAllEntities();
        neurons = new List<Entity>();
    }

    public void SetNeuronSpiking(Entity entity, SpikingComponent spikeComp, Scale scale)
    {
        eManager.SetComponentData(entity, spikeComp);
        eManager.SetComponentData(entity, scale);
    }

    // ** HELPER FUNCTIONS ** //

    // Note that
    // X axis = -ML
    // Y axis = -DV
    // Z axis = AP
    public static float3 CCF2Transform(float3 mlapdv)
    {
        return new float3(-mlapdv.x, -mlapdv.z, mlapdv.y) + nRootPos;
    }
    public static float3 CCF2Transform(Vector3 mlapdv)
    {
        return new float3(-mlapdv.x, -mlapdv.z, mlapdv.y) + nRootPos;
    }
}
