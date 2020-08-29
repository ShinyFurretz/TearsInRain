using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace TearsInRain.Entities {

    [JsonObject(MemberSerialization.OptOut)]
    public class Player : Actor {
        public Player(Color foreground, Color background) : base(new Color(237, 186, 161), background, 1) {
            Name = "Player";
            TimeLastActed = 0;
            Dictionary<string, string> props = new Dictionary<string, string>();

            tilesheetName = "player";
            props.Add("qualities", "tilling");

            Font = SadConsole.Global.Fonts["player"].GetFont(SadConsole.Font.FontSizes.Quarter);

            decorators = new Dictionary<string, SadConsole.CellDecorator>();
            decorators.Add("eyeIris", new SadConsole.CellDecorator(Color.CornflowerBlue, 2, Microsoft.Xna.Framework.Graphics.SpriteEffects.None));
            decorators.Add("eyeWhite", new SadConsole.CellDecorator(Color.White, 3, Microsoft.Xna.Framework.Graphics.SpriteEffects.None));
            decorators.Add("hair", new SadConsole.CellDecorator(Color.PaleGoldenrod, 4, Microsoft.Xna.Framework.Graphics.SpriteEffects.None));

            Animation.AddDecorator(0, 1, decorators["eyeWhite"], decorators["eyeIris"], decorators["hair"]);

            Animation.IsDirty = true;

            Item hoe = GameLoop.ItemLibrary["hoe_shoddy"].Clone();
            Item book = GameLoop.ItemLibrary["book_incredible_tales"].Clone();
            Item seed = GameLoop.ItemLibrary["seed_potato"].Clone();

            Inventory.Add(book);
            Inventory.Add(hoe);
            Inventory.Add(seed);
        }

        public Player(Actor actor, Point pos) : base(actor.Animation.CurrentFrame[0].Foreground, actor.Animation.CurrentFrame[0].Background, 1) {
            Name = actor.Name;

            Strength = actor.Strength;
            Dexterity = actor.Dexterity;
            Constitution = actor.Constitution;
            Intelligence = actor.Intelligence;
            Wisdom = actor.Wisdom;
            Charisma = actor.Charisma;

            Level = actor.Level;

            Health = actor.Health;
            MaxHealth = actor.MaxHealth;


            MaxStamina = actor.MaxStamina;
            CurrentStamina = actor.CurrentStamina;

            MaxEnergy = actor.MaxEnergy;
            CurrentEnergy = actor.CurrentEnergy;
            Carrying_Weight = actor.Carrying_Weight;
            Carrying_Volume = actor.Carrying_Volume;
            MaxCarriedVolume = actor.MaxCarriedVolume;

            BaseSpeed = actor.BaseSpeed;
            Speed = actor.Speed;

            EncumbranceLv = actor.EncumbranceLv;
            BasicLift = actor.BasicLift;
            HeldGold = actor.HeldGold;

            BaseDodge = actor.BaseDodge;
            Dodge = actor.Dodge;

            IsStealthing = actor.IsStealthing;
            FailedStealth = actor.FailedStealth;
            StealthResult = actor.StealthResult;
            BaseStealthResult = actor.BaseStealthResult;

            Inventory = actor.Inventory;

            Skills = actor.Skills;
            ClassSkills = actor.ClassSkills;
            RanksPerLvl = actor.RanksPerLvl;
            MiscRanksMod = actor.MiscRanksMod;

            Equipped = actor.Equipped;

            Position = pos;

            tilesheetName = actor.tilesheetName;
            UpdateFontSize(GameLoop.UIManager.hold.SizeMultiple);
            decorators = actor.decorators;
            RefreshDecorators();
        }
        

        public void RefreshDecorators() {
            Animation.ClearDecorators(0, 1);
            Animation.AddDecorator(0, 1, decorators["eyeWhite"], decorators["eyeIris"], decorators["hair"]);

            //for (int i = 0; i < Equipped.Length; i++) {
                
            //}


            Animation.IsDirty = true;

        }
    }
}