using VRender;
using BasicGUI;
using vmodel;
using GUI;
public class VModelEditor
{
    Gui ui;
    public VModelEditor()
    {
        RenderSettings settings = new RenderSettings()
        {
            BackgroundColor = 0x00000000,
        };
        //Loading Stuff
        IRender render = RenderUtils.CreateIdealRenderOrDie(settings);
        //TODO: actual error handling
        #nullable disable
        RenderFont font = new RenderFont(render.LoadTexture("ascii.png", out var e1), render.LoadShader("gui", out var e2));
        #nullable enable
        System.Console.WriteLine(e1);
        System.Console.WriteLine(e2);
        //Setup
        ui = new Gui(render.WindowSize().X, render.WindowSize().Y, font, 20);
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
    }
}