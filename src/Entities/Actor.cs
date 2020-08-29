using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using RogueSharp.DiceNotation;
using TearsInRain.Serializers;
using TearsInRain.src;
using TearsInRain.Tiles;

namespace TearsInRain.Entities {
    
    [JsonConverter(typeof(ActorJsonConverter))]
    public class Actor : Entity { 
        public UInt64 TimeLastActed { get; set; }
        public Dictionary<string, SadConsole.CellDecorator> decorators = new Dictionary<string, SadConsole.CellDecorator>();
        
        // Primary Attributes

        public int Strength = 10;
        public int Dexterity = 10;
        public int Constitution = 10;
        public int Intelligence = 10;
        public int Wisdom = 10;
        public int Charisma = 10;

        public int Level = 1;
        public List<CharacterClass> Classes = new List<CharacterClass>();

        // Secondary Attributes (Direct)
        public int Health = 10; 
        public int MaxHealth = 10; 


        public int MaxStamina = 100;
        public int CurrentStamina = 100;

        public int MaxEnergy = 10;
        public int CurrentEnergy = 10;

        // Secondary Attributes (Indirect)
        public double Carrying_Weight = 0;
        public double Carrying_Volume = 0;
        public double MaxCarriedVolume = 0;

        public int BaseSpeed = 10;
        public int Speed = 10;

        public float EncumbranceLv = 0;
        public double BasicLift = (double)(10 * 10) / 2;
        public int HeldGold = 0;

        public int BaseDodge = 8;
        public int Dodge = 8;

        public bool IsStealthing = false;
        public bool FailedStealth = false; 
        public int StealthResult = 0;
        public int BaseStealthResult = 0;

        public string ClassSkills;
        public Dictionary<string, Skill> Skills = new Dictionary<string, Skill>();
        public int RanksPerLvl = 2;
        public int MiscRanksMod = 0;
        


        public List<Item> Inventory = new List<Item>(); 
        public Item[] Equipped = new Item[14];

        public Actor(Color foreground, Color background, int glyph, int width = 1, int height = 1) : base(foreground, background, width, height, glyph) {
            Animation.CurrentFrame[0].Foreground = foreground;
            Animation.CurrentFrame[0].Background = background;
            Animation.CurrentFrame[0].Glyph = glyph;

            foreach (KeyValuePair<string, Skill> skill in GameLoop.SkillLibrary) {
                Skills.Add(skill.Key, skill.Value);
            }
        }

        public bool MoveBy(Point positionChange) { 
            TileBase tile = GameLoop.World.CurrentMap.GetTileAt<TileBase>(Position.X + positionChange.X, Position.Y + positionChange.Y);


            if (tile != null) {
                Point justVert = new Point(0, positionChange.Y);
                Point justHori = new Point(positionChange.X, 0);

                if (GameLoop.World.CurrentMap.IsTileWalkable(Position + positionChange) || tile is TileDoor) {
                    Actor monster = GameLoop.World.CurrentMap.GetEntityAt<Actor>(Position + positionChange);
                    Item item = GameLoop.World.CurrentMap.GetEntityAt<Item>(Position + positionChange);

                    foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                        if (player.Value.Position == Position + positionChange) {
                            monster = player.Value;
                        }
                    }

                    if (monster != null) {
                        GameLoop.CommandManager.Attack(this, monster);
                        return false;
                    } 
                    
                    //else if (tile.Name.ToLower().Contains("door") && !tile.IsOpen) {
                    //    GameLoop.CommandManager.OpenDoor(this, door, Position + positionChange);
                    //    return true;
                    //}


                    Position += positionChange;
                    return true;
                } else if (GameLoop.World.CurrentMap.IsTileWalkable(Position + justVert) || tile is TileDoor) {
                    Actor monster = GameLoop.World.CurrentMap.GetEntityAt<Actor>(Position + justVert);
                    Item item = GameLoop.World.CurrentMap.GetEntityAt<Item>(Position + justVert);

                    foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                        if (player.Value.Position == Position + justVert) {
                            monster = player.Value;
                        }
                    }

                    if (monster != null) {
                        GameLoop.CommandManager.Attack(this, monster);
                        return true;
                    } else if (tile is TileDoor door && !door.IsOpen) {
                        GameLoop.CommandManager.OpenDoor(this, door, Position + justVert);
                        return true;
                    }


                    Position += justVert;
                    return true;
                } else if (GameLoop.World.CurrentMap.IsTileWalkable(Position + justHori) || tile is TileDoor) {
                    Actor monster = GameLoop.World.CurrentMap.GetEntityAt<Actor>(Position + justHori);
                    Item item = GameLoop.World.CurrentMap.GetEntityAt<Item>(Position + justHori);

                    foreach (KeyValuePair<long, Player> player in GameLoop.World.players) {
                        if (player.Value.Position == Position + justHori) {
                            monster = player.Value;
                        }
                    }

                    if (monster != null) {
                        GameLoop.CommandManager.Attack(this, monster);
                        return true;
                    } else if (tile is TileDoor door && !door.IsOpen) {
                        GameLoop.CommandManager.OpenDoor(this, door, Position + justHori);
                        return true;
                    }


                    Position += justHori;
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }

        public bool MoveTo(Point newPosition) {
            Position = newPosition;
            return true;
        }


        public void Stealth(int stealthResult, bool IsLocal) {
            IsStealthing = true;

            if (stealthResult > Dexterity) {
                FailedStealth = true;
            }

            if (IsLocal) {
                Animation.CurrentFrame[0].Foreground = Color.DarkGray;
                Animation.IsDirty = true;
            } else {
                UpdateStealth(0);
            }

            StealthResult = stealthResult;
            Speed += 5;
        }

        public void Unstealth() {
            if (!IsStealthing) { return; }
            IsStealthing = false;
            FailedStealth = false;
            StealthResult = 0;

            Animation.CurrentFrame[0].Foreground.A = 255;
            Animation.CurrentFrame[0].Foreground = Color.Yellow;
            Animation.IsDirty = true;

            Speed -= 5;
        }


        public void UpdateStealth(int mod) {
            if (!IsStealthing) { return; }
            
            if (!FailedStealth) {
                if (Dexterity >= (StealthResult - mod)) {
                    int successBy = (Dexterity - (StealthResult - mod));
                    int newA = 255 - (successBy * 10);
                    if (newA < 75) { newA = 75; }
                    if (newA > 255) { newA = 255; }
                    Animation.CurrentFrame[0].Foreground.A = (byte)newA;
                }
            }

            Animation.IsDirty = true;
        }


        public string GetMeleeDamage(string type) {
            double diceFormula;
            int dice;
            int add;

            if (type == "swing") {
                diceFormula = ((Strength - 10) + 4) / 4;
            } else {
                diceFormula = ((Strength - 10) + 4) / 8;
            }

            string[] splitDice = new string[2];
            splitDice = diceFormula.ToString().Split('.');

            dice = Convert.ToInt32(splitDice[0]);

            int tempAdd = 0;
            if (splitDice.Length == 2) {
                tempAdd = Convert.ToInt32(splitDice[1]);
            }

            if (tempAdd <= 0.25) {
                add = 1;
            } else if (0.25 < tempAdd && tempAdd <= 0.5) {
                add = 2;
            } else if (0.5 < tempAdd && tempAdd <= 0.75) {
                add = -1;
                dice++;
            } else {
                add = 0;
                dice++;
            }


            return dice + "d6+" + add;
        }


        public void UpdateRanksPerLvl() {
            RanksPerLvl = (int) Math.Floor((double) ((Intelligence - 10) / 2));
            if (Intelligence == 7) { RanksPerLvl = -2; }
            if (Intelligence == 8 || Intelligence == 9) { RanksPerLvl = -1; }

            for (int i = 0; i < Classes.Count; i++) {
                RanksPerLvl += Classes[i].RanksPerLv;
            }

            RanksPerLvl += MiscRanksMod;
        }


        public void RecalculateHealth() {
            MaxHealth = 0;
            for (int i = 0; i < Classes.Count; i++) {
                MaxHealth += Classes[i].HealthGranted;
            }
        }


        public int SpentSkillPoints() {
            int spent = 0;

            foreach (KeyValuePair<string, Skill> skill in Skills) {
                spent += skill.Value.Ranks;
            }

            return spent;
        }


        public void CalculateEncumbrance() {
            BasicLift = (double)(Strength * Strength) / 2;
            EncumbranceLv = (float) (Carrying_Weight / BasicLift);
            
            Speed = (int) Math.Floor((float) BaseSpeed * (1.0f + EncumbranceLv));

            if (IsStealthing) 
                Speed += 5;

            Dodge = 8;
        }

        public void RecalculateWeight() {
            double totalWeight = 0;

            for (int i = 0; i < Inventory.Count; i++) {
                totalWeight += Inventory[i].StackWeight();
            }

            Carrying_Weight = totalWeight;
            CalculateEncumbrance();
        }


        public void Unequip(int index) {
            Item item = Equipped[index];

            bool alreadyHaveItem = false;
            for (int i = 0; i < Inventory.Count; i++) {
                if (Equipped[index] != null && Inventory[i].Name == item.Name) {
                    alreadyHaveItem = true;
                    Inventory[i].Quantity += item.Quantity;
                    if (item.Quantity == 1) {
                        GameLoop.UIManager.MessageLog.Add($"{Name} unequipped the {item.Name}.");
                    } else {
                        GameLoop.UIManager.MessageLog.Add($"{Name} unequipped {item.Quantity} {item.NamePlural}.");
                    }

                    Equipped[index] = null;
                    break;
                }
            }

            if (!alreadyHaveItem && item != null) {
                if (Inventory.Count < 26) {
                    Inventory.Add(item);
                    if (item.Quantity == 1) {
                        GameLoop.UIManager.MessageLog.Add($"Unequipped the {item.Name}.");
                    } else {
                        GameLoop.UIManager.MessageLog.Add($"Unequipped {item.Quantity} {item.NamePlural}.");
                    }

                    Equipped[index] = null;
                } else {
                    GameLoop.UIManager.MessageLog.Add("Your inventory is full!");
                }
            }

            RecalculateWeight();

            if (this is Player) {
                string msg = "p_update|" + GameLoop.NetworkingManager.myUID + "|" + Position.X + "|" + Position.Y + "|" + JsonConvert.SerializeObject(this, Formatting.Indented, new ActorJsonConverter());
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
            }

            GameLoop.UIManager.UpdateInventory();
        }

        public void Equip(int index) {
            if (index < Inventory.Count && Inventory.Count < 26) {
                if (Inventory[index].Slot != -1) {
                    Item toEquip = Inventory[index].Clone();
                    
                    Inventory[index].Quantity--;

                    Unequip(toEquip.Slot);

                    Equipped[toEquip.Slot] = toEquip;

                    if (Inventory[index].Quantity <= 0) {
                        Inventory.RemoveAt(index);
                    }

                    GameLoop.UIManager.UpdateInventory();
                    GameLoop.UIManager.UpdateEquipment();
                    GameLoop.UIManager.MessageLog.Add($"Equipped the {toEquip.Name}.");
                }
            } else {
                GameLoop.UIManager.MessageLog.Add($"Inventory too full to do that!");
            }

            if (this is Player) {
                string msg = "p_update|" + GameLoop.NetworkingManager.myUID + "|" + Position.X + "|" + Position.Y + "|" + JsonConvert.SerializeObject(this, Formatting.Indented, new ActorJsonConverter());
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
            }
        }



        public void PickupItem(Item item) {
            bool alreadyHaveItem = false;
            for (int i = 0; i < Inventory.Count; i++) {
                if (Inventory[i].Name == item.Name) {
                    alreadyHaveItem = true;
                    Inventory[i].Quantity += item.Quantity;
                    if (item.Quantity == 1) {
                        GameLoop.UIManager.MessageLog.Add($"{Name} picked up the {item.Name}.");
                    } else {
                        GameLoop.UIManager.MessageLog.Add($"{Name} picked up {item.Quantity} {item.NamePlural}."); 
                    }


                    string pickupMsg = "i_data|pickup|" + item.Position.X + "|" + item.Position.Y + "|" + JsonConvert.SerializeObject(item, Formatting.Indented, new ItemJsonConverter()) + "|" + item.Quantity;
                    GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(pickupMsg));
                    item.Destroy();
                    break;
                }
            }

            if (!alreadyHaveItem) {
                if (Inventory.Count < 26) {
                    Inventory.Add(item);
                    if (item.Quantity == 1) {
                        GameLoop.UIManager.MessageLog.Add($"{Name} picked up the {item.Name}.");
                    } else {
                        GameLoop.UIManager.MessageLog.Add($"{Name} picked up {item.Quantity} {item.NamePlural}.");
                    }

                    string pickupMsg = "i_data|pickup|" + item.Position.X + "|" + item.Position.Y + "|" + JsonConvert.SerializeObject(item, Formatting.Indented, new ItemJsonConverter()) + "|" + item.Quantity;
                    GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(pickupMsg));
                    item.Destroy();
                } else {
                    GameLoop.UIManager.MessageLog.Add("Your inventory is full!");
                }
            } 

            RecalculateWeight();
            GameLoop.UIManager.UpdateInventory();

            if (this is Player) {
                string msg = "p_update|" + GameLoop.NetworkingManager.myUID + "|" + Position.X + "|" + Position.Y + "|" + JsonConvert.SerializeObject(this, Formatting.Indented, new ActorJsonConverter());
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
            }
        }


        public void DropItem(int index, int num) {
            List<Item> itemsAtPos = GameLoop.World.CurrentMap.GetEntitiesAt<Item>(Position);

            if (num == 0 || num >= Inventory[index].Quantity) {
                Item dropped = Inventory[index].Clone();
                dropped.Font = GameLoop.UIManager.hold;
                dropped.Position = Position;

                bool foundSame = false;

                for (int i = 0; i < itemsAtPos.Count; i++) {
                    if (itemsAtPos[i].Name == Inventory[index].Name) {
                        itemsAtPos[i].Quantity += dropped.Quantity;
                        foundSame = true;
                        break;
                    }
                }

                if (!foundSame) {
                    GameLoop.World.CurrentMap.Add(dropped);

                }

                Inventory.RemoveAt(index);

                RecalculateWeight();
                GameLoop.UIManager.UpdateInventory();

                string secondMsg = "i_data|drop|" + Position.X + "|" + Position.Y + "|" + JsonConvert.SerializeObject(dropped, Formatting.Indented, new ItemJsonConverter());
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(secondMsg));
            } else {
                Item dropped = Inventory[index].Clone();
                dropped.Font = GameLoop.UIManager.hold;
                dropped.Position = Position;
                dropped.Quantity = num;

                Inventory[index].Quantity -= num;

                bool foundSame = false;

                for (int i = 0; i < itemsAtPos.Count; i++) {
                    if (itemsAtPos[i].Name == Inventory[index].Name) {
                        itemsAtPos[i].Quantity += dropped.Quantity;
                        foundSame = true;
                        break;
                    }
                }

                if (!foundSame) {
                    GameLoop.World.CurrentMap.Add(dropped);
                }

                if (dropped.Quantity == 1) {
                    GameLoop.UIManager.MessageLog.Add($"{Name} dropped the {dropped.Name}.");
                } else {
                    GameLoop.UIManager.MessageLog.Add($"{Name} dropped {dropped.Quantity} {dropped.NamePlural}.");
                }

                RecalculateWeight();
                GameLoop.UIManager.UpdateInventory();

                string secondMsg = "i_data|drop|" + Position.X + "|" + Position.Y + "|" + JsonConvert.SerializeObject(dropped, Formatting.Indented, new ItemJsonConverter());
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(secondMsg));
            }


            if (this is Player) {
                string msg = "p_update|" + GameLoop.NetworkingManager.myUID + "|" + Position.X + "|" + Position.Y + "|" + JsonConvert.SerializeObject(this, Formatting.Indented, new ActorJsonConverter());
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
            }


        }
    }
}