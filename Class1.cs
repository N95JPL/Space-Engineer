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
        public class Graphics
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
            private bool ErrorFlash = false;
            Action<string> Echo;
            private Random Rand;
            NumberFormatInfo percentFormat = new NumberFormatInfo {PercentPositivePattern = 1, PercentNegativePattern = 1 };

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
                    Line((int)curx1, scanlineY, (int)curx2, scanlineY);
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
                    Line((int)curx1, scanlineY, (int)curx2, scanlineY);
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
                    Line(x1, y1, x2, y2);
                    Line(x2, y2, x3, y3);
                    Line(x3, y3, x1, y1);
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
                    Pixel(cx, cy);
                    for (int i = 1; i < rx * ry; i++)
                    {
                        int x = i % rx;
                        int y = i / rx;
                        if (ry2 * x * x + rx2 * y * y <= rxsys)
                        {
                            Pixel(cx + x, cy + y);
                            Pixel(cx - x, cy - y);
                            //if (x && y) { //unnecessary (prevents overdrawing pixels)
                            Pixel(cx + x, cy - y);
                            Pixel(cx - x, cy + y);
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
                        Pixel(cx + x, cy + y);
                        Pixel(cx - x, cy + y);
                        Pixel(cx + x, cy - y);
                        Pixel(cx - x, cy - y);
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
                        Pixel(cx + x, cy + y);
                        Pixel(cx - x, cy + y);
                        Pixel(cx + x, cy - y);
                        Pixel(cx - x, cy - y);
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
                    Pixel(cx, cy);
                    for (int i = 1; i < r * r; i++)
                    {
                        int x = i % r;
                        int y = i / r;
                        if (x * x + y * y < rr)
                        {
                            Pixel(cx + x, cy + y);
                            Pixel(cx - x, cy - y);
                            if (x > 0 && y > 0)
                            {
                                Pixel(cx + x, cy - y);
                                Pixel(cx - x, cy + y);
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
                        Pixel(cx + x, cy + y);
                        Pixel(cx + y, cy + x);
                        Pixel(cx - x, cy + y);
                        Pixel(cx - y, cy + x);
                        Pixel(cx - x, cy - y);
                        Pixel(cx - y, cy - x);
                        Pixel(cx + x, cy - y);
                        Pixel(cx + y, cy - x);
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
                clip[2] = Width - 1;
                clip[3] = Height - 1;
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
            public void centerText(string input, Color colour, int y)
            {
                int WordLength = 0;
                foreach (char c in input)
                {
                    WordLength += 4;
                }
                int textPosition = (Width / 2) - (WordLength / 2);
                SetForeground(colour);
                Print(textPosition, y, input);
            }
            public void titleText(string input, Color TextColour, Color BoxColour, int y)
            {
                int WordLength = 0;
                foreach (char c in input)
                {
                    WordLength += 4;
                }
                int textPosition = (Width / 2) - (WordLength / 2);
                SetForeground(BoxColour);
                Rect(textPosition - 2, y - 2, WordLength + 3, 9, false);
                SetForeground(TextColour);
                Print(textPosition, y, input.ToUpper());
            }
            public void systemTime(Color TextColour, Color BoxColour)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                int WordLength = 0;
                foreach (char c in time)
                {
                    WordLength += 4;
                }
                int textPositionX = (Width / 2) - (WordLength / 2);
                int textPositionY = (Height - 10);
                SetForeground(BoxColour);
                Rect(textPositionX - 2, textPositionY - 2, WordLength + 3, 9);
                SetForeground(TextColour);
                Print(textPositionX, textPositionY, time);
            }
            public void FillBar(string name, string Ori, int x, int y, int width, int height, int MaxValue, int MinValue, int FillValue, string value, string Warning, Color BarColour, Color FillColour, Color TextColour)
            {
                if (Ori == "Vertical")
                {
                    SetForeground(BarColour);
                    Rect(x, y, width, height, false);
                    //float filla = (FillValue / (floatMaxValue);
                    int fill = Map(FillValue,0,MaxValue,0,height-2);
                    int WordLength = 0;
                    foreach (char c in name)
                    {
                        WordLength += 4;
                    }
                    int textPosition = ((x + (width / 2)) - (WordLength / 2));
                    Print(textPosition, (y - 6), name);
                    double Percent = (double)((FillValue*100)/MaxValue);
                    string[] SelectedArray = Warning.Split(':');
                    int WarningInt = ToInt32.Parse(SelectedArray[1])
                    if (SelectedArray[0] = "<")
                    {
                        if (Percent < WarningInt)
                        {
                            FillColour = (Color.Red);
                            TextColour = (Color.Red);
                        }
                    }
                    else if (SelectedArray[0] = ">")
                    {
                        if (Percent > WarningInt)
                        {
                            FillColour = (Color.Red);
                            TextColour = (Color.Red);
                        }
                    }
                    if (FillValue <= MinValue || FillValue > MaxValue)
                    {
                        if (ErrorFlash)
                        {
                            Rect(x + 1, (y+1), width - 2, (height-2), true);
                            ErrorFlash = false;
                        }
                        else if (!ErrorFlash)
                        {
                            Rect(x + 1, (y+1), width - 2, (height-2), false);
                            ErrorFlash = true;
                        }
                    }
                    else
                    {
                        SetForeground(FillColour);
                        Rect(x + 1, ((y+1)+(height-2))-fill, width - 2, fill, true);
                    }
                    SetForeground(TextColour);
                    int CharY = 0;
                    Print(90, 20, fillb.ToString());
                    if (value == "%")
                    {
                        string percent = (Math.Round(Percent,0)).ToString("P2", percentFormat);
                        foreach (char c in (percent)
                        {
                            Print(x + (width / 2) - 2, y + 2 + CharY, c.ToString());
                            CharY += 6;
                        }
                    }
                    else if (value == "m")
                    {
                        foreach (char c in (Math.Round(FillValue,0)).ToString() + "m"))
                        {
                            Print(x + (width / 2) - 2, y + 2 + CharY, c.ToString());
                            CharY += 6;
                        }
                    }
                    else
                    {
                        foreach (char c in (FillValue.ToString()))
                        {
                            Print(x + (width / 2) - 2, y + 2 + CharY, c.ToString());
                            CharY += 6;
                        }
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
        const string LCDs = "LCD1";
        int width = 177;
        int height = 89;
        int counter = 0;
        int currentAltitude = 0;
        int currentFuel = 100;

        Graphics G;
        IMyTextPanel LCD;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }

        public void Main(string arg)
        {
            if (counter == 0)
            {
                LCD = GridTerminalSystem.GetBlockWithName(LCDs) as IMyTextPanel;
                LCD.FontSize = 0.2f;
                LCD.FontColor = Color.White;
                LCD.BackgroundColor = Color.Black;
                LCD.Font = "DotMatrix";
                LCD.ShowPublicTextOnScreen();
                G = new Graphics(width, height, LCD, Echo); //width in pixels, height in pixels, lcd panel, The echo method for debugging.
            }
            if (arg == "Reset") { currentAltitude = 0; counter = 0; currentFuel = 100; }
            //G.Echo("Counter: " + counter);
            G.Clear();
            G.SetBackground(Color.Black);
            G.SetForeground(Color.Red);
            G.Rect(0, 0, 177, 88, false); //LCD Boarder     

            G.titleText(Ship, Color.Blue, Color.Blue, 4); //Title

            G.FillBar("Alt", "Vertical", 4, 10, 12, 76, 10000, 0, currentAltitude, "m", ">:100", Color.Blue, Color.Green, Color.Orange);

            //G.FillBar("Fuel", "Vertical", 115, 10, 12, 76, 100, 0, currentFuel,"%", "<:25", Color.Blue, Color.Green, Color.Orange);

            G.systemTime(Color.Orange, Color.Blue);

            G.Draw();
            if (counter < 100)
            {
                counter += 1;
                currentAltitude += 100;
                currentFuel -= 1;
            }
        }
    }

}
