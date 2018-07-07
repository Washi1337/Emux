using System;
using Emux.GameBoy.Cpu;
using Emux.GameBoy.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace Emux.MonoGame
{
    public class EmuxHost : Game, IClock, IVideoOutput
    {
        public new event EventHandler Tick;
        
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _video;
        private bool _clockEnabled = false;
        
        public EmuxHost()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public GameBoy.GameBoy GameBoy
        {
            get;
            set;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            _video = new Texture2D(GraphicsDevice, 160, 144);
            GameBoy.Cpu.Run();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_clockEnabled)
                Tick?.Invoke(this, EventArgs.Empty);
            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            float aspectRatio = 160f / 144f;

            int screenHeight = _graphics.PreferredBackBufferHeight;
            int screenWidth = _graphics.PreferredBackBufferWidth;
            
            int frameWidth;
            int frameHeight;
            if (screenHeight > screenWidth)
            {
                frameWidth = screenWidth;
                frameHeight = (int) (frameWidth / aspectRatio);
            }
            else
            {
                frameHeight = screenHeight;
                frameWidth = (int) (frameHeight / aspectRatio);
            }
            
            _spriteBatch.Draw(_video,
                new Rectangle((screenWidth - frameWidth) / 2, (screenHeight - frameHeight) / 2, frameWidth, frameHeight), 
                Color.White);
            
            _spriteBatch.End();
            
            base.Draw(gameTime);
        }

        public void Start()
        {
            _clockEnabled = true;
        }

        public void Stop()
        {
            _clockEnabled = false;
        }

        public void RenderFrame(byte[] pixelData)
        {
            var rawData = new byte[160 * 144 * sizeof(int)];
            
            for (int i = 0, j = 0; j < pixelData.Length; i+=4, j+=3)
            {
                rawData[i] = pixelData[j + 2];
                rawData[i + 1] = pixelData[j + 1];
                rawData[i + 2] = pixelData[j];
                rawData[i + 3] = 255;
            }
            
            _video.SetData(rawData);
        }
    }
}