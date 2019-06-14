﻿using System;
using SadConsole;
using Console = SadConsole.Console; 
using Microsoft.Xna.Framework;
using TearsInRain.UI;
using TearsInRain.Commands;
using System.Collections.Generic;
using TearsInRain.Entities;
using Newtonsoft.Json;
using TearsInRain.Serializers;
using System.IO;
using TearsInRain.Tiles;

namespace TearsInRain {
    class GameLoop {
        public static UInt64 GameTime = 0; // Deciseconds since game launch (Decisecond = tenth of a second)

        public const int GameWidth = 80;
        public const int GameHeight = 60;

        public static UIManager UIManager;
        public static World World;
        public static CommandManager CommandManager;
        public static NetworkingManager NetworkingManager;

        public static TimeManager TimeManager;
        public static Point MouseLoc = new Point(0, 0);


        public static Dictionary<string, System.Media.SoundPlayer> SoundLibrary = new Dictionary<string, System.Media.SoundPlayer>();
        public static Dictionary<string, Monster> MonsterLibrary = new Dictionary<string, Monster>();
        public static Dictionary<string, Item> ItemLibrary = new Dictionary<string, Item>();
        public static Dictionary<string, TileBase> TileLibrary = new Dictionary<string, TileBase>();

        static int oldWindowPixelWidth;
        static int oldWindowPixelHeight;

        public static bool timeFlowing = true;
        public static int centisecondCounter = 0;


        public static GoRogue.MultiSpatialMap<Entity> ReceivedEntities;

        public static Random Random = new Random();
        static void Main(string[] args) {
            initSounds();

            SadConsole.Game.Create(GameWidth, GameHeight);

            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;


            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        private static void Update(GameTime time) {
            if (GameTime < (UInt64) time.TotalGameTime.TotalMilliseconds / 10) {
                GameTime = (UInt64) time.TotalGameTime.TotalMilliseconds / 10;

                if (timeFlowing) {
                    centisecondCounter++;
                    if (centisecondCounter >= 75) {
                        centisecondCounter = 0;
                        TimeManager.AddMinute();
                    }
                }
            }


            if (MouseLoc != Global.MouseState.ScreenPosition) {
                MouseLoc = Global.MouseState.ScreenPosition;
            }

            NetworkingManager.Update();
        }

        private static void Init() {
            SadConsole.Themes.WindowTheme windowTheme = new SadConsole.Themes.WindowTheme(new SadConsole.Themes.Colors());
            windowTheme.BorderLineStyle = CellSurface.ConnectedLineThick;
            SadConsole.Themes.Library.Default.WindowTheme = windowTheme;

            

            SadConsole.Themes.Library.Default.Colors.TitleText = new Color(51, 153, 255);
            SadConsole.Themes.Library.Default.Colors.Lines = new Color(51, 153, 255); 
            SadConsole.Themes.Library.Default.Colors.ControlHostBack = Color.Black;

            
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlNormal = new Cell();
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlOver = new Cell(Color.Blue, Color.Black);
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlMouseDown = new Cell(Color.DarkBlue, Color.Black);
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlFocused = new Cell(Color.White, Color.Black);

            Utils.InitDirections();

            Global.FontDefault = Global.LoadFont("fonts/Cheepicus12.font").GetFont(Font.FontSizes.One);
            Global.FontDefault.ResizeGraphicsDeviceManager(SadConsole.Global.GraphicsDeviceManager, 80, 60, 0, 0);
            Global.ResetRendering();

            Global.KeyboardState.InitialRepeatDelay = 0.5f;

            Settings.AllowWindowResize = true;

            UIManager = new UIManager();

            initLibraries();

            CommandManager = new CommandManager();

            NetworkingManager = new NetworkingManager();
            TimeManager = new TimeManager();

            World = new World("");

            UIManager.Init();
            SadConsole.Game.Instance.Window.ClientSizeChanged += Window_ClientSizeChanged;

            SadConsole.Game.OnUpdate += postUpdate;
        }

        private static void postUpdate(GameTime time) {
            if (NetworkingManager.discord.GetLobbyManager() != null) {
                NetworkingManager.discord.GetLobbyManager().FlushNetwork();
            }
        }

        private static void initLibraries() {
            string tileLibJson = File.ReadAllText(@"./data/json/tiles.json");
            TileLibrary = JsonConvert.DeserializeObject<Dictionary<string, TileBase>>(tileLibJson, new TileJsonConverter());

            string itemLibJson = File.ReadAllText(@"./data/json/items.json"); 
            ItemLibrary = JsonConvert.DeserializeObject<Dictionary<string, Item>>(itemLibJson, new ItemJsonConverter());

        }


        private static void initSounds() {
            SoundLibrary.Add("door_close", new System.Media.SoundPlayer(@"res/door_close.wav"));
            SoundLibrary.Add("door_open", new System.Media.SoundPlayer(@"res/door_open.wav"));
            
        }


        private static void Window_ClientSizeChanged(object sender, EventArgs e) {
            int windowPixelsWidth = SadConsole.Game.Instance.Window.ClientBounds.Width;
            int windowPixelsHeight = SadConsole.Game.Instance.Window.ClientBounds.Height;

            // If this is getting called because of the ApplyChanges, exit.
            if (windowPixelsWidth == oldWindowPixelWidth && windowPixelsHeight == oldWindowPixelHeight)
                return;

            // Store for later
            oldWindowPixelWidth = windowPixelsWidth;
            oldWindowPixelHeight = windowPixelsHeight;

            // Get the exact pixels we can fit in that window based on a font.
            int fontPixelsWidth = (windowPixelsWidth / SadConsole.Global.FontDefault.Size.X) * SadConsole.Global.FontDefault.Size.X;
            int fontPixelsHeight = (windowPixelsHeight / SadConsole.Global.FontDefault.Size.Y) * SadConsole.Global.FontDefault.Size.Y;

            // Resize the monogame rendering to match
            SadConsole.Global.GraphicsDeviceManager.PreferredBackBufferWidth = windowPixelsWidth;
            SadConsole.Global.GraphicsDeviceManager.PreferredBackBufferHeight = windowPixelsHeight;
            SadConsole.Global.GraphicsDeviceManager.ApplyChanges();

            // Tell sadconsole how much to render to the screen.
            Global.RenderWidth = fontPixelsWidth;
            Global.RenderHeight = fontPixelsHeight;
            Global.ResetRendering();

            // Get the total cells you can fit
            int totalCellsX = fontPixelsWidth / SadConsole.Global.FontDefault.Size.X;
            int totalCellsY = fontPixelsHeight / SadConsole.Global.FontDefault.Size.Y;

            
            UIManager.checkResize(totalCellsX, totalCellsY);
        }
    }
}