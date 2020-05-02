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

        private readonly Settings _settings;
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _video;
        private bool _clockEnabled = false;
        private SpriteFont _font;

		private int _fpsIndex;
		private const float _averageTimeSeconds = 5;
		private int _numDeltaSamples;
		private double[] _deltas;
        
        private double _speedFactor;

		private Texture2D _white1x1;
		private readonly byte[] _framePixels = new byte[GameBoyGpu.FrameWidth * GameBoyGpu.FrameHeight * sizeof(int)];

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
			_white1x1 = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
			var dataColors = new Color[1] { Color.White };
			_white1x1.SetData(dataColors);
            _font = Content.Load<SpriteFont>("Calibri");

			_graphics.PreferredBackBufferWidth = GameBoyGpu.FrameWidth * Settings.FrameScaler;
			_graphics.PreferredBackBufferHeight = GameBoyGpu.FrameHeight * Settings.FrameScaler;

			_graphics.SynchronizeWithVerticalRetrace = GameBoy.EnableFrameLimit;
			_graphics.ApplyChanges();
			IsFixedTimeStep = GameBoy.EnableFrameLimit;

			_numDeltaSamples = (int)((_averageTimeSeconds * 1000f) / (1000f / Emux.GameBoy.GameBoy.OfficialFrameRate));
			_deltas = Enumerable.Repeat(1000d / Emux.GameBoy.GameBoy.OfficialFrameRate, _numDeltaSamples).ToArray();

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

			if (Settings.FitVideo)
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
			var deltaMs = GameBoy.FrameDelta.TotalMilliseconds;
			if (deltaMs == 0)
				return;


			_fpsIndex = (_fpsIndex + 1) % _deltas.Length;
			_deltas[_fpsIndex] = deltaMs;

			var rawAverage = _deltas.Average();
			var average = 1000f / rawAverage;
			var min = 1000f / _deltas.Min();
			var max = 1000f / _deltas.Max();
            
            _speedFactor = average / Emux.GameBoy.GameBoy.OfficialFrameRate;
                

            string info = $"GameBoy FPS: {average:0.00} (Min: {min:0.00}, Max: {max:0.00})\n" +
                          $"Speed Factor: {_speedFactor:0.00}\n" +
                          $"GBC Mode: {(GameBoy.Cpu.DoubleSpeed ? "Double speed" : "Normal speed")}\n" +
                          $"LY: {GameBoy.Gpu.LY}\n" +
                          $"LYC: {GameBoy.Gpu.LYC}\n";
            _spriteBatch.DrawString(_font, info, Vector2.Zero, Color.Cyan);

			drawGraph(rawAverage);
		}

		private void drawGraph(double average)
		{
			// Chart
			const double max = 100; // ms
			float chartTop = _graphics.PreferredBackBufferHeight - (_graphics.PreferredBackBufferHeight * 0.1f);
			float chartBottom = _graphics.PreferredBackBufferHeight ;
			float sampleWidth = (float)_graphics.PreferredBackBufferWidth / _numDeltaSamples;

			var y = (float)map(_deltas[0], 0, max, chartBottom, chartTop);
			Vector2 endPoint = new Vector2(0, y);
			for (var i = 1; i < _deltas.Length; i++)
			{
				var delta = _deltas[i];
				y = (float)map(delta, 0, max, chartBottom, chartTop);

				var startPoint = new Vector2(i * sampleWidth, y);
				DrawLine(
					startPoint,
					endPoint,
					Color.White
				);

				endPoint = startPoint;
			}

			// Current Frame indicator
			DrawLine(
				new Vector2(_fpsIndex * sampleWidth, chartTop),
				new Vector2(_fpsIndex * sampleWidth, chartBottom),
				Color.Red
			);
			// Average
			DrawLine(
				new Vector2(0, (float)map(average, 0, max, chartBottom, chartTop)),
				new Vector2(_graphics.PreferredBackBufferWidth, (float)map(average, 0, max, chartBottom, chartTop)),
				Color.Green
			);

		}
		double map(double x, double in_min, double in_max, double out_min, double out_max) 
			=> ((x - in_min) * (out_max - out_min) / (in_max - in_min)) + out_min;

		void DrawLine(Vector2 start, Vector2 end, Color color)
		{
			Vector2 edge = end - start;
			// calculate angle to rotate line
			float angle =
				(float)Math.Atan2(edge.Y, edge.X);


			_spriteBatch.Draw(_white1x1,
				new Rectangle(
					(int)start.X,
					(int)start.Y,
					(int)edge.Length(),
					1
				),
				null,
				color,
				angle,     
				Vector2.Zero,
				SpriteEffects.None,
				0
			);

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
                _framePixels[i++] = pixelData[j++];
                _framePixels[i++] = pixelData[j++];
                _framePixels[i++] = pixelData[j++];
            }
            
            _video.SetData(_framePixels);
        }
    }
}