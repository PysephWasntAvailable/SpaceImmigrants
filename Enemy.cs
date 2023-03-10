using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SpaceImmigrants
{
    public class Enemy
    {
        public Vector2 SpriteSize = new(20, 20);
        public float MovementSpeed;
        public Vector2 Velocity;
        public Vector2 Position;
        public string Type;
        public int Lives;
        public event EventHandler PositionChanged;
        public event EventHandler Died;
        public HurtBox HurtBox;

        private Texture2D _enemySprite;
        private Game1 _currentGame;

        public struct EnemyData
        {
            public EnemyData(string type, Vector2 position, Vector2 velocity, Vector2 size)
            {
                velocity.Normalize();
                Type = type;
                Position = position;
                Velocity = velocity;
                Size = size;
            }

            public string Type { get; }
            public Vector2 Position { get; }
            public Vector2 Velocity { get; }
            public Vector2 Size { get; }
        }

        // Add a list of speeds for each enemy type
        private static Dictionary<string, float> _enemySpeeds = new()
        {
            { "Normal", 150 },
            { "Fly", 250 },
            { "Tank", 75 },
        };
        private static Dictionary<string, int> _enemyLives = new()
        {
            { "Normal", 2 },
            { "Fly", 1 },
            { "Tank", 3 },
        };
        private static Dictionary<string, int> _enemyPoints = new()
        {
            { "Normal", 3 },
            { "Fly", 5 },
            { "Tank", 10 },
        };

        public Enemy(Game1 currentGame, EnemyData Data)
        {;
            this._enemySprite = currentGame.EnemySprites[Data.Type];
            this.Position = Data.Position;
            this.Velocity = Data.Velocity;
            this.MovementSpeed = _enemySpeeds[Data.Type];
            this.SpriteSize = Data.Size;
            this._currentGame = currentGame;
            this.Lives = _enemyLives[Data.Type];
            this.Type = Data.Type;
            this.HurtBox = new HurtBox(this);

            if (Data.Type == "Tank"){
                new TankProjectileShooter(currentGame, this);
            }

            // Add a callback to the DrawQueue to draw the player, using gameTime argument to animate the sprite.
			currentGame.DrawQueue.Add(this, (double step) => {
                this.UpdatePosition(step);
                Vector2 enemyPosition = this.Position;

                Rectangle destination = new Rectangle(
                    (int)enemyPosition.X,
                    (int)enemyPosition.Y,
                    (int)Data.Size.X,
                    (int)Data.Size.Y
                );

                Game1.SpriteBatch.Draw(
                    texture: this._enemySprite,
                    destinationRectangle: destination,
                    sourceRectangle: null,
                    effects: SpriteEffects.None,
                    layerDepth: 0.1f,
                    color: Color.White,
                    // pi/2 is 90 degrees in radians, but idk the helper function to convert rad to deg
                    rotation: (float)Math.Atan2(this.Velocity.Y, this.Velocity.X) - MathHelper.PiOver2,
                    origin: new Vector2(this.SpriteSize.X / 2, this.SpriteSize.Y / 2)
                );
			});

            currentGame.HurtBoxes.Add(this.HurtBox);
            currentGame.Enemies.Add(this);
        }

        public virtual void Destroy()
        {
            // wait for GamePreStepped to run once before removing the enemy
            // This is to prevent race conditions with removing objects from the list while also iterating over it
            Died?.Invoke(this, EventArgs.Empty);

            void diedConnection(double step)
			{
                this._currentGame.DrawQueue.Remove(this);
                this._currentGame.Enemies.Remove(this);
                this._currentGame.HurtBoxes.RemoveAll((hurtBox) => hurtBox.Parent == this);
                Event.GamePreStepped.Invoked -= diedConnection;
            }

            Event.InvokedEvent<double>.InvokedDelegate preSteppedConnection = diedConnection;
            Event.GamePreStepped.Invoked += preSteppedConnection;
        }

        public void EnemyHit(Game1 currentGame)
        {
            this.Lives -= 1;
            if (this.Lives <= 0)
            {
                currentGame.Points += _enemyPoints[this.Type];
                Destroy();
            }
        }

        public virtual void UpdatePosition(double Step)
        {
            this.Position += this.Velocity * this.MovementSpeed * (float)Step;

            if (this.Position.X < -this.SpriteSize.X || this.Position.X > Game1.ViewportSize.X)
            {
                Destroy();
                return;
            }
            if (this.Position.Y > Game1.ViewportSize.Y)
            {
                Destroy();
                return;
            }

            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
