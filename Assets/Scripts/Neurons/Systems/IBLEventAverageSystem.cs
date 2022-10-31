using System;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;


// Note that all systems that modify neuron color need this -- otherwise neuron color *could*
// get reset on the same frame when the FPS is low
[UpdateAfter(typeof(NeuronSpikingSystem))]
public partial class IBLEventAverageSystem : SystemBase
{
    IBLTask iblTask;
    //private float trialTimeIndex;
    private NeuronEntityManager nemanager;
    private EventAverageManager eaManager;

    float prevBrainScale = 1f;

    protected override void OnStartRunning()
    {
        //trialTimeIndex = 0;
        iblTask = GameObject.Find("main").GetComponent<ExperimentManager>().GetIBLTask();
        nemanager = GameObject.Find("main").GetComponent<NeuronEntityManager>();
        eaManager = GameObject.Find("EventAverage").GetComponent<EventAverageManager>();
    }

    protected override void OnUpdate()
    {
        //trialTimeIndex += 0.1f;
        float deltaTime = Time.DeltaTime;
        double curTime = Time.ElapsedTime;
        bool corr = iblTask.GetCorrect();
        int curIndex = iblTask.GetTimeIndex();
        float smallScale = nemanager.GetNeuronScale();

        float brainScaleRaw = eaManager.BrainScale;
        float brainScale = 0.0625f * eaManager.BrainScale;

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
                //float4 maxFRColor = lerpColor.maxColor;
                //float4 zeroFRColor = lerpColor.zeroColor;
                float curPercent = eventAverage.spikeRate[curIndex];
                //color.Value = new float4(Mathf.Lerp(zeroFRColor.x, maxFRColor.x, curPercent),
                //                         Mathf.Lerp(zeroFRColor.y, maxFRColor.y, curPercent),
                //                         Mathf.Lerp(zeroFRColor.z, maxFRColor.z, curPercent),
                //                         Mathf.Lerp(zeroFRColor.w, maxFRColor.w, curPercent));
                scale.Value = 0.01f + curPercent * brainScale;
            }).ScheduleParallel(); // .Run();

        Entities
            .ForEach((ref MaterialVCoord vCoord) =>
            {
                vCoord.Value = curIndexPerc;
            }).ScheduleParallel();
    }
}
