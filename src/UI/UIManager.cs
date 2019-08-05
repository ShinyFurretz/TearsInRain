﻿using System;
using System.Collections.Generic;
using Discord;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using RogueSharp.DiceNotation;
using SadConsole;
using SadConsole.Controls;
using SadConsole.Input;
using TearsInRain.Entities;
using Entity = TearsInRain.Entities.Entity;
using TearsInRain.Serializers;
using TearsInRain.Tiles;
using Console = SadConsole.Console;
using Utils = TearsInRain.Utils;
using GoRogue;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;
using TearsInRain.src;

namespace TearsInRain.UI {
    public class UIManager : ContainerConsole {
        public Console MapConsole;
        public Console SplashConsole;
        public Console SplashRain;
        public Console MenuConsole;
        
        public Window MapWindow;
        public MessageLogWindow MessageLog;
        public CloseableWindow EscapeMenuWindow;
        public CloseableWindow StatusWindow;
        public CloseableWindow InventoryWindow;
        public CloseableWindow EquipmentWindow; 
        public CloseableWindow ContextWindow;
        public CloseableWindow CreatorToolWindow;
        public CloseableWindow SettingsWindow;

        public Button hostButton;
        public Button closeButton;
        public Button joinButton;

        public ControlsConsole joinPrompt;
        public TextBox joinBox;
        public CloseableWindow ServerBrowserWindow;
        public List<string> SearchResults = new List<string>();
        public int selectedCode = -1; 

        public CloseableWindow WorldCreateWindow;
        public ControlsConsole WorldCreateConsole;
        public TextBox WorldNameBox;
        public Button CreateWorldButton;

        public CloseableWindow LoadWindow;
        public string selectedWorldName = "";
        public bool tryDelete = false;


        public CloseableWindow CharLoadWindow;
        public List<Player> currPlayerChars;

        public bool NoWorldChar = false;
        public Window CharCreateWindow;
        public ScrollingConsole CharOptionsConsole;
        public ControlsConsole CharCreateConsole;
        public ControlsConsole CharInfoConsole;
        public Player CreatingChar;
        public string CreationSelectedOption = "";
        public string SelectedSkill = "";
        public string SelectedClass = "(NONE)";
        public string SelectedRace = "(NONE)";
        public CharacterClass selectClass;
        public List<string> SkillNames = new List<string>();
        public List<string> ClassNames = new List<string>();
        public List<string> RaceNames = new List<string>();
        public Color[] skinColors;
        public int skinIndex;
        public Color[] hairColors;
        public int hairIndex;
        public int hairStyleIndex;
        public TextBox PlayerNameBox;
        public Player characterToLoad;
        

        public Point Mouse;
        public string STATE = "MENU";
        public string JOIN_CODE = "";
        public string SelectedTool = "none";

       
        public bool chat = false;
        public long tempUID = 0;
        public int invContextIndex = -1;

        public List<Entity> rain = new List<Entity>();
        public int raindrops = 0;

        public string waitingForCommand = "";
        public Point viewOffset = new Point(0, 0);
        public Font hold = GameLoop.MapQuarter;
        public int zoom = 1;
        public ulong LastEntityCheck = 0;

        public Point CameraOrigin = Point.Zero;
        public Point CameraEnd = Point.Zero;
        public TileBase[] latestRegion;

        public UIManager() {
            IsVisible = true;
            IsFocused = true;

            Parent = SadConsole.Global.CurrentScreen;
        }

        public void checkResize(int newX, int newY) {
            this.Resize(newX, newY, false);

            if (MapConsole != null) {
                ResizeMap();

                MapConsole.IsDirty = true;
                MapWindow.IsDirty = true;
                CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);

                int windowPixelsWidth = SadConsole.Game.Instance.Window.ClientBounds.Width - (240);
                int windowPixelsHeight = SadConsole.Game.Instance.Window.ClientBounds.Height - (360);

                int newW = (windowPixelsWidth / 12);
                int newH = (windowPixelsHeight / 12);

                MapWindow.Resize(newW, newH, false, new Rectangle(0, 0, newW, newH));
                MapWindow.Title = "[GAME MAP]".Align(HorizontalAlignment.Center, GameLoop.GameWidth - 22, (char)205);

                StatusWindow.Position = new Point(MapWindow.Width, 0);
                MessageLog.Position = new Point(0, MapWindow.Height);

                SyncMapEntities();
            }
        }
        
        public void Init() {
            UseMouse = true;
            LoadSplash();

            IsFocused = true;


            


            joinPrompt = new ControlsConsole(12, 1, Global.FontDefault);
            joinPrompt.IsVisible = true;
            joinPrompt.Position = new Point(45, 1);

            joinBox = new TextBox(12);
            joinBox.Position = new Point(0,0);
            joinBox.UseKeyboard = true;
            joinBox.IsVisible = true;
 
            joinPrompt.Add(joinBox);

            joinPrompt.FocusOnMouseClick = true;
            CreateWorld();
            LoadWorldDialogue();

            CharacterCreationDialogue();

            CharLoadWindow = new CloseableWindow(40, 20, "[CHOOSE CHARACTER]");
            CharLoadWindow.console.MouseButtonClicked += loadCharClick;
            Children.Add(CharLoadWindow);

            CreatorToolWindow = new CloseableWindow(60, 40, "[CREATOR TOOLS]");
            Children.Add(CreatorToolWindow);

            SettingsWindow = new CloseableWindow(40, 20, "[SETTINGS]");
            Children.Add(SettingsWindow);

            EscapeMenuWindow = new CloseableWindow(20, 20, "[MENU]");
            Children.Add(EscapeMenuWindow);
            
            ServerBrowserWindow = new CloseableWindow(60, 40, "[SERVER BROWSER]");
            ServerBrowserWindow.console.FocusOnMouseClick = true;
            ServerBrowserWindow.console.MouseButtonClicked += serverBrowserClick;
            ServerBrowserWindow.Children.Add(joinPrompt);
            Children.Add(ServerBrowserWindow);
        }

        public override void Update(TimeSpan timeElapsed) {
            CheckKeyboard();

            base.Update(timeElapsed);
            if (STATE == "GAME") {
                GameLoop.World.CalculateFov(GameLoop.CommandManager.lastPeek);
        
                if (StatusWindow.IsVisible) { UpdateStatusWindow(); }
                if (ContextWindow.IsVisible) { UpdateContextWindow(); }
                if (EquipmentWindow.IsVisible) { UpdateEquipment(); }
                if (InventoryWindow.IsVisible) { UpdateInventory(); }

                if (!InventoryWindow.IsVisible && ContextWindow.IsVisible) { ContextWindow.SetVisibility(false);  }
                
                if (latestRegion != null) {
                    CameraOrigin = latestRegion[0].literalPos;
                    CameraEnd = latestRegion[latestRegion.Length - 1].literalPos;
                }

                

                foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                    if (player.Key == GameLoop.NetworkingManager.myUID) {
                        player.Value.Position = new Point(MapConsole.Width / 2, MapConsole.Height / 2);
                    } else {
                        if (Utils.PointInArea(CameraOrigin, CameraEnd, player.Value.literalPosition)) {
                            player.Value.Position = player.Value.literalPosition - CameraOrigin;
                        }
                    }
                }

               // SyncMapEntities();




                if (GameLoop.GameTime > LastEntityCheck + 100) {
                    CheckEntities();
                }
            }

            if (STATE == "MENU") {
                SplashAnim();

                if (CharCreateWindow.IsVisible) {
                    UpdateCharCreation();
                    UpdateCharOptions();
                }

                if (CharLoadWindow.IsVisible) { UpdateLoadCharWindow(); } 
                if (ServerBrowserWindow.IsVisible) { UpdateServerBrowser(); }
                if (SettingsWindow.IsVisible) { UpdateSettingsWindow(); }
                if (CreatorToolWindow.IsVisible) { UpdateCreatorToolWindow(); }
            }


        }

        public void CreateConsoles() {
            MapConsole = new Console(60, (GameLoop.GameHeight / 3) * 2);
        }

        public void UpdateContextWindow() {
            ContextWindow.UseMouse = true;
            ContextWindow.console.Clear();

            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                 
                if (invContextIndex != -1 && invContextIndex < player.Inventory.Count) {
                    Item item = player.Inventory[invContextIndex];
                    string itemName = item.Name;

                    if (itemName.Length > 18) { itemName = itemName.Substring(0, 14) + "...";  }

                    ContextWindow.console.Print(0, 0, itemName.Align(HorizontalAlignment.Center, 18, ' '));
                    ContextWindow.console.Print(0, 1, ("QTY: " + item.Quantity.ToString()).Align(HorizontalAlignment.Center, 18, ' '));
                    ContextWindow.console.Print(0, 2, (("" + (char)205).Align(HorizontalAlignment.Center, 18, (char)205)).CreateColored(GameLoop.LogoColors[3]));

                    ContextWindow.console.Print(0, 3, "Drop 01".Align(HorizontalAlignment.Center, 18, ' '));
                    ContextWindow.console.Print(0, 4, "Drop 05".Align(HorizontalAlignment.Center, 18, ' '));
                    ContextWindow.console.Print(0, 5, "Drop 10".Align(HorizontalAlignment.Center, 18, ' '));
                    ContextWindow.console.Print(0, 6, "Drop **".Align(HorizontalAlignment.Center, 18, ' '));
                    
                    if (item.Slot != -1) {
                        ContextWindow.console.Print(0, 8, "Equip".Align(HorizontalAlignment.Center, 18, ' '));
                    } 
                } else {
                    ContextWindow.console.Print(0, 0, "No Item Selected".Align(HorizontalAlignment.Center, 18, ' '));
                    ContextWindow.console.Print(0, 2, (("" + (char)205).Align(HorizontalAlignment.Center, 18, (char)205)).CreateColored(GameLoop.LogoColors[3]));
                    invContextIndex = -1;
                }
            }

            ContextWindow.IsDirty = true;
        }

        private void contextClick(object sender, MouseEventArgs e) {
            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID) && ContextWindow.IsVisible) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                if (e.MouseState.ConsoleCellPosition.Y == 3) {
                    if (invContextIndex != -1 && invContextIndex < player.Inventory.Count) {
                        player.DropItem(invContextIndex, 1);
                    } else {
                        invContextIndex = -1;
                    } 
                } else if (e.MouseState.ConsoleCellPosition.Y == 4) {
                    if (invContextIndex != -1 && invContextIndex < player.Inventory.Count) {
                        player.DropItem(invContextIndex, 5);
                    } else {
                        invContextIndex = -1;
                    }
                } else if (e.MouseState.ConsoleCellPosition.Y == 5) {
                    if (invContextIndex != -1 && invContextIndex < player.Inventory.Count) {
                        player.DropItem(invContextIndex, 10);
                    } else {
                        invContextIndex = -1;
                    }
                } else if (e.MouseState.ConsoleCellPosition.Y == 6) {
                    if (invContextIndex != -1 && invContextIndex < player.Inventory.Count) {
                        player.DropItem(invContextIndex, 0);
                    } else {
                        invContextIndex = -1;
                    }
                } else if (e.MouseState.ConsoleCellPosition.Y == 8) {
                    if (invContextIndex != -1 && invContextIndex < player.Inventory.Count) {
                        player.Equip(invContextIndex);
                    } else {
                        invContextIndex = -1;
                    }
                }

                if (player.Inventory.Count == 0) {
                    invContextIndex = -1;
                    ContextWindow.IsVisible = false;
                }
            } 
        }

        public void UpdateStatusWindow() { 
            StatusWindow.console.Clear();

            Player player = null;

            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
            }

            TimeManager time = GameLoop.TimeManager;

            ColoredString season = time.ColoredSeason();
            ColoredString timeString = new ColoredString(time.GetTimeString(), Color.White, Color.TransparentBlack);
            string dayYear = time.Day.ToString();
            if (time.Day < 10) { dayYear = " " + dayYear; }

            string year = "Year " + time.Year.ToString();

            StatusWindow.console.Print(0, 1, season);
            StatusWindow.console.Print(0 + season.Count, 1, dayYear);
            StatusWindow.console.Print(0, 2, year);
            StatusWindow.console.Print(StatusWindow.console.Width-timeString.Count - 2, 1, timeString);


            StatusWindow.console.Print(0, 3, (("" + (char) 205).Align(HorizontalAlignment.Center, 18, (char) 205)).CreateColored(GameLoop.LogoColors[3]));


            if (player != null) {
                ColoredString heldTIR = new ColoredString(player.HeldGold.ToString() + " " + (char) 1, Color.Yellow, Color.Transparent);
                StatusWindow.console.Print(StatusWindow.console.Width - heldTIR.Count - 2, 2, heldTIR);


                StatusWindow.console.Print(0, 4, " STR: " + player.Strength.ToString());
                StatusWindow.console.Print(8, 4, "  DEX: " + player.Dexterity.ToString());
                StatusWindow.console.Print(0, 5, " CON: " + player.Constitution.ToString());
                StatusWindow.console.Print(8, 5, "  INT: " + player.Intelligence.ToString());

                StatusWindow.console.Print(0, 6, " WIS: " + player.Wisdom.ToString());
                StatusWindow.console.Print(8, 6, "  CHA: " + player.Charisma.ToString());
                StatusWindow.console.Print(0, 7, (("" + (char)205).Align(HorizontalAlignment.Center, 18, (char)205)).CreateColored(GameLoop.LogoColors[3]));

                ColorGradient wgtGrad = new ColorGradient(Color.Green, Color.Red);
                player.RecalculateWeight(); 
                Color wgtColor = wgtGrad.Lerp((float) (player.Carrying_Weight / player.BasicLift));


                ColoredString wgt = new ColoredString((player.Carrying_Weight.ToString() + " / " + (player.BasicLift).ToString() + " kg"), wgtColor, Color.Transparent);
                ColoredString spd = new ColoredString("SPD: " + player.Speed, wgtColor, Color.Transparent);
                ColoredString dodge = new ColoredString("DODGE: " + player.Dodge, wgtColor, Color.Transparent);

                StatusWindow.console.Print(0, 8, "WGT: ", wgtColor);
                StatusWindow.console.Print(StatusWindow.console.Width - wgt.Count - 2, 8, wgt);
                StatusWindow.console.Print(0, 9, spd);
                StatusWindow.console.Print(10, 9, dodge);
                StatusWindow.console.Print(0, 10, (("" + (char)205).Align(HorizontalAlignment.Center, 18, (char)205)).CreateColored(GameLoop.LogoColors[3]));


                StatusWindow.console.Print(0, 11, "HEALTH: "); 
                float percent = (float) player.Health /  (float) player.MaxHealth; 
                ColorGradient hpgradient = new ColorGradient(Color.Red, Color.Green);
                string hpbar = "";
                for (int i = 0; i < 10; i++) {
                    if (percent >= ((float) i * 0.1f)) {
                        hpbar += "#";
                    }
                }
                ColoredString health = new ColoredString(hpbar, hpgradient.Lerp(percent), Color.Transparent);
                StatusWindow.console.Print(8, 11, health);


                StatusWindow.console.Print(0, 12, "  STAM: ");
                float stamPercent = (float)player.CurrentStamina / (float)player.MaxStamina;
                ColorGradient stamGradient = new ColorGradient(Color.Red, Color.Yellow);
                string stamBar = "";
                for (int i = 0; i < 10; i++) {
                    if (stamPercent >= ((float)i * 0.1f)) {
                        stamBar += "#";
                    }
                }
                ColoredString stamina = new ColoredString(stamBar, stamGradient.Lerp(stamPercent), Color.Transparent);
                StatusWindow.console.Print(8, 12, stamina);


                StatusWindow.console.Print(0, 13, "ENERGY: ");
                float energyPercent = (float)player.CurrentEnergy / (float)player.MaxEnergy;
                ColorGradient energyGradient = new ColorGradient(Color.Red, Color.Aqua);
                string energyBar = "";
                for (int i = 0; i < 10; i++) {
                    if (energyPercent >= ((float)i * 0.1f)) {
                        energyBar += "#";
                    }
                }
                ColoredString energy = new ColoredString(energyBar, energyGradient.Lerp(energyPercent), Color.Transparent);
                StatusWindow.console.Print(8, 13, energy);

                
             //   int mouseIndex = Mouse.ToIndex(MapConsole.Width);
                int mouseIndex = (Global.MouseState.ScreenPosition - new Point(12, 12)).PixelLocationToConsole(12 * zoom, 12 * zoom).ToIndex(MapConsole.Width);

                TileBase hovered = null;

                Point mousePos = Mouse;
                if (latestRegion != null) {
                    if (mouseIndex >= 0 && mouseIndex < latestRegion.Length) {
                        hovered = GameLoop.World.CurrentMap.GetTileAt<TileBase>(latestRegion[mouseIndex].literalPos);
                        mousePos = latestRegion[mouseIndex].literalPos;
                    }
                }

                StatusWindow.console.Print(0, 19, "GROUND".Align(HorizontalAlignment.Center, 18, (char)205).CreateColored(GameLoop.LogoColors[3]));
                if (hovered != null && hovered.Background.A == 255 && hovered.IsVisible) {
                    ColoredString hovName = new ColoredString(hovered.Name.Align(HorizontalAlignment.Center, 18, ' '), hovered.Foreground, Color.Transparent);
                    StatusWindow.console.Print(0, 20, hovName);

                    StatusWindow.console.Print(0, 21, mousePos.ToString());
                    int drawLineAt = 22;

                    List<TerrainFeature> tfsAtMouse = GameLoop.World.CurrentMap.GetEntitiesAt<TerrainFeature>(mousePos);
                    if (tfsAtMouse.Count > 0) {
                        drawLineAt++;
                        StatusWindow.console.Print(0, drawLineAt, "FEATURES".Align(HorizontalAlignment.Center, 18, (char)205).CreateColored(GameLoop.LogoColors[3]));
                        drawLineAt++;
                    }

                    for (int i = 0; i < tfsAtMouse.Count; i++) {
                        ColoredString tfCol = new ColoredString(tfsAtMouse[i].Name.Align(HorizontalAlignment.Center, 18, ' '), tfsAtMouse[i].Animation.CurrentFrame[0].Foreground, Color.Transparent);
                        StatusWindow.console.Print(0, drawLineAt, tfCol);
                        drawLineAt++;
                    }


                    List<Actor> monstersAtTile = GameLoop.World.CurrentMap.GetEntitiesAt<Actor>(mousePos);

                    if (monstersAtTile.Count > 0) {
                        drawLineAt++;
                        StatusWindow.console.Print(0, drawLineAt, "ENTITIES".Align(HorizontalAlignment.Center, 18, (char)205).CreateColored(GameLoop.LogoColors[3]));
                        drawLineAt++;
                    }

                    for (int i = 0; i < monstersAtTile.Count; i++) {
                        ColorGradient enHealthGrad = new ColorGradient(Color.Red, Color.Green);
                        ColoredString enHealth = new ColoredString(monstersAtTile[i].Name.Align(HorizontalAlignment.Center, 18, ' '), enHealthGrad.Lerp((float)monstersAtTile[i].Health / monstersAtTile[i].MaxHealth), Color.Transparent);
                        StatusWindow.console.Print(0, drawLineAt, enHealth);
                        drawLineAt++;
                    }


                    List<Item> itemsAtTile = GameLoop.World.CurrentMap.GetEntitiesAt<Item>(mousePos);

                    if (itemsAtTile.Count > 0) {
                        drawLineAt++;
                        StatusWindow.console.Print(0, drawLineAt, "ITEMS".Align(HorizontalAlignment.Center, 18, (char)205).CreateColored(GameLoop.LogoColors[3]));
                        drawLineAt++;
                    }

                    for (int i = 0; i < itemsAtTile.Count; i++) {
                        string itemName = itemsAtTile[i].Name;
                        if (itemName.Length > 16) { itemName = itemName.Substring(0, 13) + "..."; }
                        StatusWindow.console.Print(2, drawLineAt, itemName.Align(HorizontalAlignment.Center, 16, ' '));
                        StatusWindow.console.Print(0, drawLineAt, itemsAtTile[i].Quantity.ToString());
                        drawLineAt++;
                    }
                }
            }
        }

        private void eqpMouseClick(object sender, MouseEventArgs e) {
            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID) && EquipmentWindow.IsVisible) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                if (e.MouseState.ConsoleCellPosition.Y < 14 && player.Equipped[e.MouseState.ConsoleCellPosition.Y] != null) {
                    player.Unequip(e.MouseState.ConsoleCellPosition.Y);
                }
            }
        }

        public void UpdateEquipment() {
            EquipmentWindow.console.Clear();
            
            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                for (int i = 0; i < player.Equipped.Length; i++) {
                    if (player.Equipped[i] != null) {
                        EquipmentWindow.console.Print(0, i, player.Equipped[i].Name);
                    } else {
                        switch(i) {
                            case 0:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(MELEE)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 1:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(RANGED)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 2:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(AMMO)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 3:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(RING)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 4:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(RING)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 5:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(NECK)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 6:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(LIGHTING)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 7:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(BODY)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 8:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(CLOAK)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 9:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(SHIELD)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 10:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(HEAD)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 11:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(HANDS)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 12:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(FEET)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            case 13:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(TOOL)", Color.DarkSlateGray, Color.Transparent));
                                break;
                            default:
                                EquipmentWindow.console.Print(0, i, new ColoredString("(ERROR)", Color.DarkSlateGray, Color.Transparent));
                                break;
                        }
                    }
                    
                    
                }
            }


        }

        private void invMouseClick(object sender, MouseEventArgs e) {
            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID) && InventoryWindow.IsVisible) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                if (player.Inventory.Count >= e.MouseState.ConsoleCellPosition.Y - 2 && e.MouseState.ConsoleCellPosition.Y - 2 >= 0) {
                    invContextIndex = e.MouseState.ConsoleCellPosition.Y - 2;
                    ContextWindow.IsVisible = false;
                    ContextWindow.IsVisible = true;
                    ContextWindow.IsDirty = true;
                }
            }
        } 

        public void UpdateInventory() {
            InventoryWindow.console.Clear();


            InventoryWindow.console.Print(29, 0, "Item Name | QTY | WEIGHT");
            InventoryWindow.console.Print(0, 1, (("" + (char)205).Align(HorizontalAlignment.Center, InventoryWindow.console.Width, (char)205)).CreateColored(GameLoop.LogoColors[3]));

            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                for (int i = 0; i < player.Inventory.Count; i++) {
                    InventoryWindow.console.Print(38 - player.Inventory[i].Name.Length, i+2, player.Inventory[i].Name);

                    string qty = player.Inventory[i].Quantity.ToString();
                    if (player.Inventory[i].Quantity < 100) { qty = " " + qty; }
                    if (player.Inventory[i].Quantity < 10) { qty = " " + qty; } 

                    var space = "";

                    string wgt = string.Format("{0:N2}", player.Inventory[i].StackWeight());
                    //string wgt = player.Inventory[i].StackWeight().ToString();
                    ColoredString weight = new ColoredString(string.Format("{0:N2}", player.Inventory[i].StackWeight(), Color.White, Color.Transparent));
                    if (player.Inventory[i].StackWeight() < 100) { space = " "; }
                    if (player.Inventory[i].StackWeight() < 10) { space = "  "; }
                    if (weight[weight.Count-1].Glyph == '0') { weight[weight.Count - 1].Foreground = Color.DarkSlateGray; }
                    if (weight[weight.Count - 2].Glyph == '0' && weight[weight.Count - 1].Glyph == '0') { weight[weight.Count - 2].Foreground = Color.DarkSlateGray; weight[weight.Count - 3].Foreground = Color.DarkSlateGray; }

                    InventoryWindow.console.Print(39, i + 2, "| " + qty + " | " + space + weight);
                    InventoryWindow.console.Print(54, i + 2, "kg");
                }
            }


            InventoryWindow.console.IsDirty = true;
        }


        public void ResizeMap() {
            int cellX = 12 * zoom;
            int cellY = 12 * zoom;

            int windowPixelsWidth = SadConsole.Game.Instance.Window.ClientBounds.Width - (264);
            int windowPixelsHeight = SadConsole.Game.Instance.Window.ClientBounds.Height - (384);

            int newW = (windowPixelsWidth / cellX);
            int newH = (windowPixelsHeight / cellY);

            MapConsole.Resize(newW, newH, false);

            CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);
            
            MapConsole.IsDirty = true;
        }

        public void CreateMapWindow(int width, int height, string title, bool reset = false) {
            MapWindow = new Window(width, height);

            int mapConsoleWidth = width - 2;
            int mapConsoleHeight = height - 2;
            
            MapConsole.Position = new Point(1, 1);
            
            
            MapWindow.Title = title.Align(HorizontalAlignment.Center, mapConsoleWidth, (char) 205);
            MapWindow.Children.Add(MapConsole);
            
            Children.Add(MapWindow);
            
            MapConsole.MouseButtonClicked += mapClick;
            MapConsole.MouseMove += mapMove;

            MapWindow.CanDrag = true;
            MapWindow.IsVisible = true;

            
        }

        public void mapMove(object sender, MouseEventArgs e) {
            Mouse = e.MouseState.ConsoleCellPosition;
        }
        
        private void mapClick(object sender, MouseEventArgs e) {
            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                int mouseIndex = (Global.MouseState.ScreenPosition.PixelLocationToConsole(12, 12) - new Point(1, 1)).ToIndex(MapConsole.Width);
                Point modifiedClick = e.MouseState.ConsoleCellPosition;

                if (latestRegion != null && latestRegion.Length > mouseIndex && mouseIndex >= 0) {
                    modifiedClick = latestRegion[mouseIndex].literalPos;
                }

                int range = (int) Distance.CHEBYSHEV.Calculate(player.literalPosition, modifiedClick);
                Monster monster = GameLoop.World.CurrentMap.GetEntityAt<Monster>(modifiedClick); 

                if (monster != null) {
                    if ((player.Equipped[0] != null && range <= Convert.ToInt32(player.Equipped[0].Properties["range"])) || range <= 1) {
                        GameLoop.CommandManager.Attack(player, monster);
                    }
                } else {
                    if (range <= 1) {
                        TileBase tile = GameLoop.World.CurrentMap.GetTileAt<TileBase>(modifiedClick.X, modifiedClick.Y);
                        

                        if (tile is TileDoor door) {
                            if (!door.IsOpen)
                                GameLoop.CommandManager.OpenDoor(player, door, modifiedClick);
                            else
                                GameLoop.CommandManager.CloseDoor(player, modifiedClick, true);

                        }
                        
                        if (new List<string> { "cornflower", "rose", "violet", "dandelion", "tulip" }.Contains(tile.Name)) {
                            Item flower = GameLoop.ItemLibrary[tile.Name].Clone();
                            flower.literalPosition = modifiedClick;
                            GameLoop.World.CurrentMap.Add(flower);

                            if (GameLoop.World.CurrentMap.GetTileAt<TileBase>(modifiedClick) != null) {
                                GameLoop.World.CurrentMap.NewTiles[modifiedClick] = GameLoop.TileLibrary["grass"].Clone();
                            }

                            string serialFlower = JsonConvert.SerializeObject(flower, Formatting.Indented, new ItemJsonConverter());

                            string itemDrop = "i_data|drop|" + flower.literalPosition.X + "|" + flower.literalPosition.Y + "|" + serialFlower;
                            GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(itemDrop));

                            string tileUpdate = "t_data|flower_picked|" + flower.literalPosition.X + "|" + flower.literalPosition.Y;
                            GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(tileUpdate));
                                    
                            player.PickupItem(flower); 
                            RefreshMap(player.literalPosition);
                        }

                        if (player.Equipped[13] != null) {
                            player.Equipped[13].UseItem(player, modifiedClick);
                        }
                    }
                    
                }
            }
        }

          
        private void hostButtonClick(object sender, SadConsole.Input.MouseEventArgs e) {
            GameLoop.NetworkingManager.changeClientTarget("0"); // HAS TO BE DISABLED ON LIVE BUILD, ONLY FOR TESTING TWO CLIENTS ON ONE COMPUTER


            GameLoop.IsHosting = true;

            var lobbyManager = GameLoop.NetworkingManager.discord.GetLobbyManager(); 
            string possibleChars = "ABCDEFGHIJKLMNPQRSTUVWXYZ0123456789";
            string code = "";
            
            code = "";

            for (int i = 0; i < 4; i++) {
                code += possibleChars[GameLoop.Random.Next(0, possibleChars.Length)];
            }


            switch(GameLoop.NetworkingManager.myUID) {
                case 98983408474013696:
                    code = "HALLIDAY";
                    break;
                case 98984121312743424:
                    code = "ROOMCODE";
                    break;
                case 193245791232458752:
                    code = "COBLAT";
                    break;
                default:
                    break;
            }



            var txn = lobbyManager.GetLobbyCreateTransaction();
            txn.SetCapacity(6);
            txn.SetType(LobbyType.Public);
            txn.SetMetadata("code", code);
            txn.SetMetadata("public", "false");
            txn.SetMetadata("friendsOnly", "true");
            txn.SetMetadata("worldName", GameLoop.World.WorldName);
            txn.SetMetadata("hostName", GameLoop.World.players[GameLoop.NetworkingManager.myUID].Name);


            lobbyManager.CreateLobby(txn, (Result result, ref Lobby lobby) => {
                if (result == Result.Ok) {
                    MessageLog.Add("Created lobby! Press ESC to see code.");
                    

                    GameLoop.NetworkingManager.InitNetworking(lobby.Id);
                    lobbyManager.OnMemberConnect += onPlayerConnected;
                    lobbyManager.OnMemberDisconnect += onPlayerDisconnected;
                    
                    JOIN_CODE = code;

                    EscapeMenuWindow.Title = ("[MENU: " + code + "]").Align(HorizontalAlignment.Center, EscapeMenuWindow.Width - 2, (char) 205);
                } else {
                    MessageLog.Add("Error: " + result);
                }
            });
        }

        private void onPlayerDisconnected(long lobbyId, long userId) {
            var userManager = GameLoop.NetworkingManager.discord.GetUserManager();
            userManager.GetUser(userId, (Result result, ref User user) => {
                if (result == Discord.Result.Ok) {
                    MessageLog.Add("User disconnected: " + user.Username);
                    GameLoop.World.CurrentMap.Remove(GameLoop.World.players[user.Id]);
                    GameLoop.World.players.Remove(user.Id);
                }
            });
        }

        private void onPlayerConnected(long lobbyId, long userId) {
            var userManager = GameLoop.NetworkingManager.discord.GetUserManager();
            userManager.GetUser(userId, (Result result, ref User user) => {
                if (result == Discord.Result.Ok) {
                    MessageLog.Add("User connected: " + user.Username);
                    kickstartNet();

                    GameLoop.World.CreatePlayer(userId, new Player(Color.Yellow, Color.Transparent));
                    SyncMapEntities(true);
                    // GameLoop.NetworkingManager.SendNetMessage(2, System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(GameLoop.World, Formatting.Indented, new WorldJsonConverter())));

                    GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes("p_list|" + JsonConvert.SerializeObject(GameLoop.World.players, Formatting.Indented, new ActorJsonConverter())));


                    var timeString = "time|" + GameLoop.TimeManager.Year + "|" + GameLoop.TimeManager.Season + "|" + GameLoop.TimeManager.Day + "|" + GameLoop.TimeManager.Hour + "|" + GameLoop.TimeManager.Minute;
                    GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(timeString)); 

                    MapConsole.IsDirty = true;
                }
            });
        }

        private void kickstartNet() {
            GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes("a"));
            GameLoop.NetworkingManager.SendNetMessage(1, System.Text.Encoding.UTF8.GetBytes("a"));
            GameLoop.NetworkingManager.SendNetMessage(2, System.Text.Encoding.UTF8.GetBytes("a"));
        }

        private void joinButtonClick(object sender, SadConsole.Input.MouseEventArgs e) {
            GameLoop.NetworkingManager.changeClientTarget("1"); // HAS TO BE DISABLED ON LIVE BUILD, ONLY FOR TESTING TWO CLIENTS ON ONE COMPUTER

            joinBox.IsFocused = false;
            

            var lobbyManager = GameLoop.NetworkingManager.discord.GetLobbyManager();
            LobbySearchQuery searchQ = lobbyManager.GetSearchQuery();
            searchQ.Filter("metadata.code", LobbySearchComparison.Equal, LobbySearchCast.String, joinBox.Text);

            string acSec = "";

            lobbyManager.Search(searchQ, (resultSearch) => {
                if (resultSearch == Discord.Result.Ok) {
                    var count = lobbyManager.LobbyCount();

                    acSec = lobbyManager.GetLobbyActivitySecret(lobbyManager.GetLobbyId(0));



                    lobbyManager.ConnectLobbyWithActivitySecret(acSec, (Result result, ref Lobby lobby) => {
                        if (result == Discord.Result.Ok) {
                            MessageLog.Add("Connected to lobby successfully!");
                            GameLoop.NetworkingManager.InitNetworking(lobby.Id);
                            kickstartNet();
                            GameLoop.World.CreatePlayer(GameLoop.NetworkingManager.myUID, new Player(Color.Yellow, Color.Transparent));
                        } else {
                            MessageLog.Add("Encountered error: " + result);
                        }
                    });
                }
            });
        }

        public void CenterOnActor(Actor actor, bool checkEntityPos = true) {
            RefreshMap(actor.literalPosition, checkEntityPos);
        }

        public void RefreshMap(Point pos, bool checkEntityPos = true) {
            latestRegion = GameLoop.World.CurrentMap.GetTileRegion(pos);
            MapConsole.SetSurface(latestRegion, MapConsole.Width, MapConsole.Height);
            
            MapConsole.IsDirty = true;
        }



        public void CheckEntities() {
            if (latestRegion != null) {
                CameraOrigin = latestRegion[0].literalPos;
                CameraEnd = latestRegion[latestRegion.Length - 1].literalPos;
            }

            if (latestRegion != null && latestRegion.Length > 1) {
                for (int i = 0; i < latestRegion.Length; i++) {
                    if (GameLoop.World.CurrentMap.GetEntitiesAt<Entity>(latestRegion[i].literalPos) != null) {
                        foreach (Entity entity in GameLoop.World.CurrentMap.GetEntitiesAt<Entity>(latestRegion[i].literalPos)) {
                            if (entity.Position != i.ToPoint(MapConsole.Width)) {
                                entity.Position = i.ToPoint(MapConsole.Width);
                                entity.PositionOffset = new Point(0, 0);
                            }
                        }
                    }
                }
            }

            if (STATE == "GAME" && MapConsole.Children.Count > 0) {
                for (int i = MapConsole.Children.Count - 1; i >= 0; i--) {
                    if (MapConsole.Children[i] is Entity entity) {
                        if (!Utils.PointInArea(CameraOrigin, CameraEnd, entity.literalPosition)) {
                            MapConsole.Children.Remove(entity);
                        }
                    }
                }
            }

        }


        public void SyncMapEntities(bool keepOldView = false) {
            MapConsole.Children.Clear();

            if (latestRegion != null) {
                CameraOrigin = latestRegion[0].literalPos;
                CameraEnd = latestRegion[latestRegion.Length - 1].literalPos;
            }

            foreach (Entity entity in GameLoop.World.CurrentMap.Entities.Items) {
                if (Utils.PointInArea(CameraOrigin, CameraEnd, entity.literalPosition)) {
                    MapConsole.Children.Add(entity);
                }
                
            }

            foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                MapConsole.Children.Add(player.Value);
            }

            GameLoop.World.CurrentMap.Entities.ItemAdded += OnMapEntityAdded;
            GameLoop.World.CurrentMap.Entities.ItemRemoved += OnMapEntityRemoved;

            GameLoop.World.ResetFOV(keepOldView);
        }

        public void OnMapEntityAdded (object sender, GoRogue.ItemEventArgs<Entity> args) {
            MapConsole.Children.Add(args.Item);
        }

        public void OnMapEntityRemoved(object sender, GoRogue.ItemEventArgs<Entity> args) {
            MapConsole.Children.Remove(args.Item);
        }

        public void LoadMap(Map map) {
            GameLoop.World.CurrentMap = map;

            Point center = new Point(0, 0);

            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                center = GameLoop.World.players[GameLoop.NetworkingManager.myUID].literalPosition;
            }



            MapConsole.Clear();
            MapConsole.Children.Clear();


            //MapConsole = new ScrollingConsole(map.Width, map.Height, Global.FontDefault, new Rectangle(0, 0, MapWindow.Width - 2, MapWindow.Height - 2), GameLoop.World.CurrentMap.Tiles);
            MapConsole = new Console(MapWindow.Width - 2, MapWindow.Height - 2, Global.FontDefault);
            MapConsole.Parent = MapWindow;
            MapConsole.Position = new Point(1, 1);
            MapConsole.MouseButtonClicked += mapClick;
            MapConsole.MouseMove += mapMove;

            SyncMapEntities(false);
        }

        public void InitNewMap(string worldName = "Unnamed", bool justInit = false) {
            GameLoop.World.WorldName = worldName;


            CreateConsoles();

            MessageLog = new MessageLogWindow(60, 20, "[MESSAGE LOG]");
            MessageLog.Title.Align(HorizontalAlignment.Center, MessageLog.Title.Length);
            Children.Add(MessageLog);
            MessageLog.IsVisible = false;
            MessageLog.Position = new Point(0, 40); 
            
            CreateMapWindow(GameLoop.GameWidth - 20, GameLoop.GameHeight - 30, "[GAME MAP]");
            StatusWindow = new CloseableWindow(20, 40, "[PLAYER INFO]");
            StatusWindow.Position = new Point(GameLoop.GameWidth - 20, 0);
            
            
            ContextWindow = new CloseableWindow(20, 20, "[ITEM MENU]");
            ContextWindow.Position = new Point (GameLoop.GameWidth - 20, 40);
            ContextWindow.console.MouseButtonClicked += contextClick;


            MapWindow.IsVisible = false;
            MapConsole.IsVisible = false;


            InventoryWindow = new CloseableWindow(59, 28, "[INVENTORY]");
            InventoryWindow.console.MouseButtonClicked += invMouseClick;


            EquipmentWindow = new CloseableWindow(30, 16, "[EQUIPMENT]");
            EquipmentWindow.console.MouseButtonClicked += eqpMouseClick;


            Children.Add(StatusWindow);
            Children.Add(ContextWindow);
            Children.Add(InventoryWindow);
            Children.Add(EquipmentWindow);

            if (!justInit) {
                GameLoop.World.CreatePlayer(GameLoop.NetworkingManager.myUID, new Player(Color.Yellow, Color.Transparent));
                LoadMap(GameLoop.World.CurrentMap);
                MapConsole.Font = GameLoop.RegularSize;
                CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);
            }
        }

        public void SplashAnim() {
            if (SplashConsole.IsVisible) {
                if (raindrops < 100 && GameLoop.Random.Next(0, 10) < 7) {
                    raindrops++;

                    Entity newRaindrop = new Entity(new Color(GameLoop.CyberBlue, GameLoop.Random.Next(170, 256)), Color.Transparent, '\\', 2, 2);
                    newRaindrop.Font = GameLoop.RegularSize;
                    int hori = GameLoop.Random.Next(0, 4);
                    if (hori == 0 || hori == 1) {
                        newRaindrop.Position = new Point(0, GameLoop.Random.Next(0, SplashConsole.Height - 14));
                    } else {
                        newRaindrop.Position = new Point(GameLoop.Random.Next(0, SplashConsole.Width - 2), 0);
                    }
                    rain.Add(newRaindrop);

                    SplashRain.Children.Add(newRaindrop);
                }


                for (int i = rain.Count - 1; i >= 0; i--) {
                    if (rain[i].Position.X + 1 < SplashConsole.Width - 2 && rain[i].Position.Y + 1 < SplashConsole.Height - 14) {
                        rain[i].Position += new Point(1, 1);
                    } else {
                        int hori = GameLoop.Random.Next(0, 4);
                        if (hori == 0 || hori == 1) {
                            rain[i].Position = new Point(0, GameLoop.Random.Next(0, SplashConsole.Height - 14));
                        } else {
                            rain[i].Position = new Point(GameLoop.Random.Next(0, SplashConsole.Width - 2), 0);
                        }
                    }
                }
            }
        }


        public void LoadSplash() {

            SadRex.Image splashScreen = SadRex.Image.Load(new FileStream("./data/splash.xp", FileMode.Open));
            Cell[] splashCells = new Cell[splashScreen.Width * splashScreen.Height];
            int logoCount = 0;
            int lastY = 2;

            for (int i = 0; i < splashScreen.Layers[0].Cells.Count; i++) {
                SadRex.Cell temp = splashScreen.Layers[0].Cells[i];
                Color fore = new Color(temp.Foreground.R, temp.Foreground.G, temp.Foreground.B);
                Color back = new Color(temp.Background.R, temp.Background.G, temp.Background.B);

                if (fore == new Color(17, 17, 17) || fore == new Color(26, 26, 26)) { fore.A = 150; } 
                if (fore == new Color(35, 35, 35) || fore == new Color(48, 48, 48)) { fore.A = 170; } 
                if (fore == new Color(69, 69, 69) || fore == new Color(78, 78, 78)) { fore.A = 190; } 
                if (fore == new Color(255, 0, 255)) { fore.A = 0; } 
                if (back == new Color(255, 0, 255)) {  back.A = 0; }

                ColorGradient splashGrad = new ColorGradient(Color.DeepPink, Color.Cyan);
                Point cellPos = i.ToPoint(splashScreen.Width);

                if (fore == new Color(178, 0, 178)) {
                    if (lastY < cellPos.Y) {
                        logoCount++;
                        lastY = cellPos.Y;
                    }
                    fore = splashGrad.Lerp((float)logoCount / 5);
                }

                splashCells[i] = new Cell(fore, back, temp.Character);
            }

            SplashConsole = new Console(splashScreen.Width, splashScreen.Height, GameLoop.RegularSize, splashCells);
            SplashConsole.IsVisible = true;
            Children.Add(SplashConsole);

            Cell[] rain = new Cell[(SplashConsole.Width - 2) * (SplashConsole.Height - 14)];

            for (int i = 0; i < rain.Length; i++) {
                rain[i] = new Cell(Color.Transparent, Color.Transparent, ' ');
            }

            SplashRain = new Console(SplashConsole.Width - 2, SplashConsole.Height - 14, Global.FontDefault, rain);
            SplashRain.Position = new Point(1, 1);
            SplashConsole.Children.Add(SplashRain);


            Cell[] menu = new Cell[(SplashConsole.Width - 2) * 12];

            for (int i = 0; i < menu.Length; i++) {
                menu[i] = new Cell(Color.White, Color.Transparent, ' ');
            }

            MenuConsole = new Console(SplashConsole.Width - 2, 12, GameLoop.RegularSize, menu);
            SplashConsole.Children.Add(MenuConsole);

            MenuConsole.Position = new Point(1, SplashConsole.Height - 12);
            MenuConsole.Print(1, 1, "CREATE NEW WORLD".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));
            MenuConsole.Print(1, 2, "CREATE CHARACTER".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));
            MenuConsole.Print(1, 3, "LOAD WORLD".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));

            MenuConsole.Print(1, 5, "SERVER BROWSER".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));
            MenuConsole.Print(1, 7, "CREATOR TOOL".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));
            MenuConsole.Print(1, 8, "SETTINGS".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));

            MenuConsole.Print(1, 10, "EXIT".Align(HorizontalAlignment.Center, 76, ' ').CreateColored(GameLoop.CyberBlue, Color.Transparent));

            MenuConsole.MouseButtonClicked += menuClick; 
        }

        private void CreateWorld() {
            WorldCreateWindow = new CloseableWindow(20, 10, "[CREATE WORLD]");
            WorldCreateWindow.Position = new Point(GameLoop.GameWidth / 2 - 10, GameLoop.GameHeight / 2 - 5);
            
            WorldCreateConsole = new ControlsConsole(18, 8, Global.FontDefault);
            WorldCreateConsole.Position = new Point(1, 1);
            WorldCreateConsole.IsVisible = false;
            WorldCreateConsole.FocusOnMouseClick = true;

            WorldNameBox = new TextBox(16);
            WorldNameBox.Position = new Point(1, 1);
            WorldNameBox.IsVisible = true;
            WorldNameBox.UseKeyboard = true;
            WorldNameBox.TextAlignment = HorizontalAlignment.Center;


            CreateWorldButton = new Button(18, 1);
            CreateWorldButton.Text = "CREATE".Align(HorizontalAlignment.Center, 18, ' ');
            CreateWorldButton.Position = new Point(0, 7);
            CreateWorldButton.IsVisible = true;
            CreateWorldButton.MouseButtonClicked += createWorldButtonClicked;

            WorldCreateWindow.Children.Add(WorldCreateConsole);
            WorldCreateConsole.Add(WorldNameBox);
            WorldCreateConsole.Add(CreateWorldButton);
            Children.Add(WorldCreateWindow); 
        }

        private void createWorldButtonClicked(object sender, MouseEventArgs e) {
            WorldNameBox.Text = WorldNameBox.EditingText;
            if (WorldNameBox.Text != "") {
                WorldCreateConsole.IsVisible = false;
                WorldCreateWindow.IsVisible = false;
                WorldNameBox.IsVisible = false;
                CreateWorldButton.IsVisible = false;


                InitNewMap(WorldNameBox.Text);
                SaveEverything();

                LoadWindow.SetVisibility(true);

                UpdateLoadWindow();
            }
        }

        private void SaveEverything(bool alsoSavePlayers = false) {
            Directory.CreateDirectory(@"./saves/worlds/" + GameLoop.World.WorldName);

            // string map = Utils.SimpleMapString(GameLoop.World.CurrentMap.Tiles);

            string map = JsonConvert.SerializeObject(GameLoop.World, Formatting.Indented, new WorldJsonConverter());

            File.WriteAllText(@"./saves/worlds/" + GameLoop.World.WorldName + "/map.json", map);

            if (alsoSavePlayers) {
                Directory.CreateDirectory(@"./saves/players");

                foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                    string playerJson = JsonConvert.SerializeObject(player.Value, Formatting.Indented, new ActorJsonConverter());
                    File.WriteAllText(@"./saves/players/" + player.Value.Name + "+" + player.Key + ".json", playerJson);
                }
            }
        }

        private void LoadMapFromFile(string path) {
            InitNewMap("", true);
            string map = File.ReadAllText(path + "/map.json");
            
            GameLoop.World = JsonConvert.DeserializeObject<World>(map, new WorldJsonConverter());
            
            //LoadMap(GameLoop.World.CurrentMap);
        }


        public void LoadWorldDialogue() {
            LoadWindow = new CloseableWindow(40, 20, "[CHOOSE WORLD]");
            LoadWindow.Position = new Point(GameLoop.GameWidth / 2 - 20, GameLoop.GameHeight / 2 - 10);
            LoadWindow.console.MouseButtonClicked += loadWorldClick;

            Directory.CreateDirectory(@"./saves"); 
            string[] savelist = Directory.GetDirectories(@"./saves");
            int saveCount = 0;

            foreach (string dir in savelist) {
                string[] split = dir.Split('\\');

                Color selected;


                if (dir == selectedWorldName) {
                    selected = Color.Lime;
                } else {
                    selected = Color.DarkSlateGray;
                }

                LoadWindow.console.Print(0, saveCount, (split[split.Length - 1].Align(HorizontalAlignment.Center, 38, ' ')).CreateColored(selected));
                saveCount++;
            }

            Children.Add(LoadWindow);
        }

        private void UpdateLoadWindow() {
            LoadWindow.console.Clear();

            Directory.CreateDirectory(@"./saves/worlds");
            string[] savelist = Directory.GetDirectories(@"./saves/worlds");

            int saveCount = 0;

            string selectedName = "";

            foreach (string dir in savelist) {
                
                string[] split = dir.Split('\\');

                Color selected;

                if (dir == selectedWorldName) {
                    selected = Color.Lime;
                    selectedName = split[split.Length - 1];
                } else {
                    selected = Color.DarkSlateGray;
                }

                LoadWindow.console.Print(0, saveCount, (split[split.Length - 1].Align(HorizontalAlignment.Center, 38, ' ')).CreateColored(selected));
                saveCount++;
            }


            if (selectedWorldName != "") {
                LoadWindow.console.Print(0, 14, ("Load " + selectedName).Align(HorizontalAlignment.Center, 38, ' '));
                LoadWindow.console.Print(0, 15, (("Delete " + selectedName).Align(HorizontalAlignment.Center, 38, ' ')).CreateColored(Color.DarkRed));

                if (tryDelete) {
                    LoadWindow.console.Print(0, 16, (("Hold SHIFT and press Y to Confirm").Align(HorizontalAlignment.Center, 38, ' ')).CreateColored(Color.Red));
                }
            }

            LoadWindow.console.Print(0, 17, "BACK TO MENU".Align(HorizontalAlignment.Center, 38, ' '));
        }

        private void loadWorldClick(object sender, MouseEventArgs e) {
            Directory.CreateDirectory(@"./saves/worlds");
            string[] savelist = Directory.GetDirectories(@"./saves/worlds");

            if (e.MouseState.ConsoleCellPosition.Y < savelist.Length) { 
                selectedWorldName = savelist[e.MouseState.ConsoleCellPosition.Y];
            }


            if (e.MouseState.ConsoleCellPosition.Y == 14) {
                LoadMapFromFile(selectedWorldName);

                LoadWindow.SetVisibility(false);

                List<Player> allSaved = new List<Player>();

                Directory.CreateDirectory(@"./saves/players");
                string[] allPlayers = Directory.GetFiles(@"./saves/players");
                
                for (int i = 0; i < allPlayers.Length; i++) {
                    if (allPlayers[i].Contains(GameLoop.NetworkingManager.myUID.ToString())) {
                        Actor actor = JsonConvert.DeserializeObject<Actor>(File.ReadAllText(allPlayers[i]), new ActorJsonConverter());
                        allSaved.Add(new Player(actor, actor.literalPosition));
                    }
                }


                NoWorldChar = false;

                //ChangeState("GAME");

                CharLoadInit(allSaved);
            }

            if (e.MouseState.ConsoleCellPosition.Y == 15) {
                if (!tryDelete) {
                    tryDelete = true;
                }
            }

            if (e.MouseState.ConsoleCellPosition.Y == 17) {
                LoadWindow.SetVisibility(false);

                selectedWorldName = "";
                tryDelete = false;
            }

           
            if (e.MouseState.ConsoleCellPosition.Y != 15 && e.MouseState.ConsoleCellPosition.Y != 16) {
                tryDelete = false;
            }

            UpdateLoadWindow();
        }


        public void CharLoadInit(List<Player> players) {
            CharLoadWindow.SetVisibility(true);
            currPlayerChars = players;
        }


        private void loadCharClick(object sender, MouseEventArgs e) {
            if (e.MouseState.ConsoleCellPosition.Y == 0) {
                CharLoadWindow.SetVisibility(false);

                CharCreateWindow.IsVisible = true;
                CharCreateConsole.IsVisible = true;
                CharOptionsConsole.IsVisible = true;
                CharInfoConsole.IsVisible = true;
            }


            if (e.MouseState.ConsoleCellPosition.Y-2 < currPlayerChars.Count && e.MouseState.ConsoleCellPosition.Y-2 >= 0) {
                characterToLoad = currPlayerChars[e.MouseState.ConsoleCellPosition.Y-2];
            }


            if (e.MouseState.ConsoleCellPosition.Y == 14) {
                LoadMap(GameLoop.World.CurrentMap);
                ChangeState("GAME");
                GameLoop.World.CreatePlayer(GameLoop.NetworkingManager.myUID, characterToLoad);

                hostButtonClick(null, null);
                

                SyncMapEntities();
                
            }

            if (e.MouseState.ConsoleCellPosition.Y == 15) {
                if (!tryDelete) {
                    tryDelete = true;
                }
            }

            if (e.MouseState.ConsoleCellPosition.Y == 17) {
                CharLoadWindow.SetVisibility(false);
                tryDelete = false;
            }


            if (e.MouseState.ConsoleCellPosition.Y != 15 && e.MouseState.ConsoleCellPosition.Y != 16) {
                tryDelete = false;
            }

            UpdateLoadCharWindow();
        }


        private void UpdateLoadCharWindow() {
            CharLoadWindow.console.Clear();

            CharLoadWindow.console.Print(0, 0, ("[NEW CHARACTER]").Align(HorizontalAlignment.Center, 38, ' ').CreateColored(GameLoop.CyberBlue));

            for (int i = 0; i < currPlayerChars.Count; i++) {

                Color selected = Color.DarkSlateGray;
                if (characterToLoad != null && currPlayerChars[i].Name == characterToLoad.Name) {
                    selected = Color.Lime;
                }


                CharLoadWindow.console.Print(0, i+2, currPlayerChars[i].Name.Align(HorizontalAlignment.Center, 38, ' ').CreateColored(selected));
            }

            if (characterToLoad != null) {
                CharLoadWindow.console.Print(0, 14, ("Load " + characterToLoad.Name).Align(HorizontalAlignment.Center, 38, ' '));
                CharLoadWindow.console.Print(0, 15, (("Delete " + characterToLoad.Name).Align(HorizontalAlignment.Center, 38, ' ')).CreateColored(Color.DarkRed));

                if (tryDelete) {
                    CharLoadWindow.console.Print(0, 16, (("Hold SHIFT and press Y to Confirm").Align(HorizontalAlignment.Center, 38, ' ')).CreateColored(Color.Red));
                }
            }

            CharLoadWindow.console.Print(0, 17, "BACK TO MENU".Align(HorizontalAlignment.Center, 38, ' '));
        }



        public void CharacterCreationDialogue() {
            CharCreateWindow = new Window(80, 60, Global.FontDefault);
            CharCreateWindow.Position = new Point(0, 0);
            CharCreateWindow.IsVisible = false;
            CharCreateWindow.UseMouse = true;
            CharCreateWindow.Title = "[CREATE CHARACTER]".Align(HorizontalAlignment.Center, 78, (char)205);

            CharCreateConsole = new ControlsConsole(18, 60, Global.FontDefault);
            CharCreateConsole.Position = new Point(1, 0);
            CharCreateConsole.IsVisible = false;

            CharOptionsConsole = new ScrollingConsole(36, 60, Global.FontDefault, new Rectangle(0, 0, 36, 58));
            CharOptionsConsole.Position = new Point(19, 1);
            CharOptionsConsole.IsVisible = false;
            
            CharInfoConsole = new ControlsConsole(26, 60, Global.FontDefault);
            CharInfoConsole.Position = new Point(53, 0);
            CharInfoConsole.IsVisible = false;

            
            CharCreateConsole.MouseButtonClicked += charCreateClick;
            CharOptionsConsole.MouseButtonClicked += charOptionsClick;

            
            CharCreateWindow.Children.Add(CharCreateConsole);
            CharCreateWindow.Children.Add(CharOptionsConsole);
            CharCreateWindow.Children.Add(CharInfoConsole);
            Children.Add(CharCreateWindow);


            CreatingChar = new Player(Color.Yellow, Color.Transparent);

            CharCreateConsole.Children.Add(CreatingChar);
            CreatingChar.Position = new Point(1, 8);
            CreatingChar.UpdateFontSize(Font.FontSizes.Four);

            skinColors = new Color[] { new Color(255, 219, 172), new Color(241, 194, 125), new Color(224, 172, 105), new Color(198, 134, 66), new Color(141, 85, 36), new Color(63, 32, 0) };
            hairColors = new Color[] { Color.PaleGoldenrod, Color.OrangeRed, Color.HotPink, Color.Indigo, Color.SandyBrown, Color.Silver, Color.Snow };
            skinIndex = 0;
            hairIndex = 0;
            hairStyleIndex = 4;

            PlayerNameBox = new TextBox(10);
            PlayerNameBox.Position = new Point(6, 3);
            PlayerNameBox.FocusOnClick = true;
            CharCreateConsole.FocusOnMouseClick = true;
            CharCreateConsole.Add(PlayerNameBox);

            foreach (KeyValuePair<string, Skill> skill in GameLoop.SkillLibrary) {
                SkillNames.Add(skill.Key);
            }

            foreach (KeyValuePair<string, CharacterClass> charClass in GameLoop.ClassLibrary) {
                ClassNames.Add(charClass.Key);
            }

            foreach (KeyValuePair<string, CharacterRace> charRace in GameLoop.RaceLibrary) {
                RaceNames.Add(charRace.Key);
            }
        }


        public void UpdateCharCreation() {
            CharCreateConsole.Clear();
            

            if (SelectedClass != "(NONE)") {
                selectClass = GameLoop.ClassLibrary[SelectedClass];
            }

            if (RaceNames.Count == 1) {
                SelectedRace = "Human";
            }

            CharCreateConsole.DrawLine(new Point(17, 1), new Point(17, 58), GameLoop.LogoColors[3], Color.Transparent, (char)186);
            CharCreateConsole.DrawLine(new Point(0, 0), new Point(16, 0), GameLoop.LogoColors[3], Color.Transparent, (char)205);
            CharCreateConsole.DrawLine(new Point(0, 59), new Point(16, 59), GameLoop.LogoColors[3], Color.Transparent, (char)205);
            CharCreateConsole.Print(17, 0, new ColoredGlyph((char)203, GameLoop.LogoColors[3], Color.Transparent));
            CharCreateConsole.Print(17, 59, new ColoredGlyph((char)202, GameLoop.LogoColors[3], Color.Transparent));

            CharCreateConsole.Print(0, 1, " CHARACTER SHEET ".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 2, (((char)205).ToString().Align(HorizontalAlignment.Center, 17, (char)205)).CreateColored(GameLoop.LogoColors[3]));


            CharCreateConsole.Print(0, 3, "Name: ".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 4, " < Skin  Color >".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 5, " <  Eye  Color >".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 6, " < Hair  Style >".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 7, " < Hair  Color >".CreateColored(GameLoop.CyberBlue));
            

            CharCreateConsole.Print(0, 25, (((char)205).ToString().Align(HorizontalAlignment.Center, 17, (char)205)).CreateColored(GameLoop.LogoColors[3]));
            string str = CreatingChar.Strength.ToString().Align(HorizontalAlignment.Right, 2, '0'); 
            CharCreateConsole.Print(0, 26, "   STR  - " + str + " +   ".CreateColored(GameLoop.CyberBlue)); 
            string dex = CreatingChar.Dexterity.ToString().Align(HorizontalAlignment.Right, 2, '0');
            CharCreateConsole.Print(0, 27, "   DEX  - " + dex + " +   ".CreateColored(GameLoop.CyberBlue)); 
            string con = CreatingChar.Constitution.ToString().Align(HorizontalAlignment.Right, 2, '0');
            CharCreateConsole.Print(0, 28, "   CON  - " + con + " +   ".CreateColored(GameLoop.CyberBlue)); 
            string intelligence = CreatingChar.Intelligence.ToString().Align(HorizontalAlignment.Right, 2, '0');
            CharCreateConsole.Print(0, 29, "   INT  - " + intelligence + " +   ".CreateColored(GameLoop.CyberBlue)); 
            string wis = CreatingChar.Wisdom.ToString().Align(HorizontalAlignment.Right, 2, '0');
            CharCreateConsole.Print(0, 30, "   WIS  - " + wis + " +   ".CreateColored(GameLoop.CyberBlue)); 
            string cha = CreatingChar.Charisma.ToString().Align(HorizontalAlignment.Right, 2, '0');
            CharCreateConsole.Print(0, 31, "   CHA  - " + cha + " +   ".CreateColored(GameLoop.CyberBlue));


            List<int> pointCosts = new List<int> { -4, -2, -1, 0, 1, 2, 3, 5, 7, 10, 13, 17 };
            int modified = (pointCosts[CreatingChar.Strength - 7]) + (pointCosts[CreatingChar.Dexterity - 7]) + (pointCosts[CreatingChar.Constitution - 7]) + (pointCosts[CreatingChar.Intelligence - 7]) + (pointCosts[CreatingChar.Wisdom - 7]) + (pointCosts[CreatingChar.Charisma - 7]);
            string pts = (25 - modified).ToString().Align(HorizontalAlignment.Right, 2, '0');


            CharCreateConsole.Print(0, 33, ("Points Left: " + pts).Align(HorizontalAlignment.Center, 17, ' ').CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 34, (((char)205).ToString().Align(HorizontalAlignment.Center, 17, (char)205)).CreateColored(GameLoop.LogoColors[3]));


            CreatingChar.BasicLift = (int)Math.Floor(((double) CreatingChar.Strength * CreatingChar.Strength) / 2);
            string maxCarry = "Max Carry: " + CreatingChar.BasicLift.ToString().Align(HorizontalAlignment.Right, 3, ' ') + " kg";
            CharCreateConsole.Print(0, 36, maxCarry.CreateColored(GameLoop.CyberBlue));

            CreatingChar.UpdateRanksPerLvl();
            string spentSkills = CreatingChar.SpentSkillPoints().ToString().Align(HorizontalAlignment.Right, 2, '0');
            string maxRanks;

            if (SelectedClass != "(NONE)") {
                maxRanks = ((CreatingChar.Level * CreatingChar.RanksPerLvl) + selectClass.RanksPerLv).ToString().Align(HorizontalAlignment.Right, 2, '0');
            } else {
                maxRanks = (CreatingChar.Level * CreatingChar.RanksPerLvl).ToString().Align(HorizontalAlignment.Right, 2, '0');
            }



            Color SelectRace;
            if (SelectedRace != "(NONE)") {
                SelectRace = Color.Lime;
            } else {
                SelectRace = Color.Red;
            }

            CharCreateConsole.Print(0, 39, ("Race: " + SelectedRace).CreateColored(SelectRace));


            Color selectedAClass;
            if (SelectedClass != "(NONE)") {
                selectedAClass = Color.Lime;
            } else {
                selectedAClass = Color.Red;
            }

            CharCreateConsole.Print(0, 40, ("Class: " + SelectedClass).CreateColored(selectedAClass));



            Color spentAllSkills;

            if (spentSkills == maxRanks) {
                spentAllSkills = Color.Lime;
            } else if (Convert.ToInt32(spentSkills) > Convert.ToInt32(maxRanks)) {
                spentAllSkills = Color.Red;
            } else {
                spentAllSkills = GameLoop.CyberBlue;
            }

            CharCreateConsole.Print(0, 41, ("Skills/Lv (" + spentSkills + "/" + maxRanks + ")").CreateColored(spentAllSkills));

            


            CharCreateConsole.Print(0, 42, "Advantages".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 43, "Disadvantages".CreateColored(GameLoop.CyberBlue));
            CharCreateConsole.Print(0, 44, "Traits".CreateColored(GameLoop.CyberBlue));

            CharCreateConsole.Print(0, 55, "DONE".Align(HorizontalAlignment.Center, 15, ' ').CreateColored(GameLoop.CyberBlue));




            CharCreateConsole.Print(0, 57, " RETURN TO MENU".CreateColored(GameLoop.CyberBlue));


            CharInfoConsole.DrawLine(new Point(0, 1), new Point(0, 58), GameLoop.LogoColors[3], Color.Transparent, (char)186);
            CharInfoConsole.DrawLine(new Point(0, 0), new Point(25, 0), GameLoop.LogoColors[3], Color.Transparent, (char)205);
            CharInfoConsole.DrawLine(new Point(0, 59), new Point(25, 59), GameLoop.LogoColors[3], Color.Transparent, (char)205);
            CharInfoConsole.Print(0, 0, new ColoredGlyph((char)203, GameLoop.LogoColors[3], Color.Transparent));
            CharInfoConsole.Print(0, 59, new ColoredGlyph((char)202, GameLoop.LogoColors[3], Color.Transparent));

            CharInfoConsole.Print(1, 1, "EXTRA DETAILS".Align(HorizontalAlignment.Center, 25, ' ').CreateColored(GameLoop.CyberBlue));
            CharInfoConsole.Print(1, 2, (((char)205).ToString().Align(HorizontalAlignment.Center, 25, (char)205)).CreateColored(GameLoop.LogoColors[3]));
            
            
        }

        public void UpdateCharOptions() {
            CharOptionsConsole.Clear();
            if (CreationSelectedOption == "") {
                CharOptionsConsole.Print(7, 25, "Click an option on the left".CreateColored(GameLoop.CyberBlue));
                CharOptionsConsole.Print(10, 36, "panel to get started!".CreateColored(GameLoop.CyberBlue));
            }

            if (CreationSelectedOption == "Skills") {
                CharOptionsConsole.Print(0, 0, "SKILL NAME".Align(HorizontalAlignment.Center, 20, ' ').CreateColored(GameLoop.CyberBlue));
                CharOptionsConsole.Print(20, 0, (((char)186).ToString() + " STAT " + ((char)186).ToString() + " RANK ").CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(0, 1, (((char)205).ToString().Align(HorizontalAlignment.Center, 34, (char)205)).CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(20, 1, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(27, 1, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));

                int printLine = 2;
                foreach (KeyValuePair<string, Skill> skill in GameLoop.SkillLibrary) {
                    Color skillNameColor;

                    if (skill.Key == SelectedSkill) {
                        skillNameColor = Color.Lime;
                    } else {
                        skillNameColor = GameLoop.CyberBlue;
                    }

                    CharOptionsConsole.Print(0, printLine, skill.Value.Name.Align(HorizontalAlignment.Center, 20, ' ').CreateColored(skillNameColor));

                    string ranks = CreatingChar.Skills[skill.Key].Ranks.ToString().Align(HorizontalAlignment.Right, 2, '0');

                    CharOptionsConsole.Print(20, printLine, (((char)186).ToString() + " " + skill.Value.ControllingAttribute + "  " + ((char)186).ToString() + " -" + ranks + "+ ").CreateColored(skillNameColor));

                    printLine++;
                }
            }

            if (CreationSelectedOption == "Classes") {
                CharOptionsConsole.Print(0, 0, "CLASS NAME".Align(HorizontalAlignment.Center, 19, ' ').CreateColored(GameLoop.CyberBlue));
                CharOptionsConsole.Print(19, 0, (((char)186).ToString() + "  HD  " + ((char)186).ToString() + " SKILL ").CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(0, 1, (((char)205).ToString().Align(HorizontalAlignment.Center, 60, (char)205)).CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(19, 1, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(26, 1, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));

                int printLine = 2;
                foreach (KeyValuePair<string, CharacterClass> charClass in GameLoop.ClassLibrary) {
                    Color nameColor;

                    if (charClass.Key == SelectedClass) {
                        nameColor = Color.Lime;
                    } else {
                        nameColor = GameLoop.CyberBlue;
                    }

                    CharOptionsConsole.Print(0, printLine, charClass.Value.ClassName.Align(HorizontalAlignment.Center, 19, ' ').CreateColored(nameColor));

                    string hitdie = charClass.Value.HitDie.Align(HorizontalAlignment.Right, 3, ' ');
                    CharOptionsConsole.Print(19, printLine, (((char)186).ToString() + " " + hitdie + "  " + ((char)186).ToString() + " " + charClass.Value.RanksPerLv + "+INT ").CreateColored(nameColor));




                    printLine++;
                }


                if (SelectedClass != "(NONE)") {
                    selectClass = GameLoop.ClassLibrary[SelectedClass];

                    CharInfoConsole.Clear(new Rectangle(new Point(1, 3), new Point(25, 56)));


                    CharInfoConsole.Print(1, 3, ("Class Name: " + SelectedClass).CreateColored(GameLoop.CyberBlue));

                    string fortSave = selectClass.FRW_InitialSaves[0].ToString().Align(HorizontalAlignment.Right, 2, '+') + " (+" + selectClass.FortSave.ToString().Align(HorizontalAlignment.Left, 4, '0') + "/lv)";
                    string reflexSave = selectClass.FRW_InitialSaves[1].ToString().Align(HorizontalAlignment.Right, 2, '+') + " (+" + selectClass.ReflexSave.ToString().Align(HorizontalAlignment.Left, 4, '0') + "/lv)";
                    string willSave = selectClass.FRW_InitialSaves[2].ToString().Align(HorizontalAlignment.Right, 2, '+') + " (+" + selectClass.WillSave.ToString().Align(HorizontalAlignment.Left, 4, '0') + "/lv)";

                    CharInfoConsole.Print(1, 5, ("Fortitude:  " + fortSave).CreateColored(GameLoop.CyberBlue));
                    CharInfoConsole.Print(1, 6, ("Reflex:     " + reflexSave).CreateColored(GameLoop.CyberBlue));
                    CharInfoConsole.Print(1, 7, ("Will:       " + willSave).CreateColored(GameLoop.CyberBlue));

                    string atkPerLv = selectClass.ClassAttackBonus.ToString();
                    if (!atkPerLv.Contains(".")) { atkPerLv += "."; }

                    string attackBonus = "+" + selectClass.ClassAttackBonus.ToString()[0].ToString() + " (+" + atkPerLv.Align(HorizontalAlignment.Left, 4, '0') + "/lv)";
                    CharInfoConsole.Print(1, 9, ("Atk Bonus:  " + attackBonus).CreateColored(GameLoop.CyberBlue));



                    string[] classSkills = selectClass.ClassSkills.Split(',');

                    string skillLine = "";
                    int skillPrint = 14;

                    CharInfoConsole.Print(1, 11, ((char)205).ToString().Align(HorizontalAlignment.Center, 25, (char)205).CreateColored(GameLoop.LogoColors[3]));
                    CharInfoConsole.Print(1, 12, "Class Skills".Align(HorizontalAlignment.Center, 25, ' ').CreateColored(GameLoop.CyberBlue));
                    CharInfoConsole.Print(1, 13, ((char)205).ToString().Align(HorizontalAlignment.Center, 25, (char)205).CreateColored(GameLoop.LogoColors[3]));
                    for (int i = 0; i < classSkills.Length; i++) {
                        if (classSkills[i][0] == ' ') {
                            classSkills[i] = classSkills[i].Substring(1);
                        }

                        if (skillLine.Length + classSkills[i].Length + 2 >= 25) {
                            CharInfoConsole.Print(1, skillPrint, skillLine.CreateColored(GameLoop.CyberBlue));
                            skillPrint++;
                            skillLine = "";
                        }

                        skillLine += classSkills[i];

                        if (i + 1 != classSkills.Length) {
                            skillLine += ", ";
                        }

                        if (i + 1 == classSkills.Length) {
                            CharInfoConsole.Print(1, skillPrint, skillLine.CreateColored(GameLoop.CyberBlue));
                        }
                    }
                }
            }


            if (CreationSelectedOption == "Races") {
                CharOptionsConsole.Print(0, 0, "RACE NAME".Align(HorizontalAlignment.Center, 12, ' ').CreateColored(GameLoop.CyberBlue));
                CharOptionsConsole.Print(12, 0, (((char)186).ToString() + "ABILITY MODS").CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(0, 1, (((char)205).ToString().Align(HorizontalAlignment.Center, 35, (char)205)).CreateColored(GameLoop.LogoColors[3]));
                CharOptionsConsole.Print(12, 1, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));

                int printLine = 2;
                foreach (KeyValuePair<string, CharacterRace> charRace in GameLoop.RaceLibrary) {
                    Color raceColor;

                    if (charRace.Key == SelectedClass) {
                        raceColor = Color.Lime;
                    } else {
                        raceColor = GameLoop.CyberBlue;
                    }


                    CharOptionsConsole.Print(0, printLine, charRace.Value.RaceName.Align(HorizontalAlignment.Center, 12, ' ').CreateColored(raceColor));
                    CharOptionsConsole.Print(12, printLine, (((char)186).ToString()).CreateColored(GameLoop.LogoColors[3]));
                    printLine++;
                }
            }
        }



        public void UpdateServerBrowser() {
            ServerBrowserWindow.console.Clear();

            ServerBrowserWindow.console.Print(0, 0, (" [REFRESH] [JOIN] [FRIENDS]    [CODE SEARCH:            ] ").CreateColored(GameLoop.CyberBlue));

            ServerBrowserWindow.console.Print(0, 1, ((char)205).ToString().Align(HorizontalAlignment.Center, 58, (char)205).CreateColored(GameLoop.LogoColors[3]));
            ServerBrowserWindow.console.Print(0, 2, ("   CODE   ").CreateColored(GameLoop.CyberBlue));
            ServerBrowserWindow.console.Print(10, 2, ((char)186).ToString().CreateColored(GameLoop.LogoColors[3]));
            ServerBrowserWindow.console.Print(12, 2, "WORLD NAME".Align(HorizontalAlignment.Center, 20, ' ').CreateColored(GameLoop.CyberBlue));
            ServerBrowserWindow.console.Print(32, 2, ((char)186).ToString().CreateColored(GameLoop.LogoColors[3]));
            ServerBrowserWindow.console.Print(34, 2, "HOST NAME".Align(HorizontalAlignment.Center, 20, ' ').CreateColored(GameLoop.CyberBlue));

            ServerBrowserWindow.console.Print(10, 1, ((char)203).ToString().CreateColored(GameLoop.LogoColors[3]));
            ServerBrowserWindow.console.Print(32, 1, ((char)203).ToString().CreateColored(GameLoop.LogoColors[3]));

            ServerBrowserWindow.console.Print(0, 3, ((char)205).ToString().Align(HorizontalAlignment.Center, 58, (char) 205).CreateColored(GameLoop.LogoColors[3]));

            if (SearchResults.Count == 0) {
                ServerBrowserWindow.console.Print(10, 3, ((char)202).ToString().CreateColored(GameLoop.LogoColors[3]));
                ServerBrowserWindow.console.Print(32, 3, ((char)202).ToString().CreateColored(GameLoop.LogoColors[3]));
            } else {
                ServerBrowserWindow.console.Print(10, 3, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));
                ServerBrowserWindow.console.Print(32, 3, ((char)206).ToString().CreateColored(GameLoop.LogoColors[3]));
            }

            

            int printLoc = 4;

            for (int i = 0; i < SearchResults.Count; i++) {
                string[] resultData = SearchResults[i].Split('|');

                Color selectedLobby = GameLoop.CyberBlue;
                if (i == selectedCode) {
                    selectedLobby = Color.Lime;
                }

                ServerBrowserWindow.console.Print(0, printLoc + i, resultData[0].Align(HorizontalAlignment.Center, 10, ' ').CreateColored(selectedLobby));
                ServerBrowserWindow.console.Print(10, printLoc + i, ((char) 186).ToString().CreateColored(selectedLobby));
                ServerBrowserWindow.console.Print(12, printLoc + i, resultData[1].Align(HorizontalAlignment.Center, 20, ' ').CreateColored(selectedLobby));
                ServerBrowserWindow.console.Print(32, printLoc + i, ((char)186).ToString().CreateColored(selectedLobby));
                ServerBrowserWindow.console.Print(34, printLoc + i, resultData[2].Align(HorizontalAlignment.Center, 20, ' ').CreateColored(selectedLobby));
            }
        }

        public void serverBrowserClick(object sender, MouseEventArgs e) {
            if (Utils.PointInArea(new Point (1, 0), new Point(8, 0), e.MouseState.ConsoleCellPosition)) { // Refresh current search
                SearchResults.Clear();

                GameLoop.NetworkingManager.changeClientTarget("1");

                var lobbyManager = GameLoop.NetworkingManager.discord.GetLobbyManager();
                LobbySearchQuery searchQ = lobbyManager.GetSearchQuery();
                searchQ.Filter("metadata.friendsOnly", LobbySearchComparison.Equal, LobbySearchCast.String, "false");
                searchQ.Filter("metadata.public", LobbySearchComparison.Equal, LobbySearchCast.String, "true");

                lobbyManager.Search(searchQ, (resultSearch) => {
                    if (resultSearch == Discord.Result.Ok) {
                        var count = lobbyManager.LobbyCount();

                        for (int i = 0; i < count; i++) {
                            long lobbyId = lobbyManager.GetLobbyId(i);

                            string code = lobbyManager.GetLobbyMetadataValue(lobbyId, "code");
                            string world = lobbyManager.GetLobbyMetadataValue(lobbyId, "worldName");
                            string host = lobbyManager.GetLobbyMetadataValue(lobbyId, "hostName");

                            SearchResults.Add(code + "|" + world + "|" + host + "|" + lobbyManager.GetLobbyActivitySecret(lobbyManager.GetLobbyId(i)));
                        }
                    }
                });
            }

            else if (Utils.PointInArea(new Point(10, 0), new Point(16, 0), e.MouseState.ConsoleCellPosition) && selectedCode != -1) { // Join selected lobby
                var lobbyManager = GameLoop.NetworkingManager.discord.GetLobbyManager();
                lobbyManager.ConnectLobbyWithActivitySecret(SearchResults[selectedCode].Split('|')[3], (Result result, ref Lobby lobby) => {
                    if (result == Discord.Result.Ok) {
                        InitNewMap("", true);
                        ChangeState("GAME");
                        
                        MessageLog.Add("Connected to lobby successfully!");
                        GameLoop.NetworkingManager.InitNetworking(lobby.Id);
                        kickstartNet();
                        GameLoop.World.CreatePlayer(GameLoop.NetworkingManager.myUID, new Player(Color.Yellow, Color.Transparent));
                    } else {
                        MessageLog.Add("Encountered error: " + result);
                    }
                });
            } 
            
            else if (Utils.PointInArea(new Point(18, 0), new Point(26, 0), e.MouseState.ConsoleCellPosition)) { // Search for friend lobbies
                SearchResults.Clear();

                GameLoop.NetworkingManager.changeClientTarget("1");

                var lobbyManager = GameLoop.NetworkingManager.discord.GetLobbyManager();
                LobbySearchQuery searchQ = lobbyManager.GetSearchQuery();
                
                searchQ.Filter("metadata.friendsOnly", LobbySearchComparison.Equal, LobbySearchCast.String, "true");


                GameLoop.NetworkingManager.relationshipManager = GameLoop.NetworkingManager.discord.GetRelationshipManager();


                GameLoop.NetworkingManager.relationshipManager.OnRefresh += () => {
                    GameLoop.NetworkingManager.relationshipManager.Filter((ref Relationship relationship) => {
                        return relationship.Type == Discord.RelationshipType.Friend;
                    });
                };

                GameLoop.NetworkingManager.relationshipManager.OnRelationshipUpdate += (ref Discord.Relationship relationship) => {
                };


                lobbyManager.Search(searchQ, (resultSearch) => {
                    if (resultSearch == Discord.Result.Ok) {
                        var count = lobbyManager.LobbyCount();
                        List<long> friendIDs = new List<long>();
                        
                        for (var i = 0; i < GameLoop.NetworkingManager.relationshipManager.Count(); i++) {
                            var r = GameLoop.NetworkingManager.relationshipManager.GetAt((uint)i);
                            if (r.Type == RelationshipType.Friend || r.Type == RelationshipType.Implicit) {
                                friendIDs.Add(r.User.Id);
                            }
                        }

                        for (int i = 0; i < count; i++) {
                            long lobbyId = lobbyManager.GetLobbyId(i);

                            string code = lobbyManager.GetLobbyMetadataValue(lobbyId, "code");
                            string world = lobbyManager.GetLobbyMetadataValue(lobbyId, "worldName");
                            string host = lobbyManager.GetLobbyMetadataValue(lobbyId, "hostName");
                            
                            if (friendIDs.Contains(lobbyManager.GetLobby(lobbyManager.GetLobbyId(i)).OwnerId)) {
                                SearchResults.Add(code + "|" + world + "|" + host + "|" + lobbyManager.GetLobbyActivitySecret(lobbyManager.GetLobbyId(i)));
                            }
                        }
                    }
                });
            } 
            
            else if (Utils.PointInArea(new Point(29, 0), new Point(41, 0), e.MouseState.ConsoleCellPosition)) { // Search with a new code
                SearchResults.Clear();

                GameLoop.NetworkingManager.changeClientTarget("1");

                var lobbyManager = GameLoop.NetworkingManager.discord.GetLobbyManager();
                LobbySearchQuery searchQ = lobbyManager.GetSearchQuery();

                joinBox.Text = joinBox.EditingText;

                searchQ.Filter("metadata.code", LobbySearchComparison.Equal, LobbySearchCast.String, joinBox.Text);

                lobbyManager.Search(searchQ, (resultSearch) => {
                    if (resultSearch == Discord.Result.Ok) {
                        var count = lobbyManager.LobbyCount();

                        System.Console.WriteLine(lobbyManager.LobbyCount());

                        for (int i = 0; i < count; i++) {
                            long lobbyId = lobbyManager.GetLobbyId(i);

                            string code = lobbyManager.GetLobbyMetadataValue(lobbyId, "code");
                            string world = lobbyManager.GetLobbyMetadataValue(lobbyId, "worldName");
                            string host = lobbyManager.GetLobbyMetadataValue(lobbyId, "hostName");

                            SearchResults.Add(code + "|" + world + "|" + host + "|" + lobbyManager.GetLobbyActivitySecret(lobbyManager.GetLobbyId(i)));
                        }
                    }
                });
            }

            else if (e.MouseState.ConsoleCellPosition.Y - 4 >= 0 && e.MouseState.ConsoleCellPosition.Y - 4 < SearchResults.Count) {
                selectedCode = e.MouseState.ConsoleCellPosition.Y - 4;
            } else {
                selectedCode = -1;
            }
        }



        public void UpdateSettingsWindow() {

        }

        public void UpdateCreatorToolWindow() {
            CreatorToolWindow.console.Clear();

            CreatorToolWindow.console.Print(0, 0, " [METADATA]    [RACE] [CLASS] [SKILL] [ADVANTAGE] [TRAIT]".CreateColored(GameLoop.CyberBlue));
            CreatorToolWindow.console.Print(0, 1, "                  [ITEM] [ENEMY] [TILE] [TERRAIN FEATURE]".CreateColored(GameLoop.CyberBlue));
            CreatorToolWindow.console.Print(0, 1, "                        [ABILITY] [RECIPE] [CONSTRUCTION]".CreateColored(GameLoop.CyberBlue));
            CreatorToolWindow.console.Print(0, 3, ((char)205).ToString().Align(HorizontalAlignment.Center, 58, (char) 205).CreateColored(GameLoop.LogoColors[3]));


            if (SelectedTool == "metadata") {
                CreatorToolWindow.console.Print(0, 4, "Name: ".CreateColored(GameLoop.CyberBlue));
                CreatorToolWindow.console.Print(0, 5, "Author: ".CreateColored(GameLoop.CyberBlue));
                CreatorToolWindow.console.Print(0, 6, "Description: ".CreateColored(GameLoop.CyberBlue));
                CreatorToolWindow.console.Print(0, 7, "Name: ".CreateColored(GameLoop.CyberBlue));

                // save button

                // load sections: check /data/mods/ for all directories, grab metadata.json from each directory and use it to display info
            }

            if (SelectedTool == "race") {

            }

            if (SelectedTool == "class") {

            }

            if (SelectedTool == "skill") {

            }

            if (SelectedTool == "advantage") {

            }

            if (SelectedTool == "trait") {

            }

            if (SelectedTool == "item") {

            }

            if (SelectedTool == "enemy") {

            }

            if (SelectedTool == "tile") {

            }

            if (SelectedTool == "terrainfeature") {

            }

            if (SelectedTool == "ability") {

            }

            if (SelectedTool == "recipe") {

            }

            if (SelectedTool == "construction") {

            }
        }






        private void charOptionsClick(object sender, MouseEventArgs e) {
            if (CreationSelectedOption == "Skills") {
                if (SkillNames.Count > e.MouseState.ConsoleCellPosition.Y - 2 && e.MouseState.ConsoleCellPosition.Y - 2 >= 0) {
                    SelectedSkill = SkillNames[e.MouseState.ConsoleCellPosition.Y - 2];
                }

                if (e.MouseState.ConsoleCellPosition.X == 29) {
                    if (SelectedSkill != "") {
                        if (CreatingChar.Skills[SelectedSkill].Ranks > 0) {
                            CreatingChar.Skills[SelectedSkill].Ranks--;
                        }
                    }
                }

                if (e.MouseState.ConsoleCellPosition.X == 32) {
                    if (SelectedSkill != "") {
                        if (CreatingChar.Skills[SelectedSkill].Ranks < CreatingChar.Level) {
                            CreatingChar.Skills[SelectedSkill].Ranks++;
                        }
                    }
                }
            }



            if (CreationSelectedOption == "Classes") {
                if (ClassNames.Count > e.MouseState.ConsoleCellPosition.Y - 2 && e.MouseState.ConsoleCellPosition.Y - 2 >= 0) {
                    SelectedClass = ClassNames[e.MouseState.ConsoleCellPosition.Y - 2];
                }
            }


            if (CreationSelectedOption == "Races") {
                if (RaceNames.Count > e.MouseState.ConsoleCellPosition.Y - 2 && e.MouseState.ConsoleCellPosition.Y - 2 >= 0) {
                    SelectedRace = RaceNames[e.MouseState.ConsoleCellPosition.Y - 2];
                }
            }
        }

        private void charCreateClick(object sender, MouseEventArgs e) {
            if (e.MouseState.ConsoleCellPosition == new Point(1, 4)) {
                if (skinIndex - 1 == -1) { skinIndex = skinColors.Length - 1; }
                else { skinIndex--; }

                CreatingChar.Animation[0].Foreground = skinColors[skinIndex];
                CreatingChar.Animation.IsDirty = true;
            }

            if (e.MouseState.ConsoleCellPosition == new Point(15, 4)) {
                if (skinIndex + 1 == skinColors.Length) { skinIndex = 0; } else { skinIndex++; }

                CreatingChar.Animation[0].Foreground = skinColors[skinIndex];
                CreatingChar.Animation.IsDirty = true;
            }

            if (e.MouseState.ConsoleCellPosition == new Point(1, 7)) {
                if (hairIndex - 1 == -1) { hairIndex = hairColors.Length - 1; } else { hairIndex--; }

                CreatingChar.decorators["hair"] = new CellDecorator(hairColors[hairIndex], CreatingChar.decorators["hair"].Glyph, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
                CreatingChar.RefreshDecorators();
                CreatingChar.Animation.IsDirty = true;
            }

            if (e.MouseState.ConsoleCellPosition == new Point(15, 7)) {
                if (hairIndex + 1 == hairColors.Length) { hairIndex = 0; } else { hairIndex++; }

                CreatingChar.decorators["hair"] = new CellDecorator(hairColors[hairIndex], CreatingChar.decorators["hair"].Glyph, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
                CreatingChar.RefreshDecorators();
                CreatingChar.Animation.IsDirty = true;
            }




            if (e.MouseState.ConsoleCellPosition == new Point(1, 6)) {
                if (hairStyleIndex - 1 == 4) { hairStyleIndex = 9; } else { hairStyleIndex--; }

                CreatingChar.decorators["hair"] = new CellDecorator(hairColors[hairIndex], (char) hairStyleIndex, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
                CreatingChar.RefreshDecorators();
                CreatingChar.Animation.IsDirty = true;
            }

            if (e.MouseState.ConsoleCellPosition == new Point(15, 6)) {
                if (hairStyleIndex + 1 == 10) { hairStyleIndex = 4; } else { hairStyleIndex++; }

                CreatingChar.decorators["hair"] = new CellDecorator(hairColors[hairIndex], (char)hairStyleIndex, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
                CreatingChar.RefreshDecorators();
                CreatingChar.Animation.IsDirty = true;
            }




            if (e.MouseState.ConsoleCellPosition == new Point(8, 26) && CreatingChar.Strength > 7) { CreatingChar.Strength--; } 
            if (e.MouseState.ConsoleCellPosition == new Point(13, 26) && CreatingChar.Strength < 18) { CreatingChar.Strength++; }

            if (e.MouseState.ConsoleCellPosition == new Point(8, 27) && CreatingChar.Dexterity > 7) { CreatingChar.Dexterity--; }
            if (e.MouseState.ConsoleCellPosition == new Point(13, 27) && CreatingChar.Dexterity < 18) { CreatingChar.Dexterity++; }

            if (e.MouseState.ConsoleCellPosition == new Point(8, 28) && CreatingChar.Constitution > 7) { CreatingChar.Constitution--; }
            if (e.MouseState.ConsoleCellPosition == new Point(13, 28) && CreatingChar.Constitution < 18) { CreatingChar.Constitution++; }

            if (e.MouseState.ConsoleCellPosition == new Point(8, 29) && CreatingChar.Intelligence > 7) { CreatingChar.Intelligence--; }
            if (e.MouseState.ConsoleCellPosition == new Point(13, 29) && CreatingChar.Intelligence < 18) { CreatingChar.Intelligence++; }

            if (e.MouseState.ConsoleCellPosition == new Point(8, 30) && CreatingChar.Wisdom > 7) { CreatingChar.Wisdom--; }
            if (e.MouseState.ConsoleCellPosition == new Point(13, 30) && CreatingChar.Wisdom < 18) { CreatingChar.Wisdom++; }

            if (e.MouseState.ConsoleCellPosition == new Point(8, 31) && CreatingChar.Charisma > 7) { CreatingChar.Charisma--; }
            if (e.MouseState.ConsoleCellPosition == new Point(13, 31) && CreatingChar.Charisma < 18) { CreatingChar.Charisma++; }

            if (e.MouseState.ConsoleCellPosition.Y == 39) { CreationSelectedOption = "Races"; }
            if (e.MouseState.ConsoleCellPosition.Y == 40) { CreationSelectedOption = "Classes"; }
            if (e.MouseState.ConsoleCellPosition.Y == 41) { CreationSelectedOption = "Skills"; }
            if (e.MouseState.ConsoleCellPosition.Y == 42) { CreationSelectedOption = "Advantages"; }
            if (e.MouseState.ConsoleCellPosition.Y == 43) { CreationSelectedOption = "Disadvantages"; }
            if (e.MouseState.ConsoleCellPosition.Y == 44) { CreationSelectedOption = "Traits"; }

            if (e.MouseState.ConsoleCellPosition.Y == 55) {
                if (selectClass != null && selectClass.CheckEligibility(CreatingChar)) {
                    selectClass.LevelClass(CreatingChar);
                    PlayerNameBox.Text = PlayerNameBox.EditingText;
                    CreatingChar.Name = PlayerNameBox.Text;

                    if (!NoWorldChar) {
                        LoadMap(GameLoop.World.CurrentMap);
                        ChangeState("GAME");
                        hostButtonClick(null, null);

                        if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                            if (MapConsole.Children.Contains(GameLoop.World.players[GameLoop.NetworkingManager.myUID]))
                                MapConsole.Children.Remove(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);
                            GameLoop.World.players.Remove(GameLoop.NetworkingManager.myUID);
                        }

                        CharCreateConsole.Children.Remove(CreatingChar);


                        CreatingChar.Position = new Point(0, 0);
                        CreatingChar.UpdateFontSize(hold.SizeMultiple);
                        GameLoop.World.CreatePlayer(GameLoop.NetworkingManager.myUID, CreatingChar);

                        SyncMapEntities();
                    }

                    CharCreateWindow.IsVisible = false;
                    CharCreateConsole.IsVisible = false;
                    CharInfoConsole.IsVisible = false;
                    CharOptionsConsole.IsVisible = false;


                    Directory.CreateDirectory(@"./saves/players");
                    
                    string playerJson = JsonConvert.SerializeObject(CreatingChar, Formatting.Indented, new ActorJsonConverter()); 
                    File.WriteAllText(@"./saves/players/" + CreatingChar.Name + "+" + GameLoop.NetworkingManager.myUID + ".json", playerJson);
                }
            }

            if (e.MouseState.ConsoleCellPosition.Y == 57) {
                CharCreateWindow.IsVisible = false;
                CharCreateConsole.IsVisible = false;
                CharOptionsConsole.IsVisible = false;
                CharInfoConsole.IsVisible = false;
            }
        }

        private void menuClick(object sender, MouseEventArgs e) {

            if (Utils.PointInArea(new Point(31, 1), new Point(47, 1), e.MouseState.ConsoleCellPosition)) { // Open world creation dialogue 
                WorldCreateWindow.IsVisible = true;
                WorldCreateConsole.IsVisible = true;
            }

            else if (Utils.PointInArea(new Point(34, 3), new Point(44, 3), e.MouseState.ConsoleCellPosition)) { // Open world loading dialogue
                LoadWindow.SetVisibility(true);

                UpdateLoadWindow();
            }

            else if (Utils.PointInArea(new Point(31, 2), new Point(47, 2), e.MouseState.ConsoleCellPosition)) { // Open character creation dialogue
                CharacterCreationDialogue();

                CharCreateWindow.IsVisible = true;
                CharCreateConsole.IsVisible = true;
                CharInfoConsole.IsVisible = true;
                CharOptionsConsole.IsVisible = true;

                NoWorldChar = true;
            }

            else if (Utils.PointInArea(new Point(32, 5), new Point(46, 5), e.MouseState.ConsoleCellPosition)) { // Open the Server Browser
                ServerBrowserWindow.SetVisibility(true);
            } 
            
            else if (Utils.PointInArea(new Point(33, 7), new Point(45, 7), e.MouseState.ConsoleCellPosition)) { // Open the Creator Tools
                CreatorToolWindow.SetVisibility(true);
            } 
            
            else if (Utils.PointInArea(new Point(35, 8), new Point(43, 8), e.MouseState.ConsoleCellPosition)) { // Open the Settings menu
                SettingsWindow.SetVisibility(true);
            } 
            
            else if (Utils.PointInArea(new Point(37, 10), new Point(41, 10), e.MouseState.ConsoleCellPosition)) { // Should open a dialogue that lets the player specify whether to use a new world or load an existing world
                SadConsole.Game.Instance.Exit();
            }
        }

        private void ClearWait(Actor actor) {
            waitingForCommand = "";
            GameLoop.CommandManager.ResetPeek(actor);
        }

        private void ChangeState(string newState) {
            STATE = newState;
            if (STATE == "MENU") {
                MapConsole.IsVisible = false;
                MapWindow.IsVisible = false;

                InventoryWindow.SetVisibility(false);
                StatusWindow.SetVisibility(false);
                EquipmentWindow.SetVisibility(false);


                SplashConsole.IsVisible = true;
                SplashRain.IsVisible = true;
                joinBox.IsVisible = true;
                joinPrompt.IsVisible = true;


                GameLoop.IsHosting = false;
            }

            if (STATE == "GAME") {
                MapConsole.IsVisible = true;
                MapWindow.IsVisible = true;
                MessageLog.IsVisible = true;
                
                StatusWindow.SetVisibility(true);

                SplashConsole.IsVisible = false;
                SplashRain.IsVisible = false;
                joinBox.IsVisible = false;
                joinPrompt.IsVisible = false;

                LoadWindow.SetVisibility(false);

                CharLoadWindow.SetVisibility(false);
                
                CharCreateWindow.IsVisible = false;
                CharCreateConsole.IsVisible = false;
                CharOptionsConsole.IsVisible = false;
                CharInfoConsole.IsVisible = false;
            }
        }


        private void CheckKeyboard() {
            if (STATE == "MENU") {
                if (Global.KeyboardState.IsKeyDown(Keys.LeftShift) && Global.KeyboardState.IsKeyReleased(Keys.Y) && tryDelete && LoadWindow.IsVisible) {
                    Directory.Delete(selectedWorldName, true);
                    selectedWorldName = "";
                    UpdateLoadWindow();
                }

                if (Global.KeyboardState.IsKeyDown(Keys.LeftShift) && Global.KeyboardState.IsKeyReleased(Keys.Y) && tryDelete && CharLoadWindow.IsVisible) {
                    File.Delete(selectedWorldName + "/players/" + characterToLoad.Name + "+" + GameLoop.NetworkingManager.myUID + ".json");
                    characterToLoad = null;
                    
                } 
            }

            if (STATE == "GAME") {
                
                if (Global.KeyboardState.IsKeyReleased(Keys.F5)) {
                    for (var i = 0; i < GameLoop.NetworkingManager.relationshipManager.Count(); i++) {
                        var r = GameLoop.NetworkingManager.relationshipManager.GetAt((uint)i);
                        System.Console.WriteLine("relationships: {0} {1}", r.Type, r.User.Username);
                    }
                }

                if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                    Player player = GameLoop.World.players[GameLoop.NetworkingManager.myUID];
                    if (Global.KeyboardState.IsKeyPressed(Keys.G)) {
                        waitingForCommand = "g";
                    }

                    if (Global.KeyboardState.IsKeyPressed(Keys.X)) {
                        if (GameLoop.CommandManager.lastPeek == new Point(0, 0)) {
                            waitingForCommand = "x";
                        } else {
                            ClearWait(player);
                        }
                    }

                    if (Global.KeyboardState.IsKeyPressed(Keys.C)) {
                        if (Global.KeyboardState.IsKeyDown(Keys.LeftShift)) {
                            if (StatusWindow.IsVisible) {
                                StatusWindow.SetVisibility(false);
                            } else {
                                StatusWindow.SetVisibility(true);
                            }
                        } else {
                            waitingForCommand = "c";
                        }
                    }

                    if (Global.KeyboardState.IsKeyReleased(Keys.OemTilde)) {
                        SaveEverything(true);
                    }

                    if (Global.KeyboardState.IsKeyPressed(Keys.E)) {
                        if (EquipmentWindow.IsVisible) {
                            EquipmentWindow.SetVisibility(false);
                        } else {
                            EquipmentWindow.SetVisibility(true);
                        }
                    }

                    if (Global.KeyboardState.IsKeyPressed(Keys.I)) {
                        if (InventoryWindow.IsVisible) {
                            InventoryWindow.SetVisibility(false);
                            ContextWindow.SetVisibility(false);
                            invContextIndex = -1;
                        } else {
                            InventoryWindow.SetVisibility(true);
                            ContextWindow.SetVisibility(true);
                            invContextIndex = -1;
                        }
                    }

                    if (Global.KeyboardState.IsKeyPressed(Keys.S) && (Global.KeyboardState.IsKeyDown(Keys.LeftShift) || Global.KeyboardState.IsKeyPressed(Keys.LeftShift))) {
                        if (!player.IsStealthing) {
                            int skillCheck = player.SkillCheck("stealth", 0);
                            player.Stealth(skillCheck, true);
                            GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes("stealth|yes|" + GameLoop.NetworkingManager.myUID + "|" + skillCheck));
                        } else {
                            player.Unstealth();
                            GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes("stealth|no|" + GameLoop.NetworkingManager.myUID + "|0"));
                        }
                    }

                    if (Global.KeyboardState.IsKeyReleased(Keys.H)) {
                        SyncMapEntities();
                    }

                    if (Global.KeyboardState.IsKeyReleased(Keys.Escape)) {
                        if (EscapeMenuWindow.IsVisible) {
                            EscapeMenuWindow.SetVisibility(false);
                        } else {
                            EscapeMenuWindow.SetVisibility(true);
                        }
                    }

                    if (Global.KeyboardState.IsKeyReleased(Keys.OemPlus)) {
                        if (zoom != 8) {
                            switch (zoom) {
                                case 1:
                                    MapConsole.Font = GameLoop.MapHalf;
                                    hold = GameLoop.MapHalf;
                                    zoom = 2;
                                    ResizeMap();
                                    break;
                                case 2:
                                    MapConsole.Font = GameLoop.MapOne;
                                    hold = GameLoop.MapOne;
                                    zoom = 4;
                                    ResizeMap();
                                    break;
                                case 4:
                                    MapConsole.Font = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.Two);
                                    hold = Global.Fonts["Cheepicus48"].GetFont(Font.FontSizes.Two);
                                    zoom = 8;
                                    ResizeMap();
                                    break;
                            }

                            foreach (Entity entity in GameLoop.World.CurrentMap.Entities.Items) {
                                entity.UpdateFontSize(hold.SizeMultiple);
                                entity.literalPosition = entity.literalPosition;
                                entity.IsDirty = true;
                            }

                            foreach (KeyValuePair<long, Player> entity in GameLoop.World.players) {
                                entity.Value.UpdateFontSize(hold.SizeMultiple);
                                entity.Value.literalPosition = entity.Value.literalPosition;
                                entity.Value.IsDirty = true;
                            }

                            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                                CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);
                            }

                            GameLoop.UIManager.SyncMapEntities();
                            MapConsole.IsDirty = true;
                        }
                    }

                    if (Global.KeyboardState.IsKeyReleased(Keys.OemMinus)) {
                        if (zoom != 1) {
                            switch (zoom) {
                                case 8:
                                    MapConsole.Font = GameLoop.MapOne;
                                    hold = GameLoop.MapOne;
                                    zoom = 4;
                                    ResizeMap();
                                    break;
                                case 4: 
                                    MapConsole.Font = GameLoop.MapHalf;
                                    hold = GameLoop.MapHalf;
                                    zoom = 2;
                                    ResizeMap();
                                    break;
                                case 2:
                                    MapConsole.Font = GameLoop.MapQuarter;
                                    hold = GameLoop.MapQuarter;
                                    zoom = 1;
                                    ResizeMap();
                                    break;
                            }

                            foreach (Entity entity in GameLoop.World.CurrentMap.Entities.Items) {
                                entity.UpdateFontSize(hold.SizeMultiple);
                                entity.literalPosition = entity.literalPosition;
                                entity.IsDirty = true;
                            }

                            foreach (KeyValuePair<long, Player> entity in GameLoop.World.players) {
                                entity.Value.UpdateFontSize(hold.SizeMultiple);
                                entity.Value.literalPosition = entity.Value.literalPosition;
                                entity.Value.IsDirty = true;
                            }


                            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                                CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);
                            }

                            GameLoop.UIManager.SyncMapEntities();
                            MapConsole.IsDirty = true;

                        }
                    }



                    if (player.TimeLastActed + (UInt64)player.Speed <= GameLoop.GameTime) {
                        if (Global.KeyboardState.IsKeyPressed(Keys.NumPad9)) {
                            Point thisDir = Utils.Directions["UR"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }

                        } else if (Global.KeyboardState.IsKeyPressed(Keys.W) || Global.KeyboardState.IsKeyPressed(Keys.NumPad8)) {
                            Point thisDir = Utils.Directions["U"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }

                        } else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad7)) {
                            Point thisDir = Utils.Directions["UL"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }

                        } else if (Global.KeyboardState.IsKeyPressed(Keys.D) || Global.KeyboardState.IsKeyPressed(Keys.NumPad6)) {
                            Point thisDir = Utils.Directions["R"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }
                        } else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad5)) {
                            Point thisDir = Utils.Directions["C"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }
                        } else if (Global.KeyboardState.IsKeyPressed(Keys.A) || Global.KeyboardState.IsKeyPressed(Keys.NumPad4)) {
                            Point thisDir = Utils.Directions["L"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }
                        } else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad3)) {
                            Point thisDir = Utils.Directions["DR"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }
                        } else if ((Global.KeyboardState.IsKeyPressed(Keys.S) && !Global.KeyboardState.IsKeyDown(Keys.LeftShift)) || Global.KeyboardState.IsKeyPressed(Keys.NumPad2)) {
                            Point thisDir = Utils.Directions["D"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }
                        } else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad1)) {
                            Point thisDir = Utils.Directions["DL"];
                            if (waitingForCommand == "") {
                                ClearWait(player);
                                GameLoop.CommandManager.MoveActorBy(player, thisDir);
                                CenterOnActor(player);
                            } else if (waitingForCommand == "c") {
                                ClearWait(player);
                                GameLoop.CommandManager.CloseDoor(player, thisDir);
                            } else if (waitingForCommand == "g") {
                                ClearWait(player);
                                GameLoop.CommandManager.Pickup(player, thisDir);
                            } else if (waitingForCommand == "x") {
                                ClearWait(player);
                                GameLoop.CommandManager.Peek(player, thisDir);
                            }
                        }

                    }
                }
            } 
        }
    }
}
