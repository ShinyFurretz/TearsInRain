﻿using System;
using SadConsole;
using Console = SadConsole.Console;
using Microsoft.Xna.Framework;
using TearsInRain.UI;
using TearsInRain.Commands;

namespace TearsInRain {
    class GameLoop {

        public const int GameWidth = 120;
        public const int GameHeight = 80;

        public static UIManager UIManager;
        public static World World;
        public static CommandManager CommandManager;
        public static NetworkingManager NetworkingManager;

        static int oldWindowPixelWidth;
        static int oldWindowPixelHeight;

        public static Random Random = new Random();
        static void Main(string[] args) {
            SadConsole.Game.Create(GameWidth, GameHeight);
            
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;
            

            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
            
        }

        private static void Update(GameTime time) {
            NetworkingManager.Update();
        }

        private static void Init() {
            SadConsole.Themes.WindowTheme windowTheme = new SadConsole.Themes.WindowTheme(new SadConsole.Themes.Colors());
            windowTheme.BorderLineStyle = CellSurface.ConnectedLineThin;
            SadConsole.Themes.Library.Default.WindowTheme = windowTheme;

            SadConsole.Themes.Library.Default.Colors.TitleText = Color.White;
            SadConsole.Themes.Library.Default.Colors.ControlHostBack = Color.Black;
            SadConsole.Themes.Library.Default.Colors.Appearance_ControlNormal = new Cell();
            SadConsole.Themes.Library.Default.Colors.Appearance_ControlOver = new Cell(Color.Blue, Color.Black);
            SadConsole.Themes.Library.Default.Colors.Appearance_ControlMouseDown = new Cell(Color.DarkBlue, Color.Black);



            Global.FontDefault = Global.LoadFont("fonts/Cheepicus12.font").GetFont(Font.FontSizes.One);
            Global.FontDefault.ResizeGraphicsDeviceManager(SadConsole.Global.GraphicsDeviceManager, 100, 75, 0, 0);
            Global.ResetRendering();
            

            Settings.AllowWindowResize = true;

            UIManager = new UIManager();
            CommandManager = new CommandManager();

            NetworkingManager = new NetworkingManager();

            World = new World();
            UIManager.Init();
            SadConsole.Game.Instance.Window.ClientSizeChanged += Window_ClientSizeChanged;

            SadConsole.Game.OnUpdate += postUpdate;

        }

        private static void postUpdate(GameTime time) {
            if (NetworkingManager.discord.GetLobbyManager() != null) {
                NetworkingManager.discord.GetLobbyManager().FlushNetwork();
            }
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