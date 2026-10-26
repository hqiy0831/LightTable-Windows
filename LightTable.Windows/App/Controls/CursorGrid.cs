using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace LightTable.Windows;

sealed class CursorGrid : Grid
{
    readonly InputCursor arrow=InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    bool hidden;
    public CursorGrid(){ProtectedCursor=arrow;}
    public void HideLoupeCursor(bool value)
    {if(hidden==value)return;hidden=value;ProtectedCursor=value?null:arrow;}
}
