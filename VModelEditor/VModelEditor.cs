using VRender.Interface;
using VRender.Utility;
using VRender;
using vmodel;
using GUI;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using StbImageSharp;
public class VModelEditor
{
    public VModel? model;
    public ImageResult fallbackTexture;

    //TODO: Refactor so this isn't required
    #nullable disable
    Gui ui; //UI is initialize in the Start method since it requires the Render to be functional
    #nullable restore
    IRender render;
    Camera camera;

    IRenderTexture? modelRenderTexture;
    IRenderMesh? modelRenderMesh;
    IRenderShader? modelShader;

    public VModelEditor()
    {
        RenderSettings settings = new RenderSettings()
        {
            BackgroundColor = 0x00000000,
            WindowTitle = "VModel converter",
            size = new Vector2i(800, 600),
            TargetFrameTime = 1f/60f,
        };
        //Loading Stuff
        render = VRenderLib.InitRender(settings);
        render.OnStart += Start;
        render.OnUpdate += Update;
        render.OnDraw += Render;

        //Load some stuff that doesn't interact with the GPU yet
        fallbackTexture = ImageResult.FromMemory(File.ReadAllBytes("ascii.png"));
        camera = new Camera(Vector3.Zero, Vector3.Zero, 90, render.WindowSize());
        render.Run();

        //render.Dispose();
    }
    private void Start()
    {
        var font = render.LoadTexture("ascii.png", out var e1);
        if(font is null)
        {
            System.Console.WriteLine(e1);
            throw new Exception("bro can't load font file");
        }
        //Setup
        ui = new Gui(render.WindowSize().X, render.WindowSize().Y, font, 12, this);
    }
    private void Render(TimeSpan delta)
    {
        render.BeginRenderQueue();
        if(modelRenderMesh is not null && modelRenderTexture is not null && modelShader is not null)
        {
            KeyValuePair<string, object>[] uniforms = new KeyValuePair<string, object>[]{
                new KeyValuePair<string, object>("camera", camera.GetTransform()),
                //The model does not have a transform; the camera moves around it instead.
            };
            render.Draw(modelRenderTexture, modelRenderMesh, modelShader, uniforms, true);
        }
        
        ui.Render();
        render.EndRenderQueue();
    }

    private void Update(TimeSpan delta)
    {
        ui.Update();
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var keyboard = render.Keyboard();
        var mouse = render.Mouse();
        if (keyboard.IsKeyReleased(Keys.C))
        {
            render.CursorLocked  = !render.CursorLocked;
        }

        Vector3 cameraInc = new Vector3();
        if (keyboard.IsKeyDown(Keys.W)) {
            cameraInc.Z = -1;
        } else if (keyboard.IsKeyDown(Keys.S)) {
            cameraInc.Z = 1;
        }
        if (keyboard.IsKeyDown(Keys.A)) {
            cameraInc.X = -1;
        } else if (keyboard.IsKeyDown(Keys.D)) {
            cameraInc.X = 1;
        }
        if (keyboard.IsKeyDown(Keys.LeftControl)) {
            cameraInc.Y = -1;
        } else if (keyboard.IsKeyDown(Keys.Space)) {
            cameraInc.Y = 1;
        }
        // Update camera position
        float cameraSpeed = 1f / 6f;
        if(keyboard.IsKeyDown(Keys.LeftShift)) cameraSpeed = 1f;

        camera.Move(cameraInc * cameraSpeed);

        // Update camera baseda on mouse
        float sensitivity = 0.5f;

        if (render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
            camera.Rotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
    }
    
    public void OpenImage(ImageResult r)
    {
        if(model is null)
            return;
        VModel m = model.Value;
        m.texture = r;
        OpenModel(m);
    }
    public void OpenModel(VModel model)
    {
        if(modelRenderMesh is not null)
        {
            modelRenderMesh.Dispose();
            modelRenderMesh = null;
        }
        if(modelRenderTexture is not null)
        {
            modelRenderTexture.Dispose();
            modelRenderTexture = null;
        }
        //We don't bother destroying the shader since it's very small and VRender reuses shaders if it can.
        this.model = model;
        //We need to set up rendering for this model.

        var gpuModel = render.LoadModel(model);
        modelRenderMesh = gpuModel.mesh;
        modelRenderTexture = gpuModel.texture;
        modelShader = render.GetShader(new ShaderFeatures(model.mesh.attributes, false, true));
    }
}