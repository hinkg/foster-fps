#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Numerics;
using Foster.Framework;

namespace FosterTest;

public static class GUI
{
    public static Batcher Batch;
    private static SpriteFont font;

    public static Vector2 pressedPosition;
    public static Vector2 releasedPosition;
    private static Rect viewport;

    public static bool AnyHovered   { private set; get; }
    public static bool AnyTriggered { private set; get; }
    public static bool AnyActive    { private set; get; }
    public static bool AnyInteraction => AnyHovered | AnyTriggered | AnyActive;

    public static void Init(SpriteFont inFont)
    {
        Batch = new();
        Batch.Clear();

        font = inFont;
    }

    public static void NewFrame()
    {
        AnyHovered = false;
        AnyTriggered = false;
        AnyActive = false;

        if (Input.Mouse.LeftPressed) pressedPosition = Input.Mouse.Position;
        if (Input.Mouse.LeftReleased) releasedPosition = Input.Mouse.Position;

        viewport = new Rect(0.0f, 0.0f, App.WidthInPixels, App.HeightInPixels);

        Batch.Clear();
    }

    public static void Render()
    {
        Batch.Render();
    }

    //
    // Widgets
    //

    public static bool Button(Rect rect, ReadOnlySpan<char> text)
    {
        var hovered = rect.Hovered();
        var active = rect.Active();

        Batch.RectRoundedLine(rect, 2.0f, 1.0f, Color.White);

        if (active) Batch.Rect(rect.Inflate(-2.0f), Color.White);

        Batch.Text(font, text, AlignCenter(rect, TextRect(font, text)).Position, active ? Color.Black : Color.White);

        return rect.Triggered();
    }

    public static bool HueWheel(Rect rect, ref float hue)
    {
        float prevHue = hue;
        float v = (Input.Mouse.Position.Clamp(rect).X - rect.Position.X) / rect.Width;

        if (rect.Active())
        {
            hue = Math.Clamp(v, 0.0f, 1.0f);
        }

        var rc = rect.Inflate(-2.0f);

        // Hue gradient, not accurate but w/e doesnt matter
        int steps = (int)rc.Width / 6 + 1;

        for (int i = 0; i < steps; i++)
        {
            float v0 = (float)(i + 0) / steps * 360;
            float v1 = (float)(i + 1) / steps * 360;

            Color cl = ColorFromHSV(v0, 1.0f, 1.0f);
            Color cr = ColorFromHSV(v1, 1.0f, 1.0f);

            float w = (i < steps - 1) ? 6.0f : rc.Width;
            Batch.Rect(rc.CutLeft(w), cl, cr, cr, cl);
        }

        // Border
        Batch.RectRoundedLine(rect, 2.0f, 1.0f, Color.White);

        // Pointer

        rc = rect.Inflate(-2.0f);
        var pointerX = (int)(rc.Position.X + rc.Width * hue);

        // var currentColor = ColorFromHSV(hue * 360, 0.3f, 1.0f);
        // var pos = new Vector2(pointerX, rc.Position.Y);
        // float oy = 3.0f; // offset y
        //Batch.Triangle(pos + new Vector2(0, +2 + oy), pos + new Vector2(-4, -5 + oy), pos + new Vector2(+5, -5 + oy), Color.Black);
        //Batch.Triangle(pos + new Vector2(0, -0 + oy), pos + new Vector2(-3, -5 + oy), pos + new Vector2(+4, -5 + oy), currentColor);

        Batch.Rect(new Rect(pointerX - 1, rc.Y, 3.0f, rc.Height), Color.Black);
        Batch.Rect(new Rect(pointerX, rc.Y, 1.0f, rc.Height), Color.White);

        return prevHue != hue;
    }

    public static bool RadioInline<T>(Rect rect, ref T en) where T : Enum
    {
        var numItems = Enum.GetValues(typeof(T)).Length;

        var r0 = rect;
        Batch.RectRoundedLine(r0, 2.0f, 1.0f, Color.White);

        foreach (int item in Enum.GetValues(typeof(T)))
        {
            var r1 = r0.CutLeft(rect.Width / numItems);

            if (r1.Triggered()) en = (T)Enum.ToObject(typeof(T), item); ;

            var rt = r1.AlignCenter(TextRect(font, Enum.GetName(typeof(T), item))).Position;
            var eq = (int)(object)en == item;

            if (eq)
            {
                Batch.Rect(r1.Inflate(-2.0f, -2.0f, -2.0f, -2.0f), Color.White);
                Batch.Text(font, Enum.GetName(typeof(T), item), rt, Color.Black);
            }
            else
            {
                //if (item != numItems - 1) batch.Rect(new Rect(r1.RightLine.From, r1.RightLine.To + Vector2.UnitX), Color.White);
                Batch.Text(font, Enum.GetName(typeof(T), item), rt, Color.White);
            }
        }

        return false;
    }

    public static bool Slider(Rect rect, ReadOnlySpan<char> text, ref float value, float min, float max)
    {
        float prevValue = value;
        float alpha = (value - min) / (max - min);

        if (rect.Active())
        {
            float new_alpha = (Input.Mouse.Position.Clamp(rect).X - rect.Position.X) / rect.Width;
            float v = new_alpha * (max - min) + min;
            value = Math.Clamp(v, min, max);
        }

        var rc = rect.Inflate(-2.0f);
        var rt = rc.AlignCenter(TextRect(font, text));

        Batch.Text(font, text, rt.Position, Color.White);

        Batch.PushScissor(new RectInt((int)rc.X, (int)rc.Y, (int)(rc.Width * alpha), (int)rc.Height));
        Batch.Rect(rc, Color.White);
        Batch.Text(font, text, rt.Position, Color.Black);
        Batch.PopScissor();

        // Border
        Batch.RectRoundedLine(rect, 2.0f, 1.0f, Color.White);

        return prevValue != value;
    }

    public static bool Slider(Rect rect, ReadOnlySpan<char> text, ref int value, int min, int max)
    {
        int prevValue = value;
        float alpha = (float)(value - min) / (max - min);

        if (rect.Active())
        {
            float new_alpha = (Input.Mouse.Position.Clamp(rect).X - rect.Position.X) / rect.Width;
            int v = (int)(new_alpha * (max - min) + min);
            value = Math.Clamp(v, min, max);
        }

        var rc = rect.Inflate(-2.0f);
        var rt = rc.AlignCenter(TextRect(font, text));

        Batch.Text(font, text, rt.Position, Color.White);

        Batch.PushScissor(new RectInt((int)rc.X, (int)rc.Y, (int)(rc.Width * alpha), (int)rc.Height));
        Batch.Rect(rc, Color.White);
        Batch.Text(font, text, rt.Position, Color.Black);
        Batch.PopScissor();

        // Border
        Batch.RectRoundedLine(rect, 2.0f, 1.0f, Color.White);

        return prevValue != value;
    }

    public static void TextLine(ref Rect rect, ReadOnlySpan<char> text)
    {
        var r0 = rect.CutTop(18.0f);
        Batch.Text(font, text, r0.Position, Color.White);
    }

    //
    // Rect Interaction
    //

    public static bool Active(this Rect rect)
    {
        bool v = rect.Contains(pressedPosition) && Input.Mouse.LeftDown;
        AnyActive |= v;
        return v;
    }

    public static bool Triggered(this Rect rect)
    {
        bool v = rect.Contains(pressedPosition) && rect.Contains(releasedPosition) && Input.Mouse.LeftReleased;
        AnyTriggered |= v;
        return v;
    }

    public static bool Hovered(this Rect rect)
    {
        bool v = rect.Contains(Input.Mouse.Position);
        AnyHovered |= v;
        return v;
    }

    //
    // Rect Layout
    //

    public static Rect Viewport => viewport;

    public static Rect CutLeft(this ref Rect rect, float w)
    {
        var n = new Rect(rect.X, rect.Y, w, rect.Height);
        rect = new Rect(rect.X + w, rect.Y, rect.Width - w, rect.Height);
        return n;
    }

    public static Rect CutRight(this ref Rect rect, float w)
    {
        var n = new Rect(rect.X + rect.Width - w, rect.Y, w, rect.Height);
        rect = new Rect(rect.X, rect.Y, rect.Width - w, rect.Height);
        return n;
    }

    public static Rect CutTop(this ref Rect rect, float h)
    {
        var n = new Rect(rect.X, rect.Y, rect.Width, h);
        rect = new Rect(rect.X, rect.Y + h, rect.Width, rect.Height - h);
        return n;
    }

    public static Rect CutBottom(this ref Rect rect, float h)
    {
        var n = new Rect(rect.X, rect.Y + rect.Height - h, rect.Width, h);
        rect = new Rect(rect.X, rect.Y, rect.Width, rect.Height - h);
        return n;
    }

    public static Rect GetLeft(this Rect rect, float w)
    {
        return new Rect(rect.X, rect.Y, w, rect.Height);
    }

    public static Rect GetRight(this Rect rect, float w)
    {
        return new Rect(rect.X + rect.Width - w, rect.Y, w, rect.Height);
    }

    public static Rect GetTop(this Rect rect, float h)
    {
        return new Rect(rect.X, rect.Y, rect.Width, h);
    }

    public static Rect GetBottom(this Rect rect, float h)
    {
        return new Rect(rect.X, rect.Y + rect.Height - h, rect.Width, h);
    }

    public static Rect AlignCenter(this Rect container, Rect rect)
    {
        return new Rect(container.TopLeft + (container.Size - rect.Size) / 2, container.BottomRight - (container.Size - rect.Size) / 2);
    }

    public static Rect TextRect(SpriteFont font, ReadOnlySpan<char> text)
    {
        Vector2 size = font.SizeOf(text);
        return new Rect(0, 0, size.X, size.Y);
    }

    //
    // Utility
    //

    public static Color ColorFromHSV(float hue, float saturation, float value)
    {
        int hi = Convert.ToInt32(MathF.Floor(hue / 60)) % 6;
        float f = hue / 60 - MathF.Floor(hue / 60);

        value *= 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return new Color((byte)v, (byte)t, (byte)p, 255);
        else if (hi == 1)
            return new Color((byte)q, (byte)v, (byte)p, 255);
        else if (hi == 2)
            return new Color((byte)p, (byte)v, (byte)t, 255);
        else if (hi == 3)
            return new Color((byte)p, (byte)q, (byte)v, 255);
        else if (hi == 4)
            return new Color((byte)t, (byte)p, (byte)v, 255);
        else
            return new Color((byte)v, (byte)p, (byte)q, 255);
    }
}