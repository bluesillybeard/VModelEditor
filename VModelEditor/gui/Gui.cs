//the GUI of our model editor
namespace GUI;

using BasicGUI;
public sealed class Gui
{
    //properties of the GUI state
    public int fontSize;
    public VerticesOrTriangles verticesOrTriangles;
    //GUI elements and containers
    BasicGUIPlane plane;
    RenderDisplay display;
    StackingContainer topButtons;
    ButtonElement fileButton;
    ButtonElement verticesOrTrianglesButton;

    //LayoutContainer fileMenu;

    public Gui(int width, int height, RenderFont font, int fontSize)
    {
        this.fontSize = fontSize;
        //builds the UI
        display = new RenderDisplay(font);
        plane = new BasicGUIPlane(width, height, display);
        //The top buttons are always visible and are on the root node.
        var tl = new LayoutContainer(plane.GetRoot(), VAllign.top, HAllign.left);
        topButtons = new StackingContainer(tl, StackDirection.right, fontSize);
        //Filling TopButtons
        //File menu button
        fileButton = new ButtonElement(topButtons, FileMenuShow, FileMenuHide, null, null, null);
        fileButton.drawable = new TextElement(fileButton, 0xFFFFFFFF, this.fontSize, "File", display.defaultFont, display, 0);
        // Triangles or Vertices toggle. Defaults to Triangles
        verticesOrTrianglesButton = new ButtonElement(topButtons, null, null, ToggleVerticesOrTriangles, null, null);
        verticesOrTrianglesButton.drawable = new TextElement(verticesOrTrianglesButton, 0xFFFFFFFF, this.fontSize, "Triangle", display.defaultFont, display, 0);
        verticesOrTriangles = VerticesOrTriangles.Triangles;
    }

    public void Update()
    {
        plane.Iterate();
    }
    public void Render()
    {
        plane.Draw();
    }

    void FileMenuShow(ButtonElement b)
    {
        System.Console.WriteLine("ShowFileMenu");
    }
    void FileMenuHide(ButtonElement b)
    {
        System.Console.WriteLine("HideFileMenu");
    }

    void ToggleVerticesOrTriangles(ButtonElement b)
    {
        //Handle vertices case
        if(verticesOrTriangles is VerticesOrTriangles.Vertices)
        {
            verticesOrTriangles = VerticesOrTriangles.Triangles;
            //I know FOR A FACT that the drawable is a text element and is not null.
            // At least as long as nobody messed with it while I wasn't looking...
            #nullable disable
            ((TextElement)verticesOrTrianglesButton.drawable).Text = "Triangles";
            #nullable enable
            return;
        }
        //It was triangles
        //I know FOR A FACT that the drawable is a text element and is not null.
        // At least as long as nobody messed with it while I wasn't looking...
        #nullable disable
        ((TextElement)verticesOrTrianglesButton.drawable).Text = "Vertices";
        #nullable enable
        verticesOrTriangles = VerticesOrTriangles.Vertices;
        
    }
}

public enum VerticesOrTriangles
{
    Vertices, Triangles
}