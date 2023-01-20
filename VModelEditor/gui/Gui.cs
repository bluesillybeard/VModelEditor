//the GUI of our model editor
namespace Gui;

using BasicGUI;
public class Gui
{
    BasicGUIPlane plane;
    RenderDisplay display;
    LayoutContainer TopButtons;

    public Gui(int width, int height, RenderFont font)
    {
        display = new RenderDisplay(font);
        plane = new BasicGUIPlane(width, height, display);

        TopButtons = new LayoutContainer(null, VAllign.top, HAllign.left);

    }

}