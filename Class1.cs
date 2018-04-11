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

        Graphics G;
        IMyTextPanel LCD;
        public Program()
        {
            LCD = GridTerminalSystem.GetBlockWithName("LCD Panel") as IMyTextPanel;
            LCD.FontSize = 0.2f;
            LCD.FontColor = Color.White;
            LCD.BackgroundColor = Color.Black;
            LCD.Font = "DotMatrix";
            LCD.ShowPublicTextOnScreen();


            G = new Graphics(132, 89, LCD, Echo); //width in pixels, height in pixels, lcd panel, The echo method for debugging.
        }

        public void Save()
        {

        }

        public void Main(string argument)
        {
            G.SetForeground(Color.Green);
            G.Rect(5, 5, 10, 10, true);
            G.Draw();
        }
    }

}
