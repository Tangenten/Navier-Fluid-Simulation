using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace Navier {
	public class Game {
		private int windowHeight;
		private int windowWidth;
		private RenderWindow window;

		private readonly Clock clock;
		private float deltaTime;

		private Vector2f currentMousePosition;
		private Vector2f previousMousePosition;

		private Fluid fluid;

		public Game() {
			this.windowWidth = Fluid.N * Fluid.SCALE;
			this.windowHeight = Fluid.N * Fluid.SCALE;

			this.fluid = new Fluid(6f, 0.00000000001f, 0.00000000001f);

			this.window = new RenderWindow(new VideoMode((uint) this.windowWidth, (uint) this.windowHeight), "Navier", Styles.Default);
			this.window.SetVerticalSyncEnabled(true);

			this.window.Closed += this.OnClose;
			this.window.Resized += this.OnResize;
			this.window.MouseMoved += this.OnMouseMoved;

			this.currentMousePosition = new Vector2f(0f, 0f);
			this.previousMousePosition = new Vector2f(0f, 0f);

			this.clock = new Clock();
			this.deltaTime = 0f;
		}

		private void OnResize(object sender, SizeEventArgs e) {
			this.windowWidth = (int) e.Width;
			this.windowHeight = (int) e.Height;
		}

		private void OnClose(object sender, EventArgs e) {
			this.window.Close();
		}

		private void OnMouseMoved(object sender, MouseMoveEventArgs e) {
			this.previousMousePosition = this.currentMousePosition;
			this.currentMousePosition.X = e.X;
			this.currentMousePosition.Y = e.Y;

			if (Mouse.IsButtonPressed(Mouse.Button.Left)) {
				Vector2f mousePos = (Vector2f) (Mouse.GetPosition(this.window) / Fluid.SCALE);
				float angle = (float) Math.Atan2(this.currentMousePosition.Y - this.previousMousePosition.Y, this.currentMousePosition.X - this.previousMousePosition.X);
				Vector2f v = this.AngleToVector(angle) * 0.01f;
				for (int i = -4; i <= 4; i++) {
					for (int j = -4; j <= 4; j++) {
						this.fluid.addDensity((int) (mousePos.X + i), (int) (mousePos.Y + j), 60);
						this.fluid.addVelocity((int) (mousePos.X + i), (int) (mousePos.Y + j), v.X, v.Y);
					}
				}
			}

			if (Mouse.IsButtonPressed(Mouse.Button.Right)) {
				Vector2f mousePos = (Vector2f) (Mouse.GetPosition(this.window) / Fluid.SCALE);
				float angle = (float) Math.Atan2(this.currentMousePosition.Y - this.previousMousePosition.Y, this.currentMousePosition.X - this.previousMousePosition.X);
				Vector2f v = this.AngleToVector(angle) * 0.01f;
				for (int i = -2; i <= 2; i++) {
					for (int j = -2; j <= 2; j++) {
						this.fluid.addVelocity((int) (mousePos.X + i), (int) (mousePos.Y + j), v.X, v.Y);
					}
				}
			}
		}

		public void Run() {
			while (this.window.IsOpen) {
				this.deltaTime = this.clock.Restart().AsSeconds();
				Console.WriteLine(1f / this.deltaTime);
				this.window.Clear(Color.Black);
				this.window.DispatchEvents();

				this.fluid.step();
				this.fluid.renderD(ref this.window);
				this.fluid.fadeD();
			//	this.fluid.fadeV();

			if (Keyboard.IsKeyPressed(Keyboard.Key.Space)) {
				this.fluid.renderV(ref this.window);
			}

				this.window.Display();
			}
		}

		public Vector2f AngleToVector(float radians) {
			return new Vector2f((float) Math.Cos(radians), (float) Math.Sin(radians));
		}
	}
}
