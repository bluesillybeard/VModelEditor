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
    StackingContainer fileMenu;


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
        fileButton = new ButtonElement(topButtons, FileMenuShow, null, null, null, null);
        fileButton.drawable = new TextElement(fileButton, 0xFFFFFFFF, this.fontSize, "File", display.defaultFont, display, 0);
        // Triangles or Vertices toggle. Defaults to Triangles
        verticesOrTrianglesButton = new ButtonElement(topButtons, null, null, ToggleVerticesOrTriangles, null, null);
        verticesOrTrianglesButton.drawable = new TextElement(verticesOrTrianglesButton, 0xFFFFFFFF, this.fontSize, "Triangle", display.defaultFont, display, 0);
        verticesOrTriangles = VerticesOrTriangles.Triangles;

        fileMenu = new StackingContainer(null, StackDirection.down, -1);
        var openButton = new ButtonElement(fileMenu, null, null, (_) => {Console.WriteLine("Opening file apparently");}, null, null);
        new TextElement(openButton, 0xFFFFFFFF, fontSize, "Open File", font, display, 0);
        var saveButton = new ButtonElement(fileMenu, null, null,  (_) => {Console.WriteLine("saving file apparently");}, null, null);
        new TextElement(saveButton, 0xFFFFFFFF, fontSize, "Save File", font, display, 0);


    }

    public void Update()
    {
        plane.Iterate();
        FileMenuUpdate();
    }
    public void Render()
    {
        plane.Draw();
    }
    private bool _addFileMenu;
    void FileMenuShow(ButtonElement _)
    {
        //We can't add elements while we are iterating - bad things happen.
        // Instead we simply set this flag for it to be added once the iteration is over.
        _addFileMenu = true;
    }

    void FileMenuUpdate()
    {
        if(topButtons.GetChildren().Contains(fileMenu))
        {
            bool remove = !ContainsHoveredButtons(fileMenu);
            if(remove)
            {
                topButtons.RemoveChild(fileMenu);
                topButtons.AddChildBeginning(fileButton);
            }
        }
        if(_addFileMenu)
        {
            topButtons.RemoveChild(fileButton);
            topButtons.AddChildBeginning(fileMenu);
            _addFileMenu = false;
        }
    }

    bool ContainsHoveredButtons(IContainerNode n)
    {
        bool contains = false;
        foreach(INode node in n.GetChildren())
        {
            if(node is ButtonElement b)
            {
                if(b.isHover) contains = true;
            }
        }
        return contains;
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
            ((TextElement)verticesOrTrianglesButton.drawable).Text = "Triangle";
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