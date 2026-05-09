namespace Content.Server._Horizon.FoodBoost;

[RegisterComponent]
public sealed partial class ChefComponent : Component
{
    [DataField]
    public bool Advanced = false;
}
