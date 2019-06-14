﻿using System; 
using Microsoft.Xna.Framework; 
using TearsInRain.Tiles;
using TearsInRain.Entities;
using System.Collections.Generic;
using System.Linq;
using GoRogue;
using TearsInRain.Serializers;
using Newtonsoft.Json;

namespace TearsInRain {

    [JsonConverter(typeof(WorldJsonConverter))]
    public class World {
        
        private int _mapWidth = 100;
        private int _mapHeight = 100;
        private int _maxRooms = 100;
        private int _minRoomSize = 4;
        private int _maxRoomSize = 15;
        public Map CurrentMap { get; set; }
        public string WorldName = "";

        public List<Point> SeenTiles = new List<Point>();

        public Dictionary<long, Player> players = new Dictionary<long, Player>();
        public GoRogue.FOV lastFov;


        public World(string Name) {
            WorldName = Name;

            CreateMap(); 
            
            CreatePlayer(GameLoop.NetworkingManager.myUID, new Player(Color.Yellow, Color.Transparent));

            CreateMonsters();

            CreateLoot();

            CurrentMap.PlaceTrees(200);
        }

        public World() {
        }


        private void CreateMap() {
            CurrentMap = new Map(_mapWidth, _mapHeight);
            MapGenerator mapGen = new MapGenerator();
            CurrentMap = mapGen.GenerateMap(_mapWidth, _mapHeight, _maxRooms, _minRoomSize, _maxRoomSize);
        }
        
        private void CreateMonsters() {
            int numMonsters = 10;
            for (int i = 0; i < numMonsters; i++) {
                int monsterPosition = GameLoop.Random.Next(0, CurrentMap.Width * CurrentMap.Height);
                Monster newMonster = new Monster(Color.Blue, Color.Transparent);

                while (CurrentMap.Tiles[monsterPosition].IsBlockingMove) {
                    monsterPosition = GameLoop.Random.Next(0, CurrentMap.Width * CurrentMap.Height);
                }
                
               
                newMonster.Name = "the common troll";
                
                newMonster.Position = new Point(monsterPosition % CurrentMap.Width, monsterPosition / CurrentMap.Width);
                CurrentMap.Add(newMonster);
            }
        }
        
        public void CreatePlayer(long playerUID, Player player, bool overwrite = false) {
            if (!players.ContainsKey(playerUID)) {
                Player newPlayer = player;


                if (player.Position == new Point(0, 0)) {
                    for (int i = 0; i < CurrentMap.Tiles.Length; i++) {
                        if (!CurrentMap.Tiles[i].IsBlockingMove) {
                            newPlayer.Position = SadConsole.Helpers.GetPointFromIndex(i, CurrentMap.Width);
                            break;
                        }
                    }
                }
                
                players.Add(playerUID, newPlayer);
            } else if (players.ContainsKey(playerUID) && overwrite) {
                players[playerUID] = player;
            }
        }
        
        private void CreateLoot() {
            int numLoot = 20;
            
            for (int i = 0; i < numLoot; i++) {
                int lootPosition = GameLoop.Random.Next(0, CurrentMap.Width * CurrentMap.Height);
                Item newLoot = GameLoop.ItemLibrary["potato"].Clone();
                
                while (CurrentMap.Tiles[lootPosition].IsBlockingMove) {
                    lootPosition = GameLoop.Random.Next(0, CurrentMap.Width * CurrentMap.Height);
                }
                
                newLoot.Position = new Point(lootPosition % CurrentMap.Width, lootPosition / CurrentMap.Width);
                
                CurrentMap.Add(newLoot);
            }

        }

        public void PlayerStealth(long UID, int StealthResult, bool ToStealth) {

            if (ToStealth) {
                GameLoop.World.players[UID].Stealth(StealthResult, false);
            } else {
                GameLoop.World.players[UID].Unstealth();
            }
        }


        
        public void ResetFOV(bool keepOld = false) {
            if (!keepOld) {
                for (int i = SeenTiles.Count - 1; i > 0; i--) {
                    var spot = SeenTiles[i];

                    TileBase tile = CurrentMap.GetTileAt<TileBase>(spot.X, spot.Y);
                    tile.Darken(true);

                    SeenTiles.Remove(spot);
                }
                 
            }
            lastFov = null;
            GameLoop.UIManager.RefreshMap();

            CalculateFov(new Point (0, 0));
        }


        public void CalculateFov(Point dir) {
            // Use a GoRogue class that creates a map view so that the IsTransparent function is called whenever FOV asks for the value of a position
            var fovMap = new GoRogue.MapViews.LambdaMapView<bool>(CurrentMap.Width, CurrentMap.Height, CurrentMap.IsTransparent);
            
            lastFov = new FOV(fovMap);

            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) { 

                Point start = GameLoop.World.players[GameLoop.NetworkingManager.myUID].Position + dir;

                Point playerRel = GameLoop.World.players[GameLoop.NetworkingManager.myUID].CalculatedPosition;
                playerRel += new Point(6, 6);


                Point mouseLoc = GameLoop.MouseLoc;
                double degrees = Math.Atan2((mouseLoc.Y - playerRel.Y), (mouseLoc.X - playerRel.X)) * (180.0 / Math.PI);
                degrees = (degrees > 0.0 ? degrees : (360.0 + degrees));
                lastFov.Calculate(start, 20, Radius.CIRCLE, degrees, 114);


                foreach (var spot in lastFov.NewlySeen) {
                    TileBase tile = CurrentMap.GetTileAt<TileBase>(spot.X, spot.Y);
                    tile.IsVisible = true;

                    if (tile is TileDoor door) {
                        door.UpdateGlyph();
                    }

                    if (!SeenTiles.Contains(spot)) {
                        SeenTiles.Add(new Point(spot.X, spot.Y));
                    }
                }

                foreach (KeyValuePair<long, Player> player in players) {
                    if (!lastFov.BooleanFOV[player.Value.Position.X, player.Value.Position.Y] && player.Key != GameLoop.NetworkingManager.myUID) { 
                        player.Value.IsVisible = false;
                    } else if (lastFov.BooleanFOV[player.Value.Position.X, player.Value.Position.Y] || player.Key == GameLoop.NetworkingManager.myUID) {
                        player.Value.IsVisible = true;


                        if (player.Key != GameLoop.NetworkingManager.myUID) {
                            Point myPos = players[GameLoop.NetworkingManager.myUID].Position;
                            Point theirPos = player.Value.Position;
                            int distance = (int) Distance.CHEBYSHEV.Calculate(myPos.X, myPos.Y, theirPos.X, theirPos.Y);

                            player.Value.UpdateStealth((distance / 2) - 5);
                        }

                    }
                }
                foreach (Entity entity in GameLoop.World.CurrentMap.Entities.Items) {
                    if (!(entity is Player)) {
                        if (!lastFov.BooleanFOV[entity.Position.X, entity.Position.Y]) {
                            entity.IsVisible = false;
                        } else if (lastFov.BooleanFOV[entity.Position.X, entity.Position.Y] && GameLoop.UIManager.MapConsole.ViewPort.Contains(entity.Position.X, entity.Position.Y)) {
                            entity.IsVisible = true;
                        }
                    }
                }


                for (int i = SeenTiles.Count - 1; i > 0; i--) {
                    var spot = SeenTiles[i];

                    if (!lastFov.CurrentFOV.Contains(new GoRogue.Coord(spot.X, spot.Y))) {
                        TileBase tile = CurrentMap.GetTileAt<TileBase>(spot.X, spot.Y);
                        tile.Darken(true);

                        SeenTiles.Remove(spot);
                    } else {
                        TileBase tile = CurrentMap.GetTileAt<TileBase>(spot.X, spot.Y);
                        tile.Darken(false);

                        GameLoop.UIManager.MapConsole.ClearDecorators(spot.X, spot.Y, 1);
                    }
                }
                

                GameLoop.UIManager.MapConsole.IsDirty = true;
            }
        }
    }
}
