using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LoadData_IBL_EventAverage : MonoBehaviour
{
    [SerializeField] private CCFModelControl ccfmodelcontrol;
    [SerializeField] private NeuronEntityManager nemanager;
    [SerializeField] private VolumeDatasetManager vdmanager;

    [SerializeField] private EventAverageManager _eventAverageManager;

    //[SerializeField] private AssetReference uuidListReference;
    //[SerializeField] private AssetReference dataReference;

    #region text assets
    [SerializeField] private TextAsset dataAssetRaw;
    [SerializeField] private TextAsset uuidListAssetRaw;
    [SerializeField] private TextAsset mlapdvAssetRaw;

    [SerializeField] private TextAsset dataAssetBN;
    [SerializeField] private TextAsset uuidListAssetBN;
    [SerializeField] private TextAsset mlapdvAssetBN;

    [SerializeField] private TextAsset dataAssetStimOn;
    [SerializeField] private TextAsset uuidListAssetStimOn;
    [SerializeField] private TextAsset mlapdvAssetStimOn;

    [SerializeField] private TextAsset dataAssetWheel;
    [SerializeField] private TextAsset uuidListAssetWheel;
    [SerializeField] private TextAsset mlapdvAssetWheel;

    [SerializeField] private TextAsset dataAssetFeedback;
    [SerializeField] private TextAsset uuidListAssetFeedback;
    [SerializeField] private TextAsset mlapdvAssetFeedback;
    #endregion

    (List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data) avgTrialCompRaw;
    (List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data) avgTrialCompBN;
    (List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data) avgTrialCompStimOn;
    (List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data) avgTrialCompWheel;
    (List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data) avgTrialCompFeedback;
    List<(List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data)> dataLists;

    public Utils util;

    CCFAnnotationDataset annotationDataset;
    //float scale = 1000;
    public const int SCALED_LEN_TRIAL = 250;
    public const int SCALED_LEN_PSTH = 100;
    int conditions = 4;
    int[] side = { -1, -1, 1, 1 };
    int[] corr = { 1, -1, 1, -1 };

    private float TIME_SCALE_FACTOR = 0.0625f;

    private float[] spikeRateMap;

    private void Awake()
    {
        avgTrialCompRaw.mlapdv = new List<float3>();
        avgTrialCompRaw.colors = new List<float4>();
        avgTrialCompRaw.data = new List<IBLEventAverageComponent>();
        avgTrialCompBN.mlapdv = new List<float3>();
        avgTrialCompBN.colors = new List<float4>();
        avgTrialCompBN.data = new List<IBLEventAverageComponent>();
        avgTrialCompStimOn.mlapdv = new List<float3>();
        avgTrialCompStimOn.colors = new List<float4>();
        avgTrialCompStimOn.data = new List<IBLEventAverageComponent>();
        avgTrialCompWheel.mlapdv = new List<float3>();
        avgTrialCompWheel.colors = new List<float4>();
        avgTrialCompWheel.data = new List<IBLEventAverageComponent>();
        avgTrialCompFeedback.mlapdv = new List<float3>();
        avgTrialCompFeedback.colors = new List<float4>();
        avgTrialCompFeedback.data = new List<IBLEventAverageComponent>();
        dataLists = new List<(List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data)>();
        dataLists.Add(avgTrialCompRaw);
        dataLists.Add(avgTrialCompBN);
        dataLists.Add(avgTrialCompStimOn);
        dataLists.Add(avgTrialCompWheel);
        dataLists.Add(avgTrialCompFeedback);
    }

    // Start is called before the first frame update
    async void Start()
    {
        //ParseIBLData_EventAverage();

        await vdmanager.LoadAnnotationDataset(new List<Action>());

        annotationDataset = vdmanager.GetAnnotationDataset();
        SwapNeuronData(1);
    }

    public void SwapNeuronData(int dataGroup)
    {
        _eventAverageManager.SetDatasetType(dataGroup <= 1);
        _eventAverageManager.SetDatasetIndex(dataGroup);

        if (dataLists[dataGroup].mlapdv.Count == 0)
        {
            // We need to load the data before we create neurons
            LoadData(dataGroup);
        }


        nemanager.RemoveAllNeurons();
        nemanager.AddNeurons(dataLists[dataGroup].mlapdv, dataLists[dataGroup].data, dataLists[dataGroup].colors);
    }

    private void LoadData(int dataGroup)
    {
        Debug.Log(string.Format("Data is loading for group {0}", dataGroup));

        // We just re-generated everything, so it's safe to assume that the uuid list is matched between data and mlapdv
        (TextAsset data, TextAsset uuidList, TextAsset mlapdv) textAssets = GetTextAssets(dataGroup);

        (List<float3> mlapdv, List<float4> colors, List<IBLEventAverageComponent> data) dataObject = dataLists[dataGroup];

        //Dictionary<string, IBLEventAverageComponent> eventAverageData = new Dictionary<string, IBLEventAverageComponent>();

        int SCALED_LEN = dataGroup <= 1 ? SCALED_LEN_TRIAL : SCALED_LEN_PSTH;

        //string uuidListFile = textAssets.uuidList.text;
        //string[] uuidList = uuidListFile.Split(char.Parse(","));


        List<Dictionary<string, object>> data_mlapdv = CSVReader.ParseText(textAssets.mlapdv.text);

        float scale = 1000f;

        for (var i = 0; i < data_mlapdv.Count; i++)
        {
            //string uuid = (string)data_mlapdv[i]["uuid"];
            float ml = (float)data_mlapdv[i]["ml"] / scale;
            float ap = (float)data_mlapdv[i]["ap"] / scale;
            float dv = (float)data_mlapdv[i]["dv"] / scale;
            dataObject.mlapdv.Add(new float3(ml, ap, dv));
        }

        byte[] tempData = dataAssetRaw.bytes;

        float[] spikeRates = new float[tempData.Length / 4];
        Buffer.BlockCopy(tempData, 0, spikeRates, 0, tempData.Length);

        for (var ui = 0; ui < data_mlapdv.Count; ui++)
        {
            //string uuid = uuidList[ui];
            FixedList4096Bytes<float> spikeRate = new FixedList4096Bytes<float>();

            for (int i = 0; i < (SCALED_LEN * conditions); i++)
            {
                spikeRate.AddNoResize(Mathf.Log(1f + spikeRates[(ui * (SCALED_LEN * conditions)) + i] * TIME_SCALE_FACTOR));
            }

            IBLEventAverageComponent eventAverageComponent = new IBLEventAverageComponent();
            eventAverageComponent.spikeRate = spikeRate;

            dataObject.data.Add(eventAverageComponent);
        }

        // Figure out which neurons we have both a mlapdv data and an event average dataset
        //List<float3> iblPos = new List<float3>();

        //foreach (string uuid in eventAverageData.Keys)
        //{
        //    if (mlapdvData.ContainsKey(uuid))
        //    {
        //        if (UnityEngine.Random.value < 0.5f)
        //        {
        //            // randomly flip the ML position of half the neurons
        //            float3 pos = mlapdvData[uuid];
        //            pos.x = 11.4f - pos.x;
        //            iblPos.Add(pos);
        //        }
        //        else
        //            iblPos.Add(mlapdvData[uuid]);
        //        eventAverageComponents.Add(eventAverageData[uuid]);
        //    }
        //}

        //Debug.Log(string.Format("Number of neurons: {0}", eventAverageComponents.Count));

        /*for (int i = 0; i < n / 100; i++)
        {
            Debug.Log(iblPos[i] + ", " + spikeRates[i * 1000] + ", " + eventAverageComponents[i].spikeRate[0]);
        }*/

        // Add neurons with different components based on the current display mode

        for (int i = 0; i < dataObject.mlapdv.Count; i++)
        {
            float3 pos = dataObject.mlapdv[i];
            // Convert order from mlapdv to apdvlr for CCF
            int posId = annotationDataset.ValueAtIndex(Mathf.RoundToInt(pos.y * 1000 / 25),
                                                        Mathf.RoundToInt(pos.z * 1000 / 25),
                                                        Mathf.RoundToInt(pos.x * 1000 / 25));
            Color posColor = ccfmodelcontrol.GetCCFAreaColor(posId);
            dataObject.colors.Add(new float4(posColor.r, posColor.b, posColor.g, 1f));
        }

        dataLists[dataGroup] = dataObject;
        Debug.Log(string.Format("Loaded for group {0} found {1} neurons {2} spikerates", dataGroup, dataObject.mlapdv.Count, dataObject.data.Count));
    }

    private (TextAsset data, TextAsset uuidList, TextAsset mlapdv) GetTextAssets(int dataGroup)
    {
        Debug.Log(string.Format("Returning assets for data group {0}", dataGroup));
        switch (dataGroup)
        {
            case 0:
                return (dataAssetRaw, uuidListAssetRaw, mlapdvAssetRaw);
            case 1:
                return (dataAssetBN, uuidListAssetBN, mlapdvAssetBN);
            case 2:
                return (dataAssetStimOn, uuidListAssetStimOn, mlapdvAssetStimOn);
            case 3:
                return (dataAssetWheel, uuidListAssetWheel, mlapdvAssetWheel);
            case 4:
                return (dataAssetFeedback, uuidListAssetFeedback, mlapdvAssetFeedback);
            default:
                return (null, null, null);
        }
    }
}
