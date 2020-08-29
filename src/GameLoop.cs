using System;
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
using TearsInRain.src;

namespace TearsInRain {
    class GameLoop {
        public static UInt64 GameTime = 0; // Deciseconds since game launch (Decisecond = tenth of a second)

        public static int GameWidth = 80;
        public static int GameHeight = 60;

        public static UIManager UIManager;
        public static World World;
        public static CommandManager CommandManager;
        public static NetworkingManager NetworkingManager;

        public static TimeManager TimeManager;
        public static Point MouseLoc = new Point(0, 0);


        public static Dictionary<string, System.Media.SoundPlayer> SoundLibrary = new Dictionary<string, System.Media.SoundPlayer>(); 
        public static SortedDictionary<string, Item> ItemLibrary = new SortedDictionary<string, Item>();
        public static SortedDictionary<string, TileBase> TileLibrary = new SortedDictionary<string, TileBase>();
        public static SortedDictionary<string, Actor> ActorLibrary = new SortedDictionary<string, Actor>();
        public static SortedDictionary<string, TerrainFeature> TerrainFeatureLibrary = new SortedDictionary<string, TerrainFeature>();

        public static SortedDictionary<string, Skill> SkillLibrary = new SortedDictionary<string, Skill>();
        public static SortedDictionary<string, CharacterClass> ClassLibrary = new SortedDictionary<string, CharacterClass>();
        public static SortedDictionary<string, CharacterRace> RaceLibrary = new SortedDictionary<string, CharacterRace>();

        static int oldWindowPixelWidth;
        static int oldWindowPixelHeight;

        public static bool timeFlowing = true;
        public static int centisecondCounter = 0;

        public static Color CyberBlue = new Color(51, 153, 255);


        public static Font RegularSize;
        public static Font DoubleSize;
        public static Font QuadrupleSize;

        public static Font MapQuarter;
        public static Font MapHalf;
        public static Font MapOne;

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

            string[] allFiles = Directory.GetFiles(@"./fonts/");

            for (int i = 0; i < allFiles.Length; i++) {
                if (allFiles[i].Contains(".font")) {
                    Global.LoadFont(allFiles[i]);
                }
            }

            RegularSize = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.Quarter);
            DoubleSize = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.Half);
            QuadrupleSize = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.One);

            MapQuarter = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.Quarter);
            MapHalf = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.Half);
            MapOne = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.One);
             
            


            SadConsole.Themes.Library.Default.Colors.TitleText = CyberBlue;
            SadConsole.Themes.Library.Default.Colors.Lines = CyberBlue; 
            SadConsole.Themes.Library.Default.Colors.ControlHostBack = Color.Black;

            
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlNormal = new Cell();
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlOver = new Cell(Color.Blue, Color.Black);
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlMouseDown = new Cell(Color.DarkBlue, Color.Black);
            //SadConsole.Themes.Library.Default.Colors.Appearance_ControlFocused = new Cell(Color.White, Color.Black);

            Utils.InitDirections();

            Global.FontDefault = RegularSize;
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






            SimplexNoise.Noise.Seed = 2;
            List<float> points = new List<float>();

            for (int i = 0; i < 200; i++) {
                points.Add(SimplexNoise.Noise.CalcPixel2D(i-10, 0, 0.5f));
            }
        }

        private static void postUpdate(GameTime time) {
            if (NetworkingManager.discord.GetLobbyManager() != null) {
                NetworkingManager.discord.GetLobbyManager().FlushNetwork();
            }
        }

        private static void initLibraries() {
            string tileLibJson = File.ReadAllText(@"./data/json/tiles.json");
            TileLibrary = JsonConvert.DeserializeObject<SortedDictionary<string, TileBase>>(tileLibJson, new TileJsonConverter());

            string itemLibJson = File.ReadAllText(@"./data/json/items.json"); 
            ItemLibrary = JsonConvert.DeserializeObject<SortedDictionary<string, Item>>(itemLibJson, new ItemJsonConverter());

            string skillLibJson = File.ReadAllText(@"./data/json/skills.json");
            SkillLibrary = JsonConvert.DeserializeObject<SortedDictionary<string, Skill>>(skillLibJson, new SkillJsonConverter());

            string classLibJson = File.ReadAllText(@"./data/json/classes.json");
            ClassLibrary = JsonConvert.DeserializeObject<SortedDictionary<string, CharacterClass>>(classLibJson, new ClassJsonConverter());


            RaceLibrary.Add("Human", new CharacterRace("Human", new int[] { 0, 0, 0, 0, 0, 0 }, Color.Yellow));
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

            GameHeight = totalCellsY;
            GameWidth = totalCellsX;
            
            UIManager.checkResize(totalCellsX, totalCellsY);
        }
    }
}