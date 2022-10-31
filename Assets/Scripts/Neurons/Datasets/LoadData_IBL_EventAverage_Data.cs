using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEditor;

public class LoadData_IBL_EventAverage_Data : MonoBehaviour
{
    [SerializeField] private CCFModelControl ccfmodelcontrol;
    [SerializeField] private NeuronEntityManager nemanager;
    [SerializeField] private VolumeDatasetManager vdmanager;

    [SerializeField] private ExperimentManager _expManager;

    //[SerializeField] private AssetReference uuidListReference;
    //[SerializeField] private AssetReference dataReference;

    [SerializeField] private TextAsset dataAsset;
    [SerializeField] private TextAsset uuidListAsset;
    [SerializeField] private TextAsset mlapdvAsset;

    public Utils util;

    //float scale = 1000;
    int SCALED_LEN = 250;
    int conditions = 4;
    int[] side = { -1, -1, 1, 1 };
    int[] corr = { 1, -1, 1, -1 };

    private float MAX_SPKRATE = 500f;

    public string displayMode = "spiking"; // Options: "spiking", "grayscaleFR, "byRegionFR"

    private float[] spikeRateMap;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Loading Event Average Data...");
        //ParseIBLData_EventAverage();

        vdmanager.LoadAnnotationDataset(new List<Action> { DelayedStart });
    }

    private void DelayedStart()
    {
        CCFAnnotationDataset annotationDataset = vdmanager.GetAnnotationDataset();

        string uuidListFile = uuidListAsset.text;
        string[] uuidList = uuidListFile.Split(char.Parse(","));

        byte[] tempData = dataAsset.bytes;

        float[] spikeRates = new float[tempData.Length / 4];
        Buffer.BlockCopy(tempData, 0, spikeRates, 0, tempData.Length);

        List<Dictionary<string, object>> data_mlapdv = CSVReader.ParseText(mlapdvAsset.text);

        Dictionary<string, float3> mlapdvData = new Dictionary<string, float3>();
        float scale = 1000f;

        for (var i = 0; i < data_mlapdv.Count; i++)
        {
            string uuid = (string)data_mlapdv[i]["uuid"];
            float ml = (float)data_mlapdv[i]["ml"] / scale;
            float ap = (float)data_mlapdv[i]["ap"] / scale;
            float dv = (float)data_mlapdv[i]["dv"] / scale;
            mlapdvData.Add(uuid, new float3(ml, ap, dv));
        }

        // Find all indexes in uuidList that are valid
        List<int> validIndexes = new List<int>();
        List<float3> validMlapdv = new List<float3>();

        for (int i = 0; i < uuidList.Length; i++)
            if (mlapdvData.ContainsKey(uuidList[i]))
            {
                validIndexes.Add(i);
                validMlapdv.Add(mlapdvData[uuidList[i]]);
            }

        // Set up texture

        // [TODO]
        // Note that we'll use the r/g/b indexes to represent different scaling sizes for different timepoints
        // so the time index 0->999 maps onto index%3 and then which channel based on the remainder.

        int nTimepoints = SCALED_LEN * conditions;

        // For now, we just map onto 1000 values but just use the red channel
        //Debug.Log(string.Format("Texture size ({0},{1})", validIndexes.Count, nTimepoints));
        //Texture2D dataTexture = new Texture2D(validIndexes.Count, nTimepoints);
        float[] uCoords = new float[validIndexes.Count];

        int pos = 0;
        float maxScale = 0f;
        foreach (int ui in validIndexes)
        {
            //for (int i = 0; i < nTimepoints; i++)
            //{
            //    float cScale = spikeRates[(ui * nTimepoints) + i] / 100f;
            //    if (cScale > maxScale)
            //        maxScale = cScale;
            //    dataTexture.SetPixel(pos, i, new Color(cScale, 1f, 0f));
            //}
            uCoords[pos] = pos / (float)validIndexes.Count;
            pos++;
        }
        Debug.Log(maxScale);

        //// Save Texture to be auto-loaded into shader
        //AssetDatabase.CreateAsset(dataTexture, "Assets/Datasets/v2/dataTexture.asset");
        //return;

        // Set global shader property
        //Shader.SetGlobalTexture("_ScaleTexture", dataTexture);

        float4[] neuronColors = new float4[validMlapdv.Count];
        for (int i = 0; i < validMlapdv.Count; i++)
        {
            int posId = annotationDataset.ValueAtIndex((int)Math.Round(validMlapdv[i].y * 1000 / 25, 0),
                                                               (int)Math.Round(validMlapdv[i].z * 1000 / 25, 0),
                                                               (int)Math.Round(validMlapdv[i].x * 1000 / 25, 0));
            Color temp = ccfmodelcontrol.GetCCFAreaColor(posId);
            neuronColors[i] = new float4(temp.r, temp.g, temp.b, 1f);
        }

        nemanager.AddNeurons(validMlapdv, neuronColors, uCoords);

        _expManager.Play();
    }
}
