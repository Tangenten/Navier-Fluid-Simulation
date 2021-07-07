using System;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;

namespace Navier {
	public struct Fluid {
		public const int N = 128;
		public const int iter = 2;
		public const int SCALE = 8;

		private int size;
		private readonly float dt;
		private readonly float diff;
		private readonly float visc;

		private float[] s;
		private float[] density;

		private float[] Vx;
		private float[] Vy;

		private float[] Vx0;
		private float[] Vy0;

		private readonly VertexArray vertexArrayQuads;
		private readonly VertexArray vertexArrayLines;
		private readonly RenderTexture renderTexture;
		private RenderStates renderStates;
		private readonly Shader blurShader;
		private Shader thresholdShader;
		private readonly Sprite sprite;
		private RenderTexture bloomRenderTexture;
		private Sprite bloomSprite;

		public Fluid(float dt, float diffusion, float viscosity) {
			this.size = N;
			this.dt = dt;
			this.diff = diffusion;
			this.visc = viscosity;

			this.s = new float[N * N];
			this.density = new float[N * N];

			this.Vx = new float[N * N];
			this.Vy = new float[N * N];

			this.Vx0 = new float[N * N];
			this.Vy0 = new float[N * N];

			this.vertexArrayQuads = new VertexArray(PrimitiveType.Quads, N * N * 4);
			this.vertexArrayLines = new VertexArray(PrimitiveType.Lines);

			this.renderTexture = new RenderTexture((uint) N * SCALE, (uint) N * SCALE);

			this.blurShader = new Shader(null, null, "C:\\Users\\Tangent\\Dropbox\\Programming\\RiderProjects\\Navier\\blur.frag");
			this.thresholdShader = new Shader(null, null, "C:\\Users\\Tangent\\Dropbox\\Programming\\RiderProjects\\Navier\\threshold.frag");
			this.renderStates = new RenderStates(BlendMode.None);

			this.sprite = new Sprite(this.renderTexture.Texture);
			this.sprite.Position = new Vector2f(0, 0);
			this.sprite.TextureRect = new IntRect(new Vector2i(0, 0), (Vector2i) this.renderTexture.Size);
			bloomRenderTexture = new RenderTexture((uint) N * SCALE, (uint) N * SCALE);
			bloomSprite = new Sprite(this.renderTexture.Texture);
			this.bloomSprite.Position = new Vector2f(0, 0);
			this.bloomSprite.TextureRect = new IntRect(new Vector2i(0, 0), (Vector2i) this.bloomRenderTexture.Size);
		}

		public int IX(int x, int y) {
			x = Math.Clamp(x, 0, N - 1);
			y = Math.Clamp(y, 0, N - 1);
			return x + y * N;
		}

		public void step() {
			this.diffuse(1, ref this.Vx0, ref this.Vx, this.visc, this.dt);
			this.diffuse(2, ref this.Vy0, ref this.Vy, this.visc, this.dt);

			this.project(ref this.Vx0, ref this.Vy0, ref this.Vx, ref this.Vy);

			this.advect(1, ref this.Vx, this.Vx0, this.Vx0, this.Vy0, this.dt);
			this.advect(2, ref this.Vy, this.Vy0, this.Vx0, this.Vy0, this.dt);

			this.project(ref this.Vx, ref this.Vy, ref this.Vx0, ref this.Vy0);

			this.diffuse(0, ref this.s, ref this.density, this.diff, this.dt);
			this.advect(0, ref this.density, this.s, this.Vx, this.Vy, this.dt);
		}

		public void addDensity(int x, int y, float amount) {
			int index = this.IX(x, y);
			this.density[index] += amount;
		}

		public void addVelocity(int x, int y, float amountX, float amountY) {
			int index = this.IX(x, y);
			this.Vx[index] += amountX;
			this.Vy[index] += amountY;
		}

		public void renderD(ref RenderWindow window) {
			this.renderTexture.Clear(Color.Transparent);
			this.bloomRenderTexture.Clear(Color.Transparent);
			this.renderStates.Shader = null;
			this.renderStates.Texture = null;

			uint indexer = 0;
			for (int i = 0; i < N; i++) {
				for (int j = 0; j < N; j++) {
					float x = i * SCALE;
					float y = j * SCALE;
					float d = this.density[this.IX(i, j)];

					Color color;

					if (d < 1f) {
						color = Color.Black;
					}else if (d < 196) {
						color = TweenFuncts.ColorLerp(d, 0, 196, Color.Black, Color.Blue);
					} else {
						color = TweenFuncts.ColorLerp(d, 196, 255, Color.Blue, Color.Red);
					}

					this.vertexArrayQuads[indexer++] = new Vertex(new Vector2f(x, y), color);
					this.vertexArrayQuads[indexer++] = new Vertex(new Vector2f(x + SCALE, y), color);
					this.vertexArrayQuads[indexer++] = new Vertex(new Vector2f(x + SCALE, y + SCALE), color);
					this.vertexArrayQuads[indexer++] = new Vertex(new Vector2f(x, y + SCALE), color);
				}
			}

			this.renderTexture.Draw(this.vertexArrayQuads, this.renderStates);
			this.renderTexture.Display();
			this.sprite.Texture = this.renderTexture.Texture;


			this.blurShader.SetUniform("blur_radius", new Vec2(0, 0.00075f));
			this.blurShader.SetUniform("texture", this.sprite.Texture);
			this.renderStates.Shader = this.blurShader;
			this.renderStates.Texture = this.sprite.Texture;

			this.sprite.Draw(this.renderTexture, this.renderStates);
			this.renderTexture.Display();
			this.sprite.Texture = this.renderTexture.Texture;

			this.blurShader.SetUniform("blur_radius", new Vec2(0.00075f, 0));
			this.blurShader.SetUniform("texture", this.sprite.Texture);
			this.renderStates.Shader = this.blurShader;
			this.renderStates.Texture = this.sprite.Texture;

			this.sprite.Draw(this.renderTexture, this.renderStates);
			this.renderTexture.Display();
			this.sprite.Texture = this.renderTexture.Texture;

			this.bloomSprite.Texture = this.sprite.Texture;

			this.thresholdShader.SetUniform("threshold", 0.11f);
			this.thresholdShader.SetUniform("texture", this.bloomSprite.Texture);
			this.renderStates.Shader = this.thresholdShader;
			this.renderStates.Texture = this.bloomSprite.Texture;

			this.bloomSprite.Draw(this.bloomRenderTexture, this.renderStates);
			this.bloomRenderTexture.Display();
			this.bloomSprite.Texture = this.bloomRenderTexture.Texture;

			this.blurShader.SetUniform("blur_radius", new Vec2(0, 0.0025f));
			this.blurShader.SetUniform("texture", this.bloomSprite.Texture);
			this.renderStates.Shader = this.blurShader;
			this.renderStates.Texture = this.bloomSprite.Texture;

			this.bloomSprite.Draw(this.bloomRenderTexture, this.renderStates);
			this.bloomRenderTexture.Display();
			this.bloomSprite.Texture = this.bloomRenderTexture.Texture;

			this.blurShader.SetUniform("blur_radius", new Vec2(0.0025f, 0));
			this.blurShader.SetUniform("texture", this.bloomSprite.Texture);
			this.renderStates.Shader = this.blurShader;
			this.renderStates.Texture = this.bloomSprite.Texture;

			this.bloomSprite.Draw(this.bloomRenderTexture, this.renderStates);
			this.bloomRenderTexture.Display();
			this.bloomSprite.Texture = this.bloomRenderTexture.Texture;

			//window.Draw(this.sprite, new RenderStates(BlendMode.Add));
			window.Draw(this.bloomSprite, new RenderStates(BlendMode.Add));
		}

		public void renderV(ref RenderWindow window) {
			this.vertexArrayLines.Clear();

			for (int i = 0; i < N; i++) {
				for (int j = 0; j < N; j++) {
					float x = i * SCALE;
					float y = j * SCALE;
					float vx = Math.Clamp(this.Vx[this.IX(i, j)] * 1000, -8, 8);
					float vy = Math.Clamp(this.Vy[this.IX(i, j)] * 1000, -8, 8);

					if ((Math.Abs(vx) > 0.001 && Math.Abs(vy) > 0.001)) {
						Color color = new Color(255, 255, 255, 64);
						this.vertexArrayLines.Append(new Vertex(new Vector2f(x, y), color));
						this.vertexArrayLines.Append(new Vertex(new Vector2f(x + vx * SCALE, y + vy * SCALE), color));
					}
				}
			}

			RenderStates renderStates = new RenderStates(BlendMode.Alpha);
			window.Draw(this.vertexArrayLines, renderStates);
		}

		public void fadeD() {
			for (int i = 0; i < this.density.Length; i++) {
				this.density[i] = Math.Clamp(this.density[i] - 0.05f, 0, 255);
				//this.density[i] *= 0.98f;
			}
		}

		public void fadeV() {
			for (int i = 0; i < this.Vx.Length; i++) {
				this.Vx[i] *= 0.99f;
				this.Vy[i] *= 0.99f;
			}
		}

		private void diffuse(int b, ref float[] x, ref float[] x0, float diff, float dt) {
			float a = dt * diff * (N - 2) * (N - 2);
			this.lin_solve(b, ref x, x0, a, 1 + 6 * a);
		}

		private void lin_solve(int b, ref float[] x, float[] x0, float a, float c) {
			float cRecip = 1.0f / c;
			for (int k = 0; k < iter; k++) {
				for (int j = 1; j < N - 1; j++) {
					for (int i = 1; i < N - 1; i++) {
						x[this.IX(i, j)] =
							(x0[this.IX(i, j)]
							 + a * (x[this.IX(i + 1, j)]
							        + x[this.IX(i - 1, j)]
							        + x[this.IX(i, j + 1)]
							        + x[this.IX(i, j - 1)]
							 )) * cRecip;
					}
				}
				this.set_bnd(b, ref x);
			}
		}

		private void project(ref float[] velocX, ref float[] velocY, ref float[] p, ref float[] div) {
			for (int j = 1; j < N - 1; j++) {
				for (int i = 1; i < N - 1; i++) {
					div[this.IX(i, j)] = -0.5f * (
						                     velocX[this.IX(i + 1, j)]
						                     - velocX[this.IX(i - 1, j)]
						                     + velocY[this.IX(i, j + 1)]
						                     - velocY[this.IX(i, j - 1)]
					                     ) / N;
					p[this.IX(i, j)] = 0;
				}
			}

			this.set_bnd(0, ref div);
			this.set_bnd(0, ref p);
			this.lin_solve(0, ref p, div, 1, 6);

			for (int j = 1; j < N - 1; j++) {
				for (int i = 1; i < N - 1; i++) {
					velocX[this.IX(i, j)] -= 0.5f * (p[this.IX(i + 1, j)]
					                                 - p[this.IX(i - 1, j)]) * N;
					velocY[this.IX(i, j)] -= 0.5f * (p[this.IX(i, j + 1)]
					                                 - p[this.IX(i, j - 1)]) * N;
				}
			}

			this.set_bnd(1, ref velocX);
			this.set_bnd(2, ref velocY);
		}

		private void advect(int b, ref float[] d, float[] d0, float[] velocX, float[] velocY, float dt) {
			float i0, i1, j0, j1;

			float dtx = dt * (N - 2);
			float dty = dt * (N - 2);

			float s0, s1, t0, t1;
			float tmp1, tmp2, x, y;

			float Nfloat = N;
			float ifloat, jfloat;
			int i, j;

			for (j = 1, jfloat = 1; j < N - 1; j++, jfloat++) {
				for (i = 1, ifloat = 1; i < N - 1; i++, ifloat++) {
					tmp1 = dtx * velocX[this.IX(i, j)];
					tmp2 = dty * velocY[this.IX(i, j)];
					x = ifloat - tmp1;
					y = jfloat - tmp2;

					if (x < 0.5f) x = 0.5f;
					if (x > Nfloat + 0.5f) x = Nfloat + 0.5f;
					i0 = (float) Math.Floor(x);
					i1 = i0 + 1.0f;
					if (y < 0.5f) y = 0.5f;
					if (y > Nfloat + 0.5f) y = Nfloat + 0.5f;
					j0 = (float) Math.Floor(y);
					j1 = j0 + 1.0f;

					s1 = x - i0;
					s0 = 1.0f - s1;
					t1 = y - j0;
					t0 = 1.0f - t1;

					int i0i = (int) i0;
					int i1i = (int) i1;
					int j0i = (int) j0;
					int j1i = (int) j1;

					// DOUBLE CHECK THIS!!!
					d[this.IX(i, j)] =
						s0 * (t0 * d0[this.IX(i0i, j0i)] + t1 * d0[this.IX(i0i, j1i)]) +
						s1 * (t0 * d0[this.IX(i1i, j0i)] + t1 * d0[this.IX(i1i, j1i)]);
				}
			}

			this.set_bnd(b, ref d);
		}

		private void set_bnd(int b, ref float[] x) {
			for (int i = 1; i < N - 1; i++) {
				x[this.IX(i, 0)] = b == 2 ? -x[this.IX(i, 1)] : x[this.IX(i, 1)];
				x[this.IX(i, N - 1)] = b == 2 ? -x[this.IX(i, N - 2)] : x[this.IX(i, N - 2)];
			}

			for (int j = 1; j < N - 1; j++) {
				x[this.IX(0, j)] = b == 1 ? -x[this.IX(1, j)] : x[this.IX(1, j)];
				x[this.IX(N - 1, j)] = b == 1 ? -x[this.IX(N - 2, j)] : x[this.IX(N - 2, j)];
			}

			x[this.IX(0, 0)] = 0.5f * (x[this.IX(1, 0)] + x[this.IX(0, 1)]);
			x[this.IX(0, N - 1)] = 0.5f * (x[this.IX(1, N - 1)] + x[this.IX(0, N - 2)]);
			x[this.IX(N - 1, 0)] = 0.5f * (x[this.IX(N - 2, 0)] + x[this.IX(N - 1, 1)]);
			x[this.IX(N - 1, N - 1)] = 0.5f * (x[this.IX(N - 2, N - 1)] + x[this.IX(N - 1, N - 2)]);
		}
	}
}
