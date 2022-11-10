using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;

public struct IBLEventAverageComponent : IComponentData
{
    public float baseline;
    public FixedList4096Bytes<float> spikeRate;
}

[MaterialProperty("_uCoord", MaterialPropertyFormat.Float)]
public struct MaterialUCoord : IComponentData
{
    public float Value;
}
[MaterialProperty("_vCoord", MaterialPropertyFormat.Float)]
public struct MaterialVCoord : IComponentData
{
    public float Value;
}

[MaterialProperty("_BrainScale", MaterialPropertyFormat.Float)]
public struct MaterialBrainScale : IComponentData
{
    public float Value;
}