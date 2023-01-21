using VRender;
using VRender.Util;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using BasicGUI;

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
    }
    public RenderFont defaultFont;

    private List<IRenderTextEntity?> texts = new List<IRenderTextEntity?>();
    int textIndex = 0;
    public void BeginFrame()
    {
        textIndex = 0;
        //scale the texts into nothing, so they don't reside in view if they stop being rendered/overriden.
        foreach(IRenderTextEntity? text in texts)
        {
            if(text is not null)text.Scale = Vector3.Zero;
        }
    }
    public void EndFrame()
    {
    }
    public void DrawPixel(int x, int y, uint rgb, byte depth = 0)
    {
        IRender.CurrentRender.WritePixelDirect(rgb, x, y);
    }
    public void FillRect(int x0, int y0, int x1, int y1, uint rgb, byte depth = 0)
    {
        if(x0 > x1)
        {
            int temp = x0;
            x0 = x1;
            x1 = temp;
        }
        if(y0 > y1)
        {
            int temp = y0;
            y0 = y1;
            y1 = temp;
        }
        
        for(int xi=x0; xi<x1; xi++)
        {
            for(int yi=y0; yi<y1; yi++)
            {
                DrawPixel(xi, yi, rgb);
            }
        }
    }
    //These were copied and translated from Wikipedia, of all places: https://en.wikipedia.org/wiki/Bresenham's_line_algorithm
    // Stackoverflow doesn't have ALL the answers.
    public void DrawLine(int x1, int y1, int x2, int y2, uint rgb, byte depth = 0)
    {
        if(int.Abs(y2-y1) < int.Abs(x2-x1))
        {
            if(x1 > x2)
                DrawLineLow(x2, y2, x1, y1, rgb);
            else
                DrawLineLow(x1, y1, x2, y2, rgb);
        }
        else
        {
            if(y1 > y2)
                DrawLineHigh(x2, y2, x1, y1, rgb);
            else
                DrawLineHigh(x1, y1, x2, y2, rgb);
        }
    }

    private void DrawLineLow(int x0, int y0, int x1, int y1, uint rgb)
    {
        int dx = x1-x0;
        int dy = y1-y0;
        int yi = 1;
        if(dy < 0)
        {
            yi = -1;
            dy = -dy;
        }
        int D = (2*dy) - dx;
        int y = y0;

        for(int x=x0; x<x1; x++)
        {
            DrawPixel(x, y, rgb);
            if(D > 0)
            {
                y += yi;
                D += (2 * (dy - dx));
            }
            else
            {
                D += 2*dy;
            }
        }
    }

    private void DrawLineHigh(int x0, int y0, int x1, int y1, uint rgb)
    {
        int dx = x1-x0;
        int dy = y1-y0;
        int xi = 1;
        if(dy < 0)
        {
            xi = -1;
            dx = -dx;
        }
        int D = (2*dx) - dy;
        int x = x0;

        for(int y=y0; y<y1; y++)
        {
            DrawPixel(x, y, rgb);
            if(D > 0)
            {
                x += xi;
                D += (2 * (dx - dy));
            }
            else
            {
                D += 2*dx;
            }
        }
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
        RenderImage renderImage = (RenderImage)image;
        IRender.CurrentRender.DrawTextureDirect(renderImage, x, y, (int)renderImage.width, (int)renderImage.height, 0, 0, (int)renderImage.width, (int)renderImage.height);
    }
    //This method is no joke.
    public void DrawImage(object image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight, byte depth = 0)
    {
        RenderImage renderImage = (RenderImage)image;
        IRender.CurrentRender.DrawTextureDirect(renderImage, x, y, width, height, srcx, srcy, srcwidth, srcheight);
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
        //For the time being, Voxelesque's text rendering is extremely simplistic - every character is a square.
        // The rendered size of text in pixels is fairly simple to compute.
        width = text.Length*fontSize;
        height = fontSize;
    }
    public void DrawText(object font, int fontSize, string text, NodeBounds bounds, uint rgba, byte depth)
    {
        RenderFont rFont = (RenderFont) font;
        IRender render = IRender.CurrentRender;
        Vector2 size = render.WindowSize();
        //This sure took a LOOONG time.
        // I eventually gave up doing it in my head and made a Desmos graph to help me out.
        // here is the link if you're curious:https://www.desmos.com/calculator/gezhhwxq3y
        Vector3 scale = new Vector3(1/size.X, 1/size.Y, 0);
        //find the location
        Vector3 location = new Vector3(
            2*scale.X*(bounds.X??0)-1,
            -2*scale.Y*(bounds.Y??0)+1,
            0
        );
        //and the actual scale
        Vector3 renderScale = new Vector3(
            2*fontSize*scale.X,
            2*fontSize*scale.Y,
            1
        );
        //put all that into an EntityPosition for the IRender.
        EntityPosition pos = new EntityPosition(
            location,
            Vector3.Zero,
            renderScale
        );
        if(texts.Count == textIndex)texts.Add(null);
        if(texts[textIndex] is not null)
        {
            //override an existing text element
            // Manually disable nullables because the compiler isn't smart enough to see that it's not null.
            #nullable disable
            texts[textIndex].Text = text;
            texts[textIndex].Position = pos;
            #nullable enable
        }
        else
        {
            texts[textIndex] = render.SpawnTextEntity(pos, text, false, false, rFont.shader, rFont.texture, false, null);
        }

        textIndex++;
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