using Avalonia.Controls.Primitives;

namespace RemnantOverseer.Controls;

public class RequiredToggleButton : ToggleButton
{
    protected override void OnClick()
    {
        if (IsChecked == true)
        {
            return;
        }

        base.OnClick();
    }
}
