using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Voronoi
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        BasicEffect effect;
        const int GridLines = 50;
        const float CellSize = 16;
        VertexPositionColor[] grid = new VertexPositionColor[GridLines * 2 * 2];
        Vector2 circleCenter;
        float circleRadius = 123;

        const int CircleSegments = 100;
        VertexPositionColor[] circle = new VertexPositionColor[CircleSegments * 2];

        VertexPositionColor[] highlights = new VertexPositionColor[5000];
        VertexPositionColorTexture[] square = new VertexPositionColorTexture[6];

        Texture2D tex;

        const int W = 256;
        const int H = W;
        const int N = 200;
        const int screenSize = W * 3;
        Vector2[] pts = new Vector2[N];
        Vector2[] ptVels = new Vector2[N];

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsMouseVisible = true;
            

            Random r = new Random();
            for (int i = 0; i < N; ++i)
            {
                pts[i] = new Vector2((float)r.NextDouble() * W, (float)r.NextDouble() * H);
            }
            
            graphics.PreferredBackBufferWidth = 512;
            graphics.PreferredBackBufferHeight = 512;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            effect = new BasicEffect(GraphicsDevice);
            tex = new Texture2D(GraphicsDevice, W, H, false, SurfaceFormat.Color);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            var st = Mouse.GetState();
            circleCenter.X = st.X / 2;
            circleCenter.Y = st.Y / 2;

            float dR = 0;

            var kbstate = Keyboard.GetState();



            if (kbstate.IsKeyDown(Keys.Up))
                dR = +90;
            else if (kbstate.IsKeyDown(Keys.Down))
                dR = -90;

            float dt = (float)gameTime.ElapsedGameTime.Ticks / (float)TimeSpan.TicksPerSecond;
            circleRadius += dR *dt ;
            if (circleRadius < 0)
                circleRadius = 0;

            Animate(dt);

            base.Update(gameTime);
        }

        private static Vector2 Repel( Vector2 pos, Vector2 ptj)
        {
            Vector2 diff = pos - ptj;
            float d2 = diff.LengthSquared();
            if (d2 < 2f)
                d2 =2f;
            if (Math.Abs(diff.X) < 1e-6f && Math.Abs(diff.Y) < 1e-6f)
                return Vector2.Zero;
            return 2e3f *Vector2.Normalize(diff) * ( 1.0f / d2);// -diff * 1e-3f;
        }

        private void Animate(float dt)
        {

            Parallel.For(0, N, i =>
            {
                Vector2 vel0 = ptVels[i];
                Vector2 vel = Vector2.Zero;// vel0;
                Vector2 pos = pts[i];

                for (int j = 0; j <= N; ++j)
                {
                    if(j == i)
                        continue;

                    Vector2 ptj;
                    float mul = 1.0f;
                    if (j == N)
                    {
                        MouseState st = Mouse.GetState();
                        if (st.LeftButton != ButtonState.Pressed)
                            continue;
                        ptj = circleCenter;
                        mul = 50.0f;
                    }
                    else
                        ptj = pts[j];

                    vel += Repel(pos, ptj)*mul;
                }

                /*float brownian = 10000.0f;
                vel.X += dt * ((float)r.NextDouble() - 0.5f) * brownian;
                vel.Y += dt * ((float)r.NextDouble() - 0.5f) * brownian;*/

                float outerRepel = 2.0f;

                vel += outerRepel*Repel(pos, new Vector2(0, pos.Y));
                vel += outerRepel * Repel(pos, new Vector2(W, pos.Y));
                vel += outerRepel * Repel(pos, new Vector2(pos.X, 0));
                vel += outerRepel * Repel(pos, new Vector2(pos.X, H));

                pos = pos + vel * dt;

                /*if ((pos - pts[i]).LengthSquared() > 30)
                {
                    Console.WriteLine("fuck");
                }*/

                /*float damp = 1.0f;// 0.9f;
                pos = pos+ vel  * dt;*/
                if (pos.X < 0)
                {
                    pos.X = 0;
                    vel.X =  Math.Abs(vel.X);
                    //vel *= damp;
                }
                if (pos.X > W)
                {
                    pos.X = W;
                    vel.X =  -Math.Abs(vel.X);
                    //vel *= damp;
                }
                if (pos.Y < 0)
                {
                    pos.Y = 0;
                    vel.Y = Math.Abs(vel.Y);
                    //vel *= damp;
                }
                if (pos.Y > H)
                {
                    pos.Y = H;
                    vel.Y =  -Math.Abs(vel.Y);
                    //vel *= damp;
                }
                pts[i] = pos;
                ptVels[i] = vel;
            });

        }

        
        private void DrawVoronoi()
        {
            effect.CurrentTechnique.Passes[0].Apply();
            effect.TextureEnabled = true;
            effect.Texture = tex;
            GraphicsDevice.BlendState = BlendState.Opaque;

            int j = 0;
            float x0 = 0, y0 = 0, x1 = screenSize, y1 = screenSize;
            float u0 = 0, v0 = 0, u1 = 1, v1 = 1;

            square[j++] = new VertexPositionColorTexture(new Vector3(x0,y0, 0), Color.White, new Vector2(u0,v0));
            square[j++] = new VertexPositionColorTexture(new Vector3(x1, y0, 0), Color.White, new Vector2(u1, v0));
            square[j++] = new VertexPositionColorTexture(new Vector3(x1, y1, 0), Color.White, new Vector2(u1, v1));

            square[j++] = new VertexPositionColorTexture(new Vector3(x0, y0, 0), Color.White, new Vector2(u0, v0));
            square[j++] = new VertexPositionColorTexture(new Vector3(x1, y1, 0), Color.White, new Vector2(u1, v1));
            square[j++] = new VertexPositionColorTexture(new Vector3(x0, y1, 0), Color.White, new Vector2(u0, v1));
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, square, 0, 2);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            effect.World = effect.View = Matrix.Identity;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, screenSize, screenSize, 0, -1.0f, 1.0f);
            
            RenderToTexture();

            DrawVoronoi();

            base.Draw(gameTime);
        }

        private void RenderToTexture()
        {
            GraphicsDevice.Textures[0] = null;

            float[] data = new float[W * H * 3];
            
            const float max = Single.MaxValue;

            Parallel.For(0, H, y =>
            {
                int j = y * 3 * W;

                for (int x = 0; x < W; ++x)
                {
                    float a = max,
                            b = max,
                            c = max;
                    float t0, t1;
                    Vector2 xy = new Vector2(x, y);

                    for (int i = 0; i < N; ++i)
                    {
                        float distSq;
                        Vector2.DistanceSquared(ref xy, ref pts[i], out distSq);

                        if (distSq < c)
                        {
                            if (distSq < b)
                            {
                                if (distSq < a)
                                {
                                    // Xab
                                    t0 = a; t1 = b;
                                    a = distSq; b = t0; c = t1;
                                }
                                else
                                {
                                    // aXb
                                    t0 = a; t1 = b;
                                    a = t0; b = distSq; c = t1;
                                }
                            }
                            else
                            {
                                //abX
                                t0 = a; t1 = b;
                                a = t0; b = t1; c = distSq;
                            }
                        }
                    }

                    data[j++] = a;
                    data[j++] = b;
                    data[j++] = c;
                }
            });

            Color[] color = new Color[W * H];

            Parallel.For(0, W*H, z =>
            {
                float r = (float)Math.Sqrt(data[z * 3 + 1]) - 0.5f * (float)Math.Sqrt(data[z * 3]) + 0.2f* (float)Math.Sqrt(data[z * 3 + 1]); 
                int ri = 255 - (int)(r * 4);
                if (ri < 0)
                    ri = 0;
                int gi = 255 - (int)(r * 8);
                if (gi < 0)
                    gi = 0;
                int bi = 255 - (int)(r * 16);
                if (bi < 0)
                    bi = 0;
                color[z] = new Color(bi, gi, ri);
            });

            tex.SetData(color);
        }
    }
}
