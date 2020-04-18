using System;
using System.Collections.Generic;
using System.Linq;
using Emux.GameBoy.Cpu;
using Emux.GameBoy.Graphics;
using Emux.GameBoy.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Color = Microsoft.Xna.Framework.Color;

namespace Emux.MonoGame
{
    public class EmuxHost : Game, IClock, IVideoOutput
    {
        public new event EventHandler Tick;

		public const int FrameScaler = 3;
		private const bool FitVideo = false;
        private readonly Settings _settings;
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _video;
        private bool _clockEnabled = false;
        private SpriteFont _font;
        
        private TimeSpan _last;
        private readonly IList<double> _fps = new List<double>();
        private double _averageFps, _minFps, _maxFps;
        private readonly IList<double> _gbfps = new List<double>();
        private double _averageGbFps, _minGbFps, _maxGbFps;
        
        private double _speedFactor;

		byte[] framePixels = new byte[GameBoyGpu.FrameWidth * GameBoyGpu.FrameHeight * sizeof(int)];

		public EmuxHost(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
            
            _video = new Texture2D(GraphicsDevice, GameBoyGpu.FrameWidth, GameBoyGpu.FrameHeight);
            _font = Content.Load<SpriteFont>("Calibri");

			_graphics.PreferredBackBufferWidth = GameBoyGpu.FrameWidth * FrameScaler;
			_graphics.PreferredBackBufferHeight = GameBoyGpu.FrameHeight * FrameScaler;

			_graphics.SynchronizeWithVerticalRetrace = GameBoy.EnableFrameLimit;
			_graphics.ApplyChanges();
			IsFixedTimeStep = GameBoy.EnableFrameLimit;

			GameBoy.Run();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_clockEnabled)
                Tick?.Invoke(this, EventArgs.Empty);

            var gamePadState = GamePad.GetState(PlayerIndex.One);
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            HandleInput(gamePadState, keyboardState);

            base.Update(gameTime);
        }

        private void HandleInput(GamePadState gamePadState, KeyboardState keyboardState)
        {
            foreach (var binding in _settings.ControllerBindings)
            {
                if (gamePadState.IsButtonDown(binding.Key))
                    GameBoy.KeyPad.PressedButtons |= binding.Value;
                else
                    GameBoy.KeyPad.PressedButtons &= ~binding.Value;
            }

            foreach (var binding in _settings.KeyboardBindings)
            {
                if (keyboardState.IsKeyDown(binding.Key))
                    GameBoy.KeyPad.PressedButtons |= binding.Value;
                else
                    GameBoy.KeyPad.PressedButtons &= ~binding.Value;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
			GraphicsDevice.Clear(Color.Black);

			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawFrame();
            
            #if DEBUG
            DrawDebugInformation(gameTime);
            #endif
            
            _spriteBatch.End();
            
            base.Draw(gameTime);
        }

        private void DrawFrame()
        {

            int screenHeight = _graphics.PreferredBackBufferHeight;
            int screenWidth = _graphics.PreferredBackBufferWidth;

			if (FitVideo)
			{
				float aspectRatio = (float)GameBoyGpu.FrameWidth / (float)GameBoyGpu.FrameHeight;
				int frameWidth;
				int frameHeight;
				if (screenHeight > screenWidth)
				{
					frameWidth = screenWidth;
					frameHeight = (int)(frameWidth / aspectRatio);
				}
				else
				{
					frameHeight = screenHeight;
					frameWidth = (int)(frameHeight / aspectRatio);
				}


				_spriteBatch.Draw(
					_video,
					new Rectangle((screenWidth - frameWidth) / 2, (screenHeight - frameHeight) / 2, frameWidth, frameHeight),
					Color.White
				);
			}
			else
			{
				_spriteBatch.Draw(
					_video,
					new Rectangle((screenWidth - screenWidth) / 2, (screenHeight - screenHeight) / 2, screenWidth, screenHeight),
					Color.White
				);
			}
        }

        private void DrawDebugInformation(GameTime time)
        {
            _fps.Add(1 / time.ElapsedGameTime.TotalSeconds);
            _gbfps.Add(GameBoy.FramesPerSecond);
            
            var difference = time.TotalGameTime - _last;
            if (difference.TotalSeconds > 1)
            {
                _minGbFps = _gbfps.Min();
                _averageGbFps = _gbfps.Average();
                _maxGbFps = _gbfps.Max();

                _minFps = _fps.Min();
                _averageFps = _fps.Average();
                _maxFps = _fps.Max();
                
                _speedFactor = _averageGbFps / Emux.GameBoy.GameBoy.OfficialFrameRate;
                
                _last = time.TotalGameTime;
                _gbfps.Clear();
                _fps.Clear();
            }

            string info = $"GameBoy FPS: {_averageGbFps:0.00} (Min: {_minGbFps:0.00}, Max: {_maxGbFps:0.00})\n" +
                          $"Clock FPS: {_averageFps:0.00} (Min: {_minFps:0.00}, Max: {_maxFps:0.00})\n" +
                          $"Speed Factor: {_speedFactor:0.00}\n" +
                          $"GBC Mode: {(GameBoy.Cpu.DoubleSpeed ? "Double speed" : "Normal speed")}\n" +
                          $"LY: {GameBoy.Gpu.LY}\n" +
                          $"LYC: {GameBoy.Gpu.LYC}\n";
                        
            _spriteBatch.DrawString(_font, info, Vector2.Zero, Color.Cyan);
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
            for (int i = 0, j = 0; j < pixelData.Length; i++)
            {
                framePixels[i++] = pixelData[j++];
                framePixels[i++] = pixelData[j++];
                framePixels[i++] = pixelData[j++];
            }
            
            _video.SetData(framePixels);
        }
    }
}