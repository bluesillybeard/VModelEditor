//the GUI of our model editor
namespace GUI;

using BasicGUI;

using System.Diagnostics;

using vmodel;

using StbImageSharp;

using VRender;
public sealed class Gui
{
    //properties of the GUI state
    public int fontSize;
    public VerticesOrTriangles verticesOrTriangles;
    //The Rendering stuff
    BasicGUIPlane plane;
    RenderDisplay display;
    //The GUI elements
    StackingContainer topButtons;
    ButtonElement fileButton;
    ButtonElement verticesOrTrianglesButton;
    // This replaced the file button when the file button is hovered.
    StackingContainer fileMenu;

    TableContainer meshTable;
    //There's definitely a better way to do this but I am too lazy to figure out what it is.
    // I made an entire application whose sole purpose is to create an open/save file dialog then print the chosen file to its output.
    Process? openFilePopup;
    Process? saveFilePopup;

    //the editor so we can alert it when things happen
    VModelEditor editor;
    
    bool modelChanged = false;
    public Gui(int width, int height, RenderFont font, int fontSize, VModelEditor editor)
    {
        this.fontSize = fontSize;
        this.editor = editor;
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
        var openButton = new ButtonElement(fileMenu, null, null, OpenFileButton, null, null);
        new TextElement(openButton, 0xFFFFFFFF, fontSize, "Open File", font, display, 0);
        var saveButton = new ButtonElement(fileMenu, null, null,  SaveFileButton, null, null);
        new TextElement(saveButton, 0xFFFFFFFF, fontSize, "Save File", font, display, 0);
        
        //The table of vertices
        meshTable = new TableContainer(
            (container) => {
                return new ColorOutlineRectElement(container, 0x666666ff, null, null, null, null, fontSize/4, 0);
            }, 1, new List<INode>(), //Only one column for now since that depends on the vertex attributes.
            new LayoutContainer(plane.GetRoot(), new List<INode>(), VAllign.top, HAllign.right), fontSize/3
        );
        new TextElement(meshTable, 0xFFFFFFFF, fontSize, "No model loaded",font, display, 0);
    }

    public void Update()
    {
        FileMenuUpdate();
        MeshTableUpdate();
        var size = VRenderLib.Render.WindowSize();
        plane.SetSize(size.X, size.Y);
        plane.Iterate();
        modelChanged = false;
    }
    public void Render()
    {
        plane.Draw();
    }
    private static readonly char[] intToXYZW = new char[]{'X', 'Y', 'Z', 'W'};
    private void MeshTableUpdate()
    {
        if(editor.model is not null && modelChanged)
        {
            System.Console.WriteLine("Regenerated mesh table");
            //populate the table.
            // TODO: scrolling
            // TODO: make the points editable
            // TODO: vertices or triangles
            //clear the table
            var children = meshTable.GetChildren().ToArray(); //ToArray to avoid enumeration problems
            foreach(var child in children)
            {
                meshTable.RemoveChild(child);
            }
            Attributes attribs = editor.model.Value.mesh.attributes;
            //make sure the table has the right number of columns
            meshTable.columns = (int)attribs.TotalAttributes();
            //table labels
            foreach(EAttribute attr in attribs)
            {
                for(int i=0; i<(int)attr % 5; i++)
                {
                    new TextElement(
                        meshTable, 0xFFFFFF, fontSize, 
                        Enum.GetName(typeof(EAttribute), attr) + intToXYZW[i], 
                        display.defaultFont, display, 0
                    );
                }
            }
            //Table values
            foreach(float f in editor.model.Value.mesh.vertices)
            {
                var textBox = new TextBoxElement(
                    meshTable, fontSize, 0xFFFFFFFF, display.defaultFont, display, 0
                );
                textBox.back = new ColorBackgroundElement(textBox, 0x000010FF, 2);
                textBox.SetText(f.ToString());
            }
        }
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
        HandleOpenFile();
        HandleSaveFile();
    }

    void HandleOpenFile()
    {
        //if a file has been selected or cancelled
        if(openFilePopup is not null && openFilePopup.HasExited)
        {
            var output = openFilePopup.StandardOutput;
            string str = output.ReadToEnd();
            if(str.StartsWith("FILE"))
            {
                OpenFile(str.Substring(4));
            } else {
                System.Console.WriteLine("You're a dingus");
            }
            openFilePopup.Dispose();
            openFilePopup = null;
        }
    }
    void HandleSaveFile()
    {
        //if a file has been selected or cancelled
        if(saveFilePopup is not null && saveFilePopup.HasExited)
        {
            var output = saveFilePopup.StandardOutput;
            string str = output.ReadToEnd();
            if(str.StartsWith("FILE"))
            {
                SaveFile(str.Substring(4));
            } else {
                System.Console.WriteLine("You're a dingus");
            }
            saveFilePopup.Dispose();
            saveFilePopup = null;
        }
    }
    //The final function that is called when it is time to open a file
    void OpenFile(string path)
    {
        //First, we detect the file type.
        // For now we will only do Voxelesque models (VMF)
        if(path.EndsWith('/'))
        {
            // TODO: tell the user using an actual GUI popup or something
            System.Console.Error.WriteLine("File is a directory");
        }
        // TODO: tell the user using an actual GUI popup or something
        if(!File.Exists(path))System.Console.Error.WriteLine("File is a directory or doesn't exist");
        //Verify that is a valid vmf file
        // raw VMeshes may be supported in the future, but for now they won't
        VModel? model;
        List<VError>? err = null;
        if(!path.ToLower().EndsWith(".vmf")){
            //Load non VMF file
            model = FileImports.LoadModelWithAssimp(path, out var error, editor.fallbackTexture);
            if(error is not null)
            {
                err = new List<VError>();
                err.Add(new VError(error));
            }
        } else
        {
            model = VModelUtils.LoadModel(path, out err);
        }
        if(model is null){
            //Check the possibility that it was actually an image
            try{
                var image = ImageResult.FromMemory(File.ReadAllBytes(path));
                editor.OpenImage(image);
                modelChanged = true;
                return;
            } catch(Exception e)
            {
                if(err is null) err = new List<VError>();
                err.Add(new VError(e));
            }
            string errors = String.Empty;
            if(err is not null)
            {
                errors = string.Join(',', err);
            }
            System.Console.Error.WriteLine("Error(s) while loading file:" + errors);
            return;
        }
        //We have the model, give it to the rest of the application
        modelChanged = true;
        editor.OpenModel(model.Value);
    }
    //The final function that is called when it is time to save a file.
    // For now, it just saves the vmf, vmesh, and png into a folder, because I am too lazy to do that.
    void SaveFile(string path)
    {
        //We can't save if there is no model
        if(editor.model is null)return;
        try{
            //For the same of simplicity, I just make that path as a directory then save our files into it.
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string name = new DirectoryInfo(path).Name;
            //save the files
            VModelUtils.SaveModel(editor.model.Value, path, "mesh.vmesh", "texture.png", "model.vmf");
        } catch(Exception e){
            System.Console.WriteLine("Could not save file: " + e.StackTrace + "\n " + e.Message);
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

    void OpenFileButton(ButtonElement _)
    {
        //Opening a popup to open a file may seem simple at first.
        // But in reality it's pretty complex.
        // I can't get access to the system file dialog,
        // as I am using GLFW+OpenGL which does not provide such functionality.
        // So instead I start a completely separate process whose entire purpose is to be the file dialog.
        // When a file has been selected, it will send the result to us through the standard output.
        // Once the output is sent, it will be parsed.

        //First, we start the process and stash it somewhere
        var process = new Process();
        //It's OS specific.
        if(OperatingSystem.IsLinux())
            process.StartInfo.FileName = "FileDialog";
        else if(OperatingSystem.IsWindows())
            process.StartInfo.FileName = "FileDialog.exe";
        else
            throw new Exception("Only Linux and Windows are supported!");
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        openFilePopup = process;
    }

    void SaveFileButton(ButtonElement _)
    {
        var process = new Process();
        if(OperatingSystem.IsLinux())
            process.StartInfo.FileName = "FileDialog";
        else if(OperatingSystem.IsWindows())
            process.StartInfo.FileName = "FileDialog.exe";
        else
            throw new Exception("Only Linux and Windows are supported!");
        process.StartInfo.Arguments = "save";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        saveFilePopup = process;
    }
}

public enum VerticesOrTriangles
{
    Vertices, Triangles
}