using VRender;
using vmodel;
using GUI;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
public class VModelEditor
{
    public VModel? model;
    Gui ui;
    IRender render;
    IRenderEntity? modelEntity;
    IRenderShader shader;
    RenderCamera camera;
    public VModelEditor()
    {
        RenderSettings settings = new RenderSettings()
        {
            BackgroundColor = 0x00000000,
        };
        //Loading Stuff
        render = RenderUtils.CreateIdealRenderOrDie(settings);
        //TODO: actual error handling
        #nullable disable
        RenderFont font = new RenderFont(render.LoadTexture("ascii.png", out var e1), render.LoadShader("gui", out var e2));
        #nullable enable
        System.Console.WriteLine(e1);
        System.Console.WriteLine(e2);
        var shaderQuestionMark = render.LoadShader("", out var err);
        if(shaderQuestionMark is null)throw new Exception("SHADER IS MISSING BRO");
        shader = shaderQuestionMark;

        camera = new RenderCamera(Vector3.Zero, Vector3.Zero, 90, render.WindowSize());
        render.SetCamera(camera);
        //Setup
        ui = new Gui(render.WindowSize().X, render.WindowSize().Y, font, 20, this);
        render.OnUpdate += Update;
        render.OnRender += Render;
        render.Run();
    }

    private void Render(double delta)
    {
        ui.Render();
    }

    private void Update(double delta)
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

    public void OpenModel(VModel model)
    {
        this.model = model;
        //We need to set up rendering for this model.
        // TODO: attribute detection and converting the model to the attributes for the shaders

        //For now we just upload it to the GPU and hope with all our might that it just so happens to have the correct attributes
        var gpuModel = render.LoadModel(model);
        modelEntity = render.SpawnEntity(EntityPosition.Zero, shader, gpuModel.mesh, gpuModel.texture, true, null);
    }
}