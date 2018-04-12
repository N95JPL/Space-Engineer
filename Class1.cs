using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        class Graphics
        {
            private List<IMyTextPanel> Panels;
            private string[] Screen;
            private string[] ClearScreen;
            private string[] ScreenLines;
            public int Width { get; private set; }
            public int Height { get; private set; }
            private string[] Foreground = { "\uE2FF", "\uE2FF" }; //White
            private string Background = "\uE100"; //Black
            private int[] clip = new int[4];
            Action<string> Echo;
            private Random Rand;

            #region Ascii
            private const int Offset = 0x21;
            // binary numbers represent bitmap glyphs, three bits to a line
            // eg 9346 == 010 010 010 000 010 == !
            //    5265 == 001 010 010 010 001 == (
            private short[] Glyphs = {
            9346, 23040, 24445, 15602,
            17057, 10923, 9216, 5265,
            17556, 21824, 1488, 20,
            448, 2, 672, 31599,
            11415, 25255, 29326, 23497,
            31118, 10666, 29330, 10922,
            10954, 1040, 1044, 5393,
            3640, 17492, 25218, 15203,
            11245, 27566, 14627, 27502,
            31143, 31140, 14827, 23533,
            29847, 12906, 23469, 18727,
            24557, 27501, 11114, 27556,
            11131, 27565, 14478, 29842,
            23403, 23378, 23549, 23213,
            23186, 29351, 13459, 2184,
            25750, 10752, 7, 17408,
            239, 18862, 227, 4843,
            1395, 14756, 1886, 18861,
            8595, 4302, 18805, 25745,
            509, 429, 170, 1396,
            1369, 228, 1934, 18851,
            363, 362, 383, 341,
            2766, 3671, 5521, 9234,
            17620, 1920
            };
            private short GetGlyph(char code)
            {
                return Glyphs[code - Offset];
            }
            #endregion

            public Graphics(int width, int height, List<IMyTextPanel> panels, Action<string> echo)
            {
                Width = width;
                Height = height;
                Screen = new string[Width * Height];
                ClearScreen = new string[Width * Height];
                ScreenLines = new string[Width * Height + Height - 1];
                Panels = panels;
                Echo = echo;
                SetBackground(Background, true);
                Rand = new Random();
                Clear();
            }
            public Graphics(int width, int height, IMyTextPanel panel, Action<string> echo)
            {
                Width = width;
                Height = height;
                Screen = new string[Width * Height];
                ClearScreen = new string[Width * Height];
                ScreenLines = new string[Width * Height + Height - 1];
                Panels = new List<IMyTextPanel>();
                Panels.Add(panel);
                Echo = echo;
                SetBackground(Background, true);
                Rand = new Random();
                Clear();
            }

            public void Pixel(int x, int y)
            {
                if (Within(x, 0, Width) && Within(y, 0, Height))
                    Screen[y * Width + x] = Foreground[0];
            }
            public void Draw()
            {
                for (int i = 0; i < Height; i++)
                {
                    ScreenLines[i] = string.Join(null, Screen, i * Width, Width) + "\n";
                }

                string combinedFrame = string.Concat(ScreenLines);

                foreach (var panel in Panels)
                {
                    panel.WritePublicText(combinedFrame);
                }
            }
            public void Line(int x0, int y0, int x1, int y1)
            {
                if (x0 == x1)
                {
                    int high = Math.Max(y1, y0);
                    for (int y = Math.Min(y1, y0); y <= high; y++)
                    {
                        Pixel(x0, y);
                    }
                }
                else if (y0 == y1)
                {
                    int high = Math.Max(x1, x0);
                    for (int x = Math.Min(x1, x0); x <= high; x++)
                    {
                        Pixel(x, y0);
                    }
                }
                else
                {
                    bool yLonger = false;
                    int incrementVal, endVal;
                    int shortLen = y1 - y0;
                    int longLen = x1 - x0;
                    if (Math.Abs(shortLen) > Math.Abs(longLen))
                    {
                        int swap = shortLen;
                        shortLen = longLen;
                        longLen = swap;
                        yLonger = true;
                    }
                    endVal = longLen;
                    if (longLen < 0)
                    {
                        incrementVal = -1;
                        longLen = -longLen;
                    }
                    else incrementVal = 1;
                    int decInc;
                    if (longLen == 0) decInc = 0;
                    else decInc = (shortLen << 16) / longLen;
                    int j = 0;
                    if (yLonger)
                    {
                        for (int i = 0; i - incrementVal != endVal; i += incrementVal)
                        {
                            Pixel(x0 + (j >> 16), y0 + i);
                            j += decInc;
                        }
                    }
                    else
                    {
                        for (int i = 0; i - incrementVal != endVal; i += incrementVal)
                        {
                            Pixel(x0 + i, y0 + (j >> 16));
                            j += decInc;
                        }
                    }
                }
            }
            public void Rect(int x, int y, int w, int h, bool fill = false)
            {
                if (!fill)
                {
                    Line(x, y, x, y + h - 1);
                    Line(x, y, x + w - 1, y);
                    Line(x + w - 1, y, x + w - 1, y + h - 1);
                    Line(x, y + h - 1, x + w - 1, y + h - 1);
                }
                else
                {
                    for (int xi = x; xi < x + w; xi++)
                    {
                        for (int yi = y; yi < y + h; yi++)
                        {
                            Pixel(xi, yi);
                        }
                    }
                }
            }
            private void flatBottom(int x1, int y1, int x2, int y2, int x3, int y3)
            {
                float invslope1 = (float)(x2 - x1) / (y2 - y1);
                float invslope2 = (float)(x3 - x1) / (y3 - y1);
                float curx1 = x1;
                float curx2 = x1;
                for (int scanlineY = y1; scanlineY <= y2; scanlineY++)
                {
                    line((int)curx1, scanlineY, (int)curx2, scanlineY);
                    curx1 += invslope1;
                    curx2 += invslope2;
                }
            }
            private void flatTop(int x1, int y1, int x2, int y2, int x3, int y3)
            {
                float invslope1 = (float)(x3 - x1) / (y3 - y1);
                float invslope2 = (float)(x3 - x2) / (y3 - y2);
                float curx1 = x3;
                float curx2 = x3;
                for (int scanlineY = y3; scanlineY > y1; scanlineY--)
                {
                    curx1 -= invslope1;
                    curx2 -= invslope2;
                    line((int)curx1, scanlineY, (int)curx2, scanlineY);
                }
            }
            private void swap(ref int a, ref int b)
            {
                int c = a;
                a = b;
                b = c;
            }
            public void tri(string m, int x1, int y1, int x2, int y2, int x3, int y3)
            {
                if (m == "line")
                {
                    line(x1, y1, x2, y2);
                    line(x2, y2, x3, y3);
                    line(x3, y3, x1, y1);
                }
                else if (m == "fill")
                {
                    if (y1 > y3)
                    {
                        swap(ref y1, ref y3);
                        swap(ref x1, ref x3);
                    }
                    if (y1 > y2)
                    {
                        swap(ref y1, ref y2);
                        swap(ref x1, ref x2);
                    }
                    if (y2 > y3)
                    {
                        swap(ref y2, ref y3);
                        swap(ref x2, ref x3);
                    }
                    if (y2 == y3)
                    {
                        flatBottom(x1, y1, x2, y2, x3, y3);
                    }
                    else if (y1 == y2)
                    {
                        flatTop(x1, y1, x2, y2, x3, y3);
                    }
                    else
                    {
                        int x4 = (int)(x1 + ((float)(y2 - y1) / (float)(y3 - y1)) * (x3 - x1));
                        flatBottom(x1, y1, x2, y2, x4, y2);
                        flatTop(x2, y2, x4, y2, x3, y3);
                    }
                }
            }
            public void ellipse(string m, int cx, int cy, int rx, int ry)
            {
                int rx2 = rx * rx;
                int ry2 = ry * ry;
                if (m == "fill")
                {
                    int rxsys = rx2 * ry2;
                    pixel(cx, cy);
                    for (int i = 1; i < rx * ry; i++)
                    {
                        int x = i % rx;
                        int y = i / rx;
                        if (ry2 * x * x + rx2 * y * y <= rxsys)
                        {
                            pixel(cx + x, cy + y);
                            pixel(cx - x, cy - y);
                            //if (x && y) { //unnecessary (prevents overdrawing pixels)
                            pixel(cx + x, cy - y);
                            pixel(cx - x, cy + y);
                            //}
                        }
                    }
                }
                else if (m == "line")
                {
                    int frx2 = 4 * rx2;
                    int fry2 = 4 * ry2;
                    int s = 2 * ry2 + rx2 * (1 - 2 * ry);
                    int y = ry;
                    for (int x = 0; ry2 * x <= rx2 * y; x++)
                    {
                        pixel(cx + x, cy + y);
                        pixel(cx - x, cy + y);
                        pixel(cx + x, cy - y);
                        pixel(cx - x, cy - y);
                        if (s >= 0)
                        {
                            s += frx2 * (1 - y);
                            y--;
                        }
                        s += ry2 * ((4 * x) + 6);
                    }
                    y = 0;
                    s = 2 * rx2 + ry2 * (1 - 2 * rx);
                    for (int x = rx; rx2 * y <= ry2 * x; y++)
                    {
                        pixel(cx + x, cy + y);
                        pixel(cx - x, cy + y);
                        pixel(cx + x, cy - y);
                        pixel(cx - x, cy - y);
                        if (s >= 0)
                        {
                            s += fry2 * (1 - x);
                            x--;
                        }
                        s += rx2 * ((4 * y) + 6);
                    }
                }
            }
            public void circle(string m, int cx, int cy, int r)
            {
                if (m == "fill")
                {
                    int rr = r * r;
                    pixel(cx, cy);
                    for (int i = 1; i < r * r; i++)
                    {
                        int x = i % r;
                        int y = i / r;
                        if (x * x + y * y < rr)
                        {
                            pixel(cx + x, cy + y);
                            pixel(cx - x, cy - y);
                            if (x > 0 && y > 0)
                            {
                                pixel(cx + x, cy - y);
                                pixel(cx - x, cy + y);
                            }
                        }
                    }
                }
                else if (m == "line")
                {
                    int x = r;
                    int y = 0;
                    int do2 = 1 - x;
                    while (y <= x)
                    {
                        pixel(cx + x, cy + y);
                        pixel(cx + y, cy + x);
                        pixel(cx - x, cy + y);
                        pixel(cx - y, cy + x);
                        pixel(cx - x, cy - y);
                        pixel(cx - y, cy - x);
                        pixel(cx + x, cy - y);
                        pixel(cx + y, cy - x);
                        y++;
                        if (do2 <= 0)
                        {
                            do2 += 2 * y + 1;
                        }
                        else
                        {
                            do2 += 2 * (y - --x) + 1;
                        }
                    }
                }
            }
            public void mask(int x1, int y1, int x2, int y2)
            {
                clip[0] = x1;
                clip[1] = y1;
                clip[2] = x2;
                clip[3] = y2;
            }
            public void mask()
            {
                clip[0] = 0;
                clip[1] = 0;
                clip[2] = width - 1;
                clip[3] = height - 1;
            }
            public void Print(int x, int y, string text, Align align = Align.Left)
            {
                y += 4; //Offset so that y represents the top of the text, like the shapes.

                if (align == Align.Right) x -= text.Length * 4 - 1;
                if (align == Align.Center) x -= (int)(text.Length * 4 - 1) / 2;

                int x1 = x;
                int y1 = y;
                for (int i = 0; i < text.Length; i++)
                {
                    switch (text[i])
                    {
                        case '\n':
                            y1 += 6;
                            x1 = x;
                            break;
                        case ' ':
                            x1 += 4;
                            break;
                        default:
                            short glyph = GetGlyph(text[i]);
                            int j = 14;
                            do
                            {
                                if ((glyph & 1) != 0)
                                {
                                    Pixel(x1 + j % 3, y1 - 4 + j / 3);
                                }
                                glyph >>= 1;
                                j--;
                            } while (glyph > 0);
                            x1 += 4;
                            break;
                    }
                }
            }
            public void centerText(string input, colour colour, int y)
            {
                int WordLength = 0;
                foreach (char c in input)
                {
                    WordLength += 4;
                }
                int textPosition = (width / 2) - (WordLength / 2);
                setForeground(colour);
                print(textPosition, y, input);
            }
            public void titleText(string input, colour TextColour, colour BoxColour, int y)
            {
                int WordLength = 0;
                foreach (char c in input)
                {
                    WordLength += 4;
                }
                int textPosition = (width / 2) - (WordLength / 2);
                setForeground(BoxColour);
                rect("line", textPosition - 2, y - 2, WordLength + 3, 9);
                setForeground(TextColour);
                print(textPosition, y, input.ToUpper());
            }
            public void systemTime(colour TextColour, colour BoxColour)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                int WordLength = 0;
                foreach (char c in time)
                {
                    WordLength += 4;
                }
                int textPositionX = (width / 2) - (WordLength / 2);
                int textPositionY = (height - 10);
                setForeground(BoxColour);
                rect("line", textPositionX - 2, textPositionY - 2, WordLength + 3, 9);
                setForeground(TextColour);
                print(textPositionX, textPositionY, time);
            }
            public void FillBar(string name, string Ori, int x, int y, int width, int height, int MaxValue, int FillValue, int Warning, colour BarColour, colour FillColour, colour TextColour)
            {
                if (Ori == "Vertical")
                {
                    setForeground(BarColour);
                    rect(x, y, width, height, false);
                    float filla = ((float)FillValue / (float)MaxValue);
                    int fill = ((y + height + 1) - (int)(height * filla));
                    int WordLength = 0;
                    foreach (char c in name)
                    {
                        WordLength += 4;
                    }
                    int textPosition = ((x + (width / 2)) - (WordLength / 2));
                    print(textPosition, (y - 6), name;
                    if (FillValue == 0)
                    {
                        if (ErrorFlash)
                        {
                            mask(x + 1, y + 1, x + width - 2, height + y - 2);
                            ErrorFlash = false;
                        }
                        else if (!ErrorFlash)
                        {
                            mask(x + 1, fill, x + width - 2, height + y - 2);
                            ErrorFlash = true;
                        }
                    }
                    else 
                    { 
                        mask(x + 1, fill, x + width - 2, height + y - 2); }
                    if (filla / 100 < Warning)
                    {
                        FillColour = (Color.Red);
                        TextColour = (Color.Red);
                    }
                    SetForeground(FillColour);
                    rect(x + 1, y + 1, width - 2, height - 2,true);
                    mask();
                    setForeground(TextColour);
                    int CharY = 0;
                    foreach (char c in FillValue.ToString())
                    {
                        print(x + (width / 2) - 2, y + 2 + CharY, c.ToString());
                        CharY += 6;
                    }
                }
            }
            public void Clear()
            {
                Screen = (string[])ClearScreen.Clone();
            }

            public void SetForeground(string color, bool log = true)
            {
                if (log) Foreground[1] = Foreground[0];
                Foreground[0] = color;
            }
            public void SetForeground(Color color, bool log = true)
            {
                if (log) Foreground[1] = Foreground[0];
                Foreground[0] = GetColorString(color);
            }
            public void SetPreviousForeground()
            {
                Foreground[0] = Foreground[1];
            }

            public void SetBackground(string color, bool forceUpdate = false)
            {
                if (Background != color || forceUpdate)
                {
                    Background = color;
                    for (int i = 0; i < ClearScreen.Length; i++)
                        ClearScreen[i] = Background;
                }
            }
            public void SetBackground(Color color, bool forceUpdate = false)
            {
                string stringColor = GetColorString(color);
                if (Background != stringColor || forceUpdate)
                {
                    Background = stringColor;
                    for (int i = 0; i < ClearScreen.Length; i++)
                        ClearScreen[i] = Background;
                }
            }

            public bool Within(double val, double min, double max)
            {
                if (val < max && val >= min) return true;
                return false;
            }
            private int Map(int x, int in_min, int in_max, int out_min, int out_max)
            {
                return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
            }
            //get a color char (byte = number from 0-255)
            private char GetColorChar(byte r, byte g, byte b)
            {
                return (char)(0xe100 + (r << 6) + (g << 3) + b);
            }
            private string GetColorString(Color color)
            {
                return ((char)((0xe100 + (Map(color.R, 0, 255, 0, 7) << 6) + (Map(color.G, 0, 255, 0, 7) << 3) + Map(color.B, 0, 255, 0, 7)))).ToString();
            }
        }
        public enum Align { Left, Center, Right };

// Vars 
        string Ship = "Beluga Lifter";
        const string LCDs = "Control Centre LCD";
        int width = 132;
        int height = 89;
        int counter = 0;
        int currentAltitude = 0;
        int currentFuel = 100;

        Graphics G;
        IMyTextPanel LCD;
        public Program()
        {
            LCD = GridTerminalSystem.GetBlockWithName(LCDs) as IMyTextPanel;
            LCD.FontSize = 0.2f;
            LCD.FontColor = Color.White;
            LCD.BackgroundColor = Color.Black;
            LCD.Font = "DotMatrix";
            LCD.ShowPublicTextOnScreen();
            G = new Graphics(width, height, LCD, Echo); //width in pixels, height in pixels, lcd panel, The echo method for debugging.
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }

        public void Main(string arg)
        {
            if (arg == "Reset") { currentAltitude = 0; counter = 0; currentFuel = 100; }
            G.echo("Counter: " + counter);
            G.clear();
            G.setBackground(Color.Black);
            G.setForeground(Color.Red);
            G.rect("line", 0, 0, 131, 88, false); //LCD Boarder     

            G.titleText(Ship, Color.Blue, Color.Blue, 4); //Title

            G.FillBar("Alt", "Vertical", 4, 10, 12, 76, 10000, currentAltitude, 10, Color.Blue, Color.Green, Color.Orange);

            G.FillBar("Fuel", "Vertical", 115, 10, 12, 76, 100, currentFuel, 25, Color.Blue, Color.Green, Color.Orange);

            G.systemTime(Color.Orange, Color.Blue);

            G.paint();

            if (counter < 100)
            {
                counter += 1;
                currentAltitude += 100;
                currentFuel -= 1;
            }
            G.Draw();
        }
    }

}
