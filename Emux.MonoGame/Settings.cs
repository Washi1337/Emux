using System.Collections.Generic;
using System.Runtime.Serialization;
using Emux.GameBoy.Input;
using Microsoft.Xna.Framework.Input;

namespace Emux.MonoGame
{
    [DataContract]
    public class Settings
    {
        [DataMember]
        public Dictionary<Buttons, GameBoyPadButton> ControllerBindings
        {
            get;
        } = new Dictionary<Buttons, GameBoyPadButton>
        {
            [Buttons.A] = GameBoyPadButton.A,
            [Buttons.B] = GameBoyPadButton.B,
            [Buttons.DPadUp] = GameBoyPadButton.Up,
            [Buttons.DPadDown] = GameBoyPadButton.Down,
            [Buttons.DPadLeft] = GameBoyPadButton.Left,
            [Buttons.DPadRight] = GameBoyPadButton.Right,
            [Buttons.Start] = GameBoyPadButton.Start,
            [Buttons.Back] = GameBoyPadButton.Select,
        };

        [DataMember]
        public Dictionary<Keys, GameBoyPadButton> KeyboardBindings
        {
            get;
        } = new Dictionary<Keys, GameBoyPadButton>
        {
            [Keys.X] = GameBoyPadButton.A,
            [Keys.Z] = GameBoyPadButton.B,
            [Keys.Up] = GameBoyPadButton.Up,
            [Keys.Down] = GameBoyPadButton.Down,
            [Keys.Left] = GameBoyPadButton.Left,
            [Keys.Right] = GameBoyPadButton.Right,
            [Keys.Enter] = GameBoyPadButton.Start,
            [Keys.LeftShift] = GameBoyPadButton.Select,
        };

    }
}