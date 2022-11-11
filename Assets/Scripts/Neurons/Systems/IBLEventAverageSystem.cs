using System;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;


// Note that all systems that modify neuron color need this -- otherwise neuron color *could*
// get reset on the same frame when the FPS is low
//[UpdateAfter(typeof(NeuronSpikingSystem))]
public partial class IBLEventAverageSystem : SystemBase
{
    IBLTask iblTask;
    //private float trialTimeIndex;
    private NeuronEntityManager nemanager;
    private EventAverageManager eaManager;

    private int[] trialIdxsFull = { 0, 250, 500, 750 };
    private int[] trialIdxsPSTH = { 0, 100, 200, 300 };

    float prevBrainScale = 1f;
    float prevIndex;

    protected override void OnStartRunning()
    {
        //trialTimeIndex = 0;
        prevIndex = -1;

        iblTask = GameObject.Find("main").GetComponent<ExperimentManager>().GetIBLTask();
        nemanager = GameObject.Find("main").GetComponent<NeuronEntityManager>();
        eaManager = GameObject.Find("EventAverage").GetComponent<EventAverageManager>();
    }

    protected override void OnUpdate()
    {
        // If materials are not transparent, skip rendering the brain
        if (!eaManager.MaterialsTransparent)
            return;

        float brainScaleRaw = eaManager.brainScale;
        float brainScale = 0.0625f * brainScaleRaw * eaManager.neuronScaleMult;
        bool baseline = eaManager.useBaseline;
        int dataGroup = eaManager.trialDatasetIndex;


        float spikingScale = baseline ?
            (dataGroup == 0 ? 10f : 5f) :
            125f;

        if (eaManager.standaloneMode)
        {
            int curIndex = iblTask.GetTimeIndex();

            int trialStartIdx = eaManager.trialDatasetType? trialIdxsFull[eaManager.trialType] : trialIdxsPSTH[eaManager.trialType];

            curIndex += trialStartIdx;

            // Update lerping neurons
            if (curIndex != prevIndex || eaManager.forceUpdate)
            {
                eaManager.forceUpdate = false;
                prevIndex = curIndex;
                Entities
                    .ForEach((ref Scale scale, in IBLEventAverageComponent eventAverage) =>
                    {
                        if (baseline)
                        {
                            float spk = eventAverage.spikeRate[curIndex];
                            spk = Mathf.Clamp((spk - eventAverage.baseline) / eventAverage.baseline,0f,10f);
                            scale.Value = spk * brainScale / spikingScale;
                        }
                        else
                            scale.Value = eventAverage.spikeRate[curIndex] * brainScale / spikingScale;
                    }).ScheduleParallel(); // .Run();
            }
        }
        else
        {        //trialTimeIndex += 0.1f;
            //float deltaTime = Time.DeltaTime;
            //double curTime = Time.ElapsedTime;
            bool corr = iblTask.GetCorrect();
            int curIndex = iblTask.GetTimeIndex();
            float smallScale = nemanager.GetNeuronScale();

            int trialStartIdx;
            if (iblTask.GetSide() == -1)
            {
                trialStartIdx = corr ? 0 : 250;
            }
            else
            {
                trialStartIdx = corr ? 500 : 750;
            }
            curIndex += trialStartIdx;
            float curIndexPerc = curIndex / 999f;

            // check for scale change
            if (brainScale != prevBrainScale)
            {
                prevBrainScale = brainScale;

                // Update neurons
                Entities
                    .ForEach((ref Translation pos, ref Scale scale, in PositionComponent origPos) =>
                    {
                        pos.Value = new float3(5.7f - origPos.position.x, 4 - origPos.position.z, origPos.position.y - 6.6f) * brainScaleRaw;
                        scale.Value = 0.015f * brainScaleRaw;
                    }).ScheduleParallel();
            }

            // Update lerping neurons
            Entities
                .ForEach((ref Scale scale, in IBLEventAverageComponent eventAverage) =>
                {
                    //float curPercent = eventAverage.spikeRate[curIndex];

                    float spk = eventAverage.spikeRate[curIndex];
                    spk = Mathf.Clamp((spk - eventAverage.baseline) / eventAverage.baseline, 0f, 10f);
                    scale.Value = spk * brainScale / spikingScale;

                    //scale.Value = 0.01f + curPercent * brainScale;
                }).ScheduleParallel(); // .Run();

            Entities
                .ForEach((ref MaterialVCoord vCoord) =>
                {
                    vCoord.Value = curIndexPerc;
                }).ScheduleParallel();
        }
    }
}
