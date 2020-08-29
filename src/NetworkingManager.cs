using Discord;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SadConsole;
using System;
using System.Collections.Generic;
using TearsInRain.Entities;
using TearsInRain.Serializers;
using TearsInRain.Tiles;
using TearsInRain.UI;

namespace TearsInRain {
    class NetworkingManager {
        public Discord.Discord discord;
        private static long applicationID = 579827348665532425;

        public long myUID = 0;
        public bool updateUID = true;
        public long hostUID = 0;
        public long lobbyID = 0;
        public string myUsername = "";
        public string MD5map = "";

        public UserManager userManager;

        public void InitNetworking(long lobbyId) {
            // First, connect to the lobby network layer
            var lobbyManager = discord.GetLobbyManager();
            lobbyManager.ConnectNetwork(lobbyId);

            lobbyID = lobbyId;

            // Next, deterministically open our channels
            // Reliable on 0, unreliable on 1
            lobbyManager.OpenNetworkChannel(lobbyId, 0, true); // Game Logic
            lobbyManager.OpenNetworkChannel(lobbyId, 1, false); // Chat Logic
            lobbyManager.OpenNetworkChannel(lobbyId, 2, true); // World Logic

            lobbyManager.OnNetworkMessage += processMessage;


            myUID = userManager.GetCurrentUser().Id;

            hostUID = lobbyManager.GetLobby(lobbyID).OwnerId;  
            // We're ready to go!
        }

        private void processMessage(long lobbyId, long userId, byte channelId, byte[] data) {
            // DO SOME NETWORK PROCESSING BULLSHIT HERE
            if (data == System.Text.Encoding.UTF8.GetBytes("a")) {
                return;
            }

            if (channelId == 0) {
                var msg = System.Text.Encoding.UTF8.GetString(data);
                var splitMsg = msg.Split('|');

                if (splitMsg[0] == "move_p") { // Player got moved
                    if (GameLoop.World.players[Convert.ToInt64(splitMsg[1])] != null) {
                        GameLoop.World.players[Convert.ToInt64(splitMsg[1])].Position = new Point(Convert.ToInt32(splitMsg[2]), Convert.ToInt32(splitMsg[3]));
                    }
                }

                if (splitMsg[0] == "p_list") { // Translates a list of players being sent
                    for (int i = 1; i < splitMsg.Length; i++) {
                        string[] playerData = splitMsg[i].Split(';');
                        long uid = Convert.ToInt64(playerData[0]);
                        int x = Convert.ToInt32(playerData[1]);
                        int y = Convert.ToInt32(playerData[2]);
                        int a = Convert.ToInt32(playerData[3]);

                        GameLoop.World.CreatePlayer(uid, new Player(Color.Yellow, Color.Transparent));

                        Player player = GameLoop.World.players[uid];
                        player.Position = new Point(x, y);
                        player.Animation.CurrentFrame[0].Foreground.A = (byte) a;

                        if (a != 255) {
                            player.IsStealthing = true;
                        } else {
                            player.IsStealthing = false;
                        }

                        player.Animation.IsDirty = true;
                    }
                }

                if (splitMsg[0] == "p_update") {
                    long uid = Convert.ToInt64(splitMsg[1]);
                    int x = Convert.ToInt32(splitMsg[2]);
                    int y = Convert.ToInt32(splitMsg[3]);

                    Actor newP = JsonConvert.DeserializeObject<Actor>(splitMsg[4], new ActorJsonConverter()); 

                    if (GameLoop.World.players.ContainsKey(uid)) {
                         GameLoop.World.players[uid] = new Player(newP, new Point(x, y));
                        GameLoop.UIManager.SyncMapEntities(true);
                    } else {
                        GameLoop.World.players.Add(uid, new Player(newP, new Point(0, 0)));
                    }
                }

                if (splitMsg[0] == "m_list") {
                    //  GameLoop.World.CurrentMap.Entities = new GoRogue.MultiSpatialMap<Entity>();
                    GameLoop.ReceivedEntities = new GoRogue.MultiSpatialMap<Entity>();
                    GameLoop.ReceivedEntities.Clear();
                    for (int i = 1; i < splitMsg.Length; i++) {
                        string[] smallerMsg = splitMsg[i].Split('~');
                        


                        Entity entity = JsonConvert.DeserializeObject<Actor>(smallerMsg[0], new ActorJsonConverter());
                        entity.Position = new Point(Convert.ToInt32(smallerMsg[1]), Convert.ToInt32(smallerMsg[2]));
                        GameLoop.ReceivedEntities.Add(entity, entity.Position);

                        entity.IsVisible = false;
                        entity.IsDirty = true;
                    }


                    GameLoop.World.CurrentMap.Entities = GameLoop.ReceivedEntities;

                    GameLoop.UIManager.SyncMapEntities(true);
                }


                if (splitMsg[0] == "time") {
                    int Year = Convert.ToInt32(splitMsg[1]);
                    int Season = Convert.ToInt32(splitMsg[2]);
                    int Day = Convert.ToInt32(splitMsg[3]);
                    int Hour = Convert.ToInt32(splitMsg[4]);
                    int Minute = Convert.ToInt32(splitMsg[5]);

                    GameLoop.TimeManager = new TimeManager(Day, Season, Year, Hour, Minute);
                }


                if (splitMsg[0] == "stealth") {
                    long stealthUID = Convert.ToInt64(splitMsg[2]);
                    int stealthResult = Convert.ToInt32(splitMsg[3]);

                    if (splitMsg[1] == "yes") {
                        if (GameLoop.World.players.ContainsKey(stealthUID)) {
                            GameLoop.World.PlayerStealth(stealthUID, stealthResult, true);
                        }
                    } else {
                        if (GameLoop.World.players.ContainsKey(stealthUID)) {
                            GameLoop.World.PlayerStealth(stealthUID, stealthResult, false);
                        }
                    }
                }


                if (splitMsg[0] == "t_data") { // Used for complex tile data
                    if (splitMsg[1] == "door") {
                        if (GameLoop.World.CurrentMap.GetTileAt<TileBase>(Convert.ToInt32(splitMsg[2]), Convert.ToInt32(splitMsg[3])) is TileDoor door) {
                            if (splitMsg[4] == "open") {
                                door.Open();
                            } else {
                                door.Close();
                            }

                            if(splitMsg[5] == "lock") {
                                door.ToggleLock(true, true);
                            } else {
                                door.ToggleLock(true, false);
                            }
                        }
                    } else if (splitMsg[1] == "farmland") {
                        Point pos = new Point(Convert.ToInt32(splitMsg[2]), Convert.ToInt32(splitMsg[3]));
                        GameLoop.World.CurrentMap.SetTile(pos, GameLoop.TileLibrary["farmland"].Clone());
                    } else if (splitMsg[1] == "flower_picked") {
                        Point pos = new Point (Convert.ToInt32(splitMsg[2]), Convert.ToInt32(splitMsg[3]));
                        GameLoop.World.CurrentMap.SetTile(pos, GameLoop.TileLibrary["grass"].Clone());
                    }

                    GameLoop.UIManager.SyncMapEntities(true);
                }

                if (splitMsg[0] == "i_data") {
                    if (splitMsg[1] == "list") { // Format: i_data|list|{itemjson}~posX~posY|{itemjson}~posX~posY|...
                        for (int i = 2; i < splitMsg.Length; i++) {
                            string[] smallerMsg = splitMsg[i].Split('~');

                            

                            Item item = JsonConvert.DeserializeObject<Item>(smallerMsg[0], new ItemJsonConverter());
                            item.Position = new Point(Convert.ToInt32(smallerMsg[1]), Convert.ToInt32(smallerMsg[2]));
                            GameLoop.World.CurrentMap.Add(item);

                            item.IsVisible = false;
                            item.IsDirty = true;
                        }


                        GameLoop.UIManager.SyncMapEntities(true);
                    }

                    if (splitMsg[1] == "drop") {
                        int x = Convert.ToInt32(splitMsg[2]);
                        int y = Convert.ToInt32(splitMsg[3]);
                        Item item = JsonConvert.DeserializeObject<Item>(splitMsg[4], new ItemJsonConverter());
                        item.Position = new Point(x, y);
                        GameLoop.World.CurrentMap.Add(item);

                        item.IsVisible = false;
                        item.IsDirty = true;
                    }

                    if (splitMsg[1] == "update") {
                        int x = Convert.ToInt32(splitMsg[2]);
                        int y = Convert.ToInt32(splitMsg[3]);
                        Item item = JsonConvert.DeserializeObject<Item>(splitMsg[4], new ItemJsonConverter());
                        item.Position = new Point(x, y);

                        Item existing = GameLoop.World.CurrentMap.GetEntityAt<Item>(new Point(x, y));
                        if (existing != null) {
                            GameLoop.World.CurrentMap.Remove(existing);
                        }

                        GameLoop.World.CurrentMap.Add(item);

                        item.IsVisible = false;
                        item.Animation.IsDirty = true;

                        GameLoop.UIManager.SyncMapEntities(true);
                    }

                    if (splitMsg[1] == "pickup") {
                        int x = Convert.ToInt32(splitMsg[2]);
                        int y = Convert.ToInt32(splitMsg[3]);
                        Item item = JsonConvert.DeserializeObject<Item>(splitMsg[4], new ItemJsonConverter());
                        int quantityGrabbed = Convert.ToInt32(splitMsg[5]);

                        List<Item> items = GameLoop.World.CurrentMap.GetEntitiesAt<Item>(new Point(x, y));

                        for (int i = 0; i < items.Count; i++) {
                            if (items[i].Name == item.Name) {
                                items[i].Quantity -= quantityGrabbed;

                                if (items[i].Quantity <= 0) {
                                    GameLoop.World.CurrentMap.Entities.Remove(items[i]);
                                }

                                break;
                            }
                        }

                    }
                }


                if (splitMsg[0] == "dmg") {
                    Point def = new Point(Convert.ToInt32(splitMsg[1]), Convert.ToInt32(splitMsg[2])); 
                    Point atk = new Point(Convert.ToInt32(splitMsg[3]),  Convert.ToInt32(splitMsg[4]));
                    
                    int attackChance = Convert.ToInt32(splitMsg[5]);
                    int dodgeChance = Convert.ToInt32(splitMsg[6]);
                    int damage = Convert.ToInt32(splitMsg[7]);

                    Actor defender = GameLoop.World.CurrentMap.GetEntityAt<Actor>(def);
                    Actor attacker = GameLoop.World.CurrentMap.GetEntityAt<Actor>(atk);

                    foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                        if (player.Value.Position == def && defender == null) { defender = player.Value; }
                        if (player.Value.Position == atk && attacker == null) { attacker = player.Value; }
                    } 

                    GameLoop.CommandManager.Attack(attacker, defender, attackChance, dodgeChance, damage, true); 
                }
            }

            if (channelId == 2) { // World Data Processing
                var encoded = System.Text.Encoding.UTF8.GetString(data);

                if (encoded != "a") {
                    GameLoop.World = JsonConvert.DeserializeObject<World>(encoded, new WorldJsonConverter());
                    GameLoop.UIManager.LoadMap(GameLoop.World.CurrentMap);
                    GameLoop.UIManager.MapConsole.Font = GameLoop.RegularSize;
                    GameLoop.UIManager.CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID]);
                }
            }
        }

        public void SendNetMessage(byte channel, byte[] packet, long ignoredID=0) {
            var lobbyManager = discord.GetLobbyManager();

            try { 
                foreach (Discord.User user in lobbyManager.GetMemberUsers(lobbyID)) {
                    lobbyManager.SendNetworkMessage(lobbyID, user.Id, channel, packet);
                }
            } catch (Discord.ResultException e) { 
            }

        }

        public NetworkingManager() {
            discord = new Discord.Discord(applicationID, (UInt64)Discord.CreateFlags.Default);
            userManager = discord.GetUserManager();
            discord.RunCallbacks();

            if (userManager != null) {

                try {
                    myUID = userManager.GetCurrentUser().Id;
                } catch (Discord.ResultException e) { 
                    myUID = 0;
                }

            }
        }

        public void changeClientTarget(string cID) { // Only used for testing multiple clients on a single computer
            System.Environment.SetEnvironmentVariable("DISCORD_INSTANCE_ID", cID);
            discord = new Discord.Discord(applicationID, (UInt64)Discord.CreateFlags.Default);

            userManager = discord.GetUserManager();
            userManager.OnCurrentUserUpdate += currentUserUpdate;
            discord.RunCallbacks();
        }

        private void currentUserUpdate() {
            myUID = userManager.GetCurrentUser().Id;
        }

        public void Update() {
            discord.RunCallbacks();
            
            try {
                if (myUID != userManager.GetCurrentUser().Id) {
                    myUID = userManager.GetCurrentUser().Id;

                    GameLoop.World.CreatePlayer(myUID, new Player(Color.Yellow, Color.Transparent));
                    GameLoop.World.players.Remove(0);
                    updateUID = false;
                }
            } catch (Discord.ResultException e) {
                System.Console.WriteLine(e);
            }
        }
    }
}
