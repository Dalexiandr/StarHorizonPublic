using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology;

[Serializable, NetSerializable]
public enum CytologyGrowingVatVisualStates : byte
{
    Working,
    Powered,
    IsError,
    WithLiquid,
    WithFoam
}

[Serializable, NetSerializable]
public enum CytologyGrowingVatVisualLayers : byte
{
    Base,
    Indicator,
    Liquid,
    Foam
}


[Serializable, NetSerializable]
public enum CytologySwabVisualLayers : byte
{
    Sample
}

[Serializable, NetSerializable]
public enum CytologySwabVisualStates : byte
{
    IsVisible
}

[Serializable, NetSerializable]
public enum CytologyPetriDishVisualLayers : byte
{
    Fill,
    Foam
}

[Serializable, NetSerializable]
public enum CytologyPetriDishVisualStates : byte
{
    Samples,
    Color
}

[Serializable, NetSerializable]
public enum CytologyInjectorVisualLayers : byte
{
    Indicator
}

[Serializable, NetSerializable]
public enum CytologyInjectorVisualStates : byte
{
    HasSamples
}
