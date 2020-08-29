using System;
using Microsoft.Xna.Framework;
using TearsInRain.Entities;
using System.Text;
using GoRogue.DiceNotation;
using TearsInRain.Tiles; 

namespace TearsInRain.Commands {
    public class CommandManager {
        public Point lastPeek = new Point(0, 0);


        public CommandManager() { }


        public bool MoveActorBy(Actor actor, Point position) {
            if (actor.MoveBy(position)) {
                actor.TimeLastActed = GameLoop.GameTime;

                string msg = "move_p" + "|" + GameLoop.NetworkingManager.myUID + "|" + actor.Position.X + "|" + actor.Position.Y;
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
                return true;
            }

            return false;
        }


        public void Peek(Actor actor, Point dir) {
            Point newPoint = actor.Position + dir;

            if (!GameLoop.World.CurrentMap.GetTileAt<TileBase>(newPoint.X, newPoint.Y).IsBlockingLOS) {
                lastPeek = dir;
                actor.PositionOffset += dir;
            }
        }

        public void ResetPeek(Actor actor) {
            actor.PositionOffset -= lastPeek;
            lastPeek = new Point(0, 0);
        }


        public void Attack(Actor attacker, Actor defender, int preAttack = -1, int preDodge = -1, int preDamage = -1, bool received = false) {
            if (attacker != null && defender != null && attacker != defender) {
                int attackChance = Dice.Roll("3d6");
                int dodgeChance = Dice.Roll("3d6");
                int damage = Dice.Roll(attacker.GetMeleeDamage("swing"));

                if (preAttack != -1) {
                    attackChance = preAttack;
                }

                if (preDodge != -1) {
                    dodgeChance = preDodge;
                }

                if (preDamage != -1) {
                    damage = preDamage;
                }

                if (!received) {
                    string msg = "dmg|" + defender.Position.X + "|" + defender.Position.Y + "|" + attacker.Position.X + "|" + attacker.Position.Y + "|" + attackChance + "|" + dodgeChance + "|" + damage;
                    GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
                }

                //GameLoop.UIManager.SaveMonster(defender);

                if (attackChance != 17 && attackChance != 18) { // Check to make sure attacker didn't critically miss
                    if (attackChance == 2 || attackChance == 3) { // Check to see if attacker critically hit
                        GameLoop.UIManager.MessageLog.Add(Utils.FirstCharToUpper(attacker.Name) + " scored a critical hit on " + defender.Name + " for " + damage + " damage!", Color.LimeGreen); // Critical hit is a guaranteed hit and 1.5x damage
                        ResolveDamage(defender, damage * 1.5);
                    } else { // Otherwise it's a normal attack
                        if (dodgeChance > defender.Dodge) { // Check if defender failed to dodge
                            GameLoop.UIManager.MessageLog.Add(Utils.FirstCharToUpper(attacker.Name) + " scored a hit on " + defender.Name + " for " + damage + " damage!", Color.CornflowerBlue);
                            ResolveDamage(defender, damage);
                        } else { // Otherwise they successfully dodged the attack, negating all damage
                            GameLoop.UIManager.MessageLog.Add(Utils.FirstCharToUpper(attacker.Name) + " attacked " + defender.Name + ", but the attack was dodged!", Color.Red);
                        }
                    }
                } else { // Attacker critically missed, which means the defender didn't have to try and dodge.
                    GameLoop.UIManager.MessageLog.Add(Utils.FirstCharToUpper(attacker.Name) + " critically missed while attacking " + defender.Name + "!", Color.DarkRed);
                }
            }
        }


        private static void ResolveDamage(Actor defender, double damage) {
            int dmg = (int) Math.Ceiling(damage);
            if (damage > 0) {
                defender.Health -= dmg;
                
                if (defender.Health <= 0) {
                    ResolveDeath(defender);
                }
            }
        }
        
        private static void ResolveDeath(Actor defender) {
            StringBuilder deathMessage = new StringBuilder($"{Utils.FirstCharToUpper(defender.Name)} died.");

            if (defender.Inventory.Count > 0) { 
                foreach (Item item in defender.Inventory) {
                    item.Font = GameLoop.UIManager.hold;
                    item.Position = defender.Position;
                    
                    GameLoop.World.CurrentMap.Add(item);
                } 
                defender.Inventory.Clear();
            }

            for (int i = 0; i < 14; i++) {
                if (defender.Equipped[i] != null) {
                    defender.Unequip(i);
                    defender.DropItem(0, 0);
                }
            }


            if (defender is Player) {
                defender.Position = new Point(0, 0);
                defender.Health = defender.MaxHealth;
            } else {
                GameLoop.World.CurrentMap.Remove(defender); 
            }

            GameLoop.UIManager.MessageLog.Add(deathMessage.ToString());
        }

        public void Pickup(Actor actor, Point pos) {
            Point newPoint = actor.Position + pos;
            Item item = GameLoop.World.CurrentMap.GetEntityAt<Item>(newPoint);

            if (item != null) {
                actor.PickupItem(item);
            } else {
                GameLoop.UIManager.MessageLog.Add("Nothing to pick up there!");
            }
        }


        public void OpenDoor(Actor actor, TileDoor door, Point pos) {
            if (!door.IsLocked) {
                door.Open();

                GameLoop.UIManager.MapConsole.IsDirty = true;
                GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes("t_data|door|" + pos.X + "|" + pos.Y + "|open|unlock"));
            } else {
                GameLoop.UIManager.MessageLog.Add("The door is locked.");
            }
        }

        public void CloseDoor(Actor actor, Point pos, bool literalPos = false) {
            Point newPoint;
            if (!literalPos)
                newPoint = actor.Position + pos;
            else
                newPoint = pos;

            TileBase tile = GameLoop.World.CurrentMap.GetTileAt<TileBase>(newPoint.X, newPoint.Y);
            Entity entity = GameLoop.World.CurrentMap.GetEntityAt<Entity>(newPoint);

            if (entity == null) {
                if (tile is TileDoor door) {
                    if (door.IsOpen) {
                        door.Close();

                        GameLoop.UIManager.MapConsole.IsDirty = true;

                        var data = "t_data|door|" + newPoint.X + "|" + newPoint.Y + "|close|";

                        if (door.IsLocked) { data += "lock"; } else if (!door.IsLocked) { data += "unlock"; }

                        GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(data));
                    } else {
                        GameLoop.UIManager.MessageLog.Add("The door is already closed.");
                    }
                } else {
                    GameLoop.UIManager.MessageLog.Add("There's nothing to close there!");
                }
            } else if (entity is Player) {
                GameLoop.UIManager.MessageLog.Add("You try to close the door, but some idiot is standing in the way!");
            } else if (entity is Monster) {
                GameLoop.UIManager.MessageLog.Add("Should have shut it before the monster walked through!");
            } else if (entity is Item) {
                if (entity.Name[0] == 'a' || entity.Name[0] == 'e' || entity.Name[0] == 'i' || entity.Name[0] == 'o' || entity.Name[0] == 'u')
                    GameLoop.UIManager.MessageLog.Add("An " + entity.Name + " is in the way!");
                else
                    GameLoop.UIManager.MessageLog.Add("A " + entity.Name + " is in the way!");
            }
        }
    }
}