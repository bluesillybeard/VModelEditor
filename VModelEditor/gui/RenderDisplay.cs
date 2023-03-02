using VRender;
using VRender.Interface;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using BasicGUI;
using vmodel;
using StbImageSharp;

namespace GUI;
//a Texture and Shader for rendering a font.
public class RenderFont
{
    public RenderFont(IRenderTexture fontTexture, IRenderShader fontShader)
    {
        texture = fontTexture;
        shader  = fontShader;
    }
    public IRenderTexture texture;
    public IRenderShader shader;
}

public sealed class RenderDisplay : IDisplay
{

    public RenderDisplay(RenderFont defaultFont)
    {
        this.defaultFont = defaultFont;
        //Load the unit square mesh
        VMesh squareMesh = new VMesh(
            new float[]{
                //position, textureCoord
                -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
                -0.5f, 0.5f, 0.0f, 0.0f, 1.0f,
                0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
                0.5f, 0.5f, 0.0f, 1.0f, 1.0f,
            },
            new uint[]{
                0, 1, 2,
                1, 2, 3,
            },
            new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords}),
            null
        );
        this.square = VRenderLib.Render.LoadMesh(squareMesh);
        //load the pure color shader.
        // The shader is pretty constant and hidden so it's hard-coded
        pureColor = VRenderLib.Render.GetShader(
            //vertex shader code
            @"
            layout (location=0) in vec3 position;
            layout (location=1) in vec2 texCoords; //These are ignored.

            uniform mat4 model;
            //uniform mat4 camera; We don't apply the camera transform
            void main()
            {
                gl_Position = model * position;
            }
            ",
            //fragment shader code
            @"
            uniform vec4 color;
            out vec4 colorOut;
            void main()
            {
                colorOut = color;
            }
            ",
            this.square.GetAttributes()
        );
    }
    //default font for text rendering
    public RenderFont defaultFont;
    //a one unit square in the XY plane, with texture coordinates for drawing images
    private IRenderMesh square;
    // This is a custom shader that takes in a vec4 uniform as a color and renders the entire object in that color
    private IRenderShader pureColor;
    int textIndex = 0;
    public void BeginFrame()
    {
        //We actually don't call any VRender functions in here since we assume someone else already did
    }
    public void EndFrame()
    {
        //We actually don't call any VRender functions in here since we assume someone else already did
    }
    public void DrawPixel(int x, int y, uint rgb, byte depth = 0)
    {
        var size = VRenderLib.Render.WindowSize();
        //To make things faster, we cull pixels that won't be drawn anyway
        if(x < 0 || x > size.X || y < 0 || y > size.Y)
        {
            return;
        }
        //The input is pixel coordinates, so we need to convert them into OpenGl coordinates
        float glX = ((float)x/(float)size.X - 0.5f) * 2;
        float glY = ((float)y/(float)size.Y - 0.5f) * 2;
        //we need to create a matrix transform that will turn our unit square into a pixel-sized object.
        Matrix4 transform = new Matrix4();
        //TODO: these might need to be reversed since multiplication order matters in matrices
        Matrix4.CreateTranslation(-glX, -glY, depth/256f, out transform);
        transform *= Matrix4.CreateScale( 1f/size.X, 1f/size.Y, 1f);
        //it's rendering time
        VRenderLib.Render.Draw(
            defaultFont.texture, //this texture isn't rendered, but for the sake of the Render API we use it anyway
            square, pureColor,
            new KeyValuePair<string, object>[]{
                new KeyValuePair<string, object>("color", new Vector4())
            },true
        );
    }
    public void FillRect(int x0, int y0, int x1, int y1, uint rgb, byte depth = 0)
    {

    }
    //These were copied and translated from Wikipedia, of all places: https://en.wikipedia.org/wiki/Bresenham's_line_algorithm
    // Stackoverflow doesn't have ALL the answers.
    public void DrawLine(int x1, int y1, int x2, int y2, uint rgb, byte depth = 0)
    {
        
    }

    public void DrawVerticalLine(int x, int y1, int y2, uint rgb, byte depth = 0)
    {
        DrawLine(x, y1, x, y2, rgb, depth);
    }
    public void DrawHorizontalLine(int x1, int x2, int y, uint rgb, byte depth = 0)
    {
        DrawLine(x1, y, x2, y, rgb, depth);
    }
    public void DrawImage(object image, int x, int y, byte depth = 0)
    {
    }
    //This method is no joke.
    public void DrawImage(object image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight, byte depth = 0)
    {

    }
    //Draw using a default font
    public void DrawText(int fontSize, string text, NodeBounds bounds, uint rgba, byte depth)
    {
        //Draw text with default font.
        DrawText(defaultFont, fontSize, text, bounds, rgba, depth);
    }
    //set the rendered size of a text element using the default font.
    public void TextBounds(int fontSize, string text, out int width, out int height)
    {
        TextBounds(defaultFont, fontSize, text, out width, out height);
    }
    public void DrawText(object font, int fontSize, string text, NodeBounds bounds, uint rgba, byte depth)
    {
        
    }
    public void TextBounds(object font, int fontSize, string text, out int width, out int height)
    {
        //For the time being, Voxelesque's text rendering is extremely simplistic - every character is a square.
        // The rendered size of text in pixels is fairly simple to compute.
        
        // This only works for text elements that are one line.
        //width = text.Length*fontSize;
        //height = fontSize;
        width = 0;
        height = 0;
        string[] lines = text.Split('\n');
        height = lines.Length*fontSize;
        foreach(string line in lines)
        {
            int lineWidth = line.Length * fontSize;
            if(lineWidth > width)width = lineWidth;
        }
    }
    //INPUTS AND OUTPUTS
    public int GetMouseX()
    {
        return (int)(IRender.CurrentRender.Mouse().X);
    }
    public int GetMouseY()
    {
        return (int)(IRender.CurrentRender.Mouse().Y);
    }
    public string GetClipboard()
    {
        return IRender.CurrentRender.GetClipboard();
    }
    public void SetClipboard(string clip)
    {
        IRender.CurrentRender.SetClipboard(clip);
    }
    public bool KeyDown(KeyCode key)
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        //special cases for ctrl, shift, and alt.
        switch(key)
        {
            case KeyCode.shift:
                return keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            case KeyCode.ctrl:
                return keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            case KeyCode.alt:
                return keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        }
        return keyboard.IsKeyDown(KeyCodeToKeys(key));
    }
    public IEnumerable<KeyCode> DownKeys()
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if(KeyDown(code))yield return code;
        }
    }
    public bool keyPressed(KeyCode key)
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        //special cases for ctrl, shift, and alt.
        switch(key)
        {
            case KeyCode.shift:
                return keyboard.IsKeyPressed(Keys.LeftShift) || keyboard.IsKeyPressed(Keys.RightShift);
            case KeyCode.ctrl:
                return keyboard.IsKeyPressed(Keys.LeftControl) || keyboard.IsKeyPressed(Keys.RightControl);
            case KeyCode.alt:
                return keyboard.IsKeyPressed(Keys.LeftAlt) || keyboard.IsKeyPressed(Keys.RightAlt);
        }
        return keyboard.IsKeyPressed(KeyCodeToKeys(key));
    }
    public IEnumerable<KeyCode> PressedKeys()
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if(keyPressed(code))yield return code;
        }
    }
    public bool KeyReleased(KeyCode key)
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        //special cases for ctrl, shift, and alt.
        switch(key)
        {
            case KeyCode.shift:
                return keyboard.IsKeyReleased(Keys.LeftShift) || keyboard.IsKeyReleased(Keys.RightShift);
            case KeyCode.ctrl:
                return keyboard.IsKeyReleased(Keys.LeftControl) || keyboard.IsKeyReleased(Keys.RightControl);
            case KeyCode.alt:
                return keyboard.IsKeyReleased(Keys.LeftAlt) || keyboard.IsKeyReleased(Keys.RightAlt);
        }
        return keyboard.IsKeyReleased(KeyCodeToKeys(key));
    }
    public IEnumerable<KeyCode> ReleasedKeys()
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if(KeyReleased(code))yield return code;
        }
    }
    public bool LeftMouseDown()
    {
        return IRender.CurrentRender.Mouse().IsButtonDown(MouseButton.Left);
    }
    public bool LeftMousePressed()
    {
        var mouse = IRender.CurrentRender.Mouse();
        return !mouse.WasButtonDown(MouseButton.Left) && mouse.IsButtonDown(MouseButton.Left);
    }
    public bool LeftMouseReleased()
    {
        var mouse = IRender.CurrentRender.Mouse();
        return mouse.WasButtonDown(MouseButton.Left) && !mouse.IsButtonDown(MouseButton.Left);
    }
    public bool CapsLock()
    {
        return IRender.CurrentRender.Keyboard().IsKeyDown(Keys.CapsLock);
    }
    public bool NumLock()
    {
        return IRender.CurrentRender.Keyboard().IsKeyDown(Keys.NumLock);
    }
    public bool ScrollLock()
    {
        return IRender.CurrentRender.Keyboard().IsKeyDown(Keys.ScrollLock);
    }

    private Keys KeyCodeToKeys(KeyCode key)
    {
        switch(key)
        {
            case KeyCode.backspace: return Keys.Backspace;
            case KeyCode.tab: return Keys.Tab;
            case KeyCode.enter: return Keys.Enter;
            case KeyCode.shift: return Keys.LeftShift;
            case KeyCode.ctrl: return Keys.LeftControl;
            case KeyCode.alt: return Keys.LeftAlt;
            case KeyCode.pauseBreak: return Keys.Pause;
            case KeyCode.caps: return Keys.CapsLock;
            case KeyCode.escape: return Keys.Escape;
            case KeyCode.space: return Keys.Space;
            case KeyCode.pageUp: return Keys.PageUp;
            case KeyCode.pageDown: return Keys.PageDown;
            case KeyCode.end: return Keys.End;
            case KeyCode.home: return Keys.Home;
            case KeyCode.left: return Keys.Left;
            case KeyCode.up: return Keys.Up;
            case KeyCode.right: return Keys.Right;
            case KeyCode.down: return Keys.Down;
            case KeyCode.printScreen: return Keys.PrintScreen;
            case KeyCode.insert: return Keys.Insert;
            case KeyCode.delete: return Keys.Delete;
            case KeyCode.zero: return Keys.D0;
            case KeyCode.one: return Keys.D1;
            case KeyCode.two: return Keys.D2;
            case KeyCode.three: return Keys.D3;
            case KeyCode.four: return Keys.D4;
            case KeyCode.five: return Keys.D5;
            case KeyCode.six: return Keys.D6;
            case KeyCode.seven: return Keys.D7;
            case KeyCode.eight: return Keys.D8;
            case KeyCode.nine: return Keys.D9;
            case KeyCode.a: return Keys.A;
            case KeyCode.b: return Keys.B;
            case KeyCode.c: return Keys.C;
            case KeyCode.d: return Keys.D;
            case KeyCode.e: return Keys.E;
            case KeyCode.f: return Keys.F;
            case KeyCode.g: return Keys.G;
            case KeyCode.h: return Keys.H;
            case KeyCode.i: return Keys.I;
            case KeyCode.j: return Keys.J;
            case KeyCode.k: return Keys.K;
            case KeyCode.l: return Keys.L;
            case KeyCode.m: return Keys.M;
            case KeyCode.n: return Keys.N;
            case KeyCode.o: return Keys.O;
            case KeyCode.p: return Keys.P;
            case KeyCode.q: return Keys.Q;
            case KeyCode.r: return Keys.R;
            case KeyCode.s: return Keys.S;
            case KeyCode.t: return Keys.T;
            case KeyCode.u: return Keys.U;
            case KeyCode.v: return Keys.V;
            case KeyCode.w: return Keys.W;
            case KeyCode.x: return Keys.X;
            case KeyCode.y: return Keys.Y;
            case KeyCode.z: return Keys.Z;
            case KeyCode.superLeft: return Keys.LeftSuper;
            case KeyCode.superRight: return Keys.RightSuper;
            //case KeyCode.select: return Keys.se
            case KeyCode.num0: return Keys.KeyPad0;
            case KeyCode.num1: return Keys.KeyPad1;
            case KeyCode.num2: return Keys.KeyPad2;
            case KeyCode.num3: return Keys.KeyPad3;
            case KeyCode.num4: return Keys.KeyPad4;
            case KeyCode.num5: return Keys.KeyPad5;
            case KeyCode.num6: return Keys.KeyPad6;
            case KeyCode.num7: return Keys.KeyPad7;
            case KeyCode.num8: return Keys.KeyPad8;
            case KeyCode.num9: return Keys.KeyPad9;
            case KeyCode.multiply: return Keys.KeyPadMultiply;
            case KeyCode.add: return Keys.KeyPadAdd;
            case KeyCode.subtract: return Keys.KeyPadSubtract;
            case KeyCode.decimalPoint: return Keys.KeyPadDecimal;
            case KeyCode.divide: return Keys.KeyPadDivide;
            case KeyCode.f1: return Keys.F1;
            case KeyCode.f2: return Keys.F2;
            case KeyCode.f3: return Keys.F3;
            case KeyCode.f4: return Keys.F4;
            case KeyCode.f5: return Keys.F5;
            case KeyCode.f6: return Keys.F6;
            case KeyCode.f7: return Keys.F7;
            case KeyCode.f8: return Keys.F8;
            case KeyCode.f9: return Keys.F9;
            case KeyCode.f10: return Keys.F10;
            case KeyCode.f11: return Keys.F11;
            case KeyCode.f12: return Keys.F12;
            case KeyCode.numLock: return Keys.NumLock;
            case KeyCode.scrollLock: return Keys.ScrollLock;
            //case KeyCode.mute: return Keys.
            //case KeyCode.audioDown: return Keys.
            //case KeyCode.audioUp
            //case KeyCode.media: return Keys.
            //case KeyCode.app1: return Keys.
            //case KeyCode.app2
            case KeyCode.semicolon: return Keys.Semicolon;
            case KeyCode.equals: return Keys.Equal;
            case KeyCode.comma: return Keys.Comma;
            case KeyCode.dash: return Keys.Minus;
            case KeyCode.period: return Keys.Period;
            case KeyCode.slash: return Keys.Slash;
            case KeyCode.grave: return Keys.GraveAccent;
            case KeyCode.bracketLeft: return Keys.LeftBracket;
            case KeyCode.backSlash: return Keys.Backslash;
            case KeyCode.bracketRight: return Keys.RightBracket;
            case KeyCode.quote: return Keys.Apostrophe;
            default: return 0; //I would return Keys.Unknown, however OpenTK doesn't handle that case.
        }
    }
}