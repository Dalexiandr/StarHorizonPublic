using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._Horizon.Lobby.UI;

/// <summary>
/// Выбор цвета градиента только ползунками R/G/B без ввода цифр.
/// </summary>
public sealed class GradientColorSliders : BoxContainer
{
    public Color Color
    {
        get => new(_rSlider.Value, _gSlider.Value, _bSlider.Value, 1f);
        set
        {
            _updating = true;
            _rSlider.Value = value.R;
            _gSlider.Value = value.G;
            _bSlider.Value = value.B;
            _updating = false;
        }
    }

    public event Action<Color>? OnColorChanged;

    private readonly Slider _rSlider;
    private readonly Slider _gSlider;
    private readonly Slider _bSlider;
    private bool _updating;

    public GradientColorSliders()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        _rSlider = new Slider
        {
            MinValue = 0f,
            MaxValue = 1f,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center
        };
        _gSlider = new Slider
        {
            MinValue = 0f,
            MaxValue = 1f,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center
        };
        _bSlider = new Slider
        {
            MinValue = 0f,
            MaxValue = 1f,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center
        };

        _rSlider.OnValueChanged += _ => OnSlidersChanged();
        _gSlider.OnValueChanged += _ => OnSlidersChanged();
        _bSlider.OnValueChanged += _ => OnSlidersChanged();

        AddChild(MakeRow("R", _rSlider));
        AddChild(MakeRow("G", _gSlider));
        AddChild(MakeRow("B", _bSlider));
    }

    private static BoxContainer MakeRow(string label, Slider slider)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };
        row.AddChild(new Label
        {
            Text = label,
            MinSize = new Vector2(20, 0),
            VerticalAlignment = VAlignment.Center
        });
        row.AddChild(slider);
        return row;
    }

    private void OnSlidersChanged()
    {
        if (_updating)
            return;
        OnColorChanged?.Invoke(Color);
    }
}
