using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;


namespace Level1;

public class IntVector2
{
    public int X;
    public int Y;
    public IntVector2(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}

public class Game1 : Game
{
    #region INSTANCE_VARS
    // Globals
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private static Point GameBounds = new Point(1280, 720); //window resolution
    private Texture2D texture;
    private const int cushion = 15;


    // Keyboard sate
    private static readonly Keys[] keysToWatch = { Keys.W, Keys.A, Keys.S, Keys.D, Keys.Left, Keys.Right, Keys.Up, Keys.Down };
    private KeyboardState ks = new KeyboardState(KeysToWatch);
    public static Keys[] KeysToWatch => keysToWatch;

    // Can modify background for testing purpose
    private Color backgroundColor;

    // Idle Spritesheet
    private Texture2D idleSheet;
    private Rectangle[] standingAnimation = new Rectangle[15]; // 15 frames per spritesheet
    // JumpFall Spritesheet 
    private Texture2D jumpFallSheet;
    private Rectangle[] jumpFallAnimation = new Rectangle[15];
    private int jumpFallAnimationIndex = 0;
    private bool jump = false;
    private int currentJumpFrame = 0;
    private int jumpFrames = 900; 
    // Attack SpriteSheet
    private Texture2D attackSheet;
    private Rectangle[] attackAnimation = new Rectangle[22];
    private int attackAnimationIndex = 0;
    private bool attack = false;
    private int currentAttackFrame = 0; 
    private int attackFrames = 800; 

    private float timer = 0f;
    private const float threshold = 40f;
    private int standingAnimationIndex = 1;
    private const int spriteHeight = 64; 
    private const int spriteWidth = 64;

    private bool fall = false; 
    // Player attributes
    private Vector2 playerPos = new Vector2(GameBounds.X / 2 , GameBounds.Y - spriteHeight + cushion);
    private const float baseSpeed = 5f;
    private Vector2 playerSpeed = new Vector2(baseSpeed, baseSpeed);
    private SpriteEffects flip = SpriteEffects.None;
    private float jumpSpeed = -8.0f;
    private float gravity = 0.4f;

    // still sprites
    private Texture2D mushroomTexture;
    private Rectangle mushroomRectangle = new Rectangle(0, 0, 32, 32);
    private Vector2 mushroomPos = new Vector2(500, 20); 
    private Texture2D cactusTexture;
    private Rectangle cactusRectangle = new Rectangle(0, 0, 32, 32);
    private Vector2 cactusPos = new Vector2(500, 100);
    private Texture2D brickTexture;
    private Rectangle brickRectangle = new Rectangle(0, 0, 32, 32);
    private Vector2 brickPos = new Vector2(500, 200);


    #endregion



    public Game1()
    {
        Content.RootDirectory = "Content";
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = GameBounds.X;
        _graphics.PreferredBackBufferHeight = GameBounds.Y;
        IsMouseVisible = true;
        backgroundColor = Color.CornflowerBlue;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        idleSheet = Content.Load<Texture2D>("noBKG_KnightIdle_strip");
        jumpFallSheet = Content.Load<Texture2D>("noBKG_KnightRoll_strip");// using shield for jump b/c i like that mechanic that he has a shield when he jumps
        attackSheet = Content.Load<Texture2D>("noBKG_KnightAttack_strip");

        mushroomTexture = Content.Load<Texture2D>("Mushroom");
        cactusTexture = Content.Load<Texture2D>("Cactus");
        brickTexture = Content.Load<Texture2D>("Brick");

        // Fill frames of animations
        for (int i = 0; i < standingAnimation.Length; i++)
        {
            standingAnimation[i] = new Rectangle(i * spriteWidth, 0, spriteWidth, spriteHeight);
        }
        for (int i = 0; i < jumpFallAnimation.Length; i++)
        {
            jumpFallAnimation[i] = new Rectangle(i * spriteWidth, 0, spriteWidth, spriteHeight); 
        }
        for (int i = 0; i < attackAnimation.Length; i++)
        {
            attackAnimation[i] = new Rectangle(i * spriteWidth, 0, spriteWidth, spriteHeight); 
        }
    }

    protected override void Update(GameTime gameTime)
    {
        // end condition
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        Reset();
        GetInput(); // gets keyboard key presses and modifies player speed 
        ApplyPhysics(gameTime);

        // Check if the timer has exceeded the threshold.
        if (timer > threshold)
        {
            // Last frame of animation, reset to beginning of animation
            if (standingAnimationIndex == standingAnimation.Length - 1)
            {
                standingAnimationIndex = 0;
            }
            // Continue cycling through animation frames 
            else
            {
                standingAnimationIndex++;
            }

            timer = 0;
        }

        // If the timer has not reached the threshold, then add the milliseconds that have past since the last Update() to the timer.
        else
        {
            timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        }

        if (attack && attackAnimationIndex == attackAnimation.Length - 1)
        {
            attack = false;
            attackAnimationIndex = 0;
        }
        else if (attack)
        {
            if (currentAttackFrame % attackFrames == 0)
            {
                attackAnimationIndex++;
            }
            else
            {
                currentAttackFrame++; 
            }
        }

        if (jump && jumpFallAnimationIndex == jumpFallAnimation.Length - 1)
        {
            jump = false;
            jumpFallAnimationIndex = 0;
            fall = true; 
        }
        else if (jump)
        {
            if (currentJumpFrame % jumpFrames == 0)
            {
                jumpFallAnimationIndex++;
            }
            else
            {
                currentJumpFrame++;
            }
        }

        if (onGround())
        {
            fall = false;
            jump = false; 
        }


        base.Update(gameTime);
    }

    public void ApplyPhysics(GameTime gameTime)
    {
        var bounds = GraphicsDevice.Viewport.Bounds;
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        playerPos.X += playerSpeed.X;
        playerPos.Y += playerSpeed.Y;
        if (playerPos.X > bounds.Right - spriteWidth)
        {
            playerPos.X = bounds.Right - spriteWidth;
        }
        if (playerPos.X < bounds.Left)
        {
            playerPos.X = bounds.Left;
        }
        if (playerPos.Y < bounds.Top)
        {
            playerPos.Y = bounds.Top;
        }
        if (playerPos.Y > bounds.Bottom - spriteWidth)
        {
            playerPos.Y = bounds.Bottom - spriteHeight; 
        }

    }

    protected override void Draw(GameTime gameTime)
    {

        GraphicsDevice.Clear(Color.CornflowerBlue);
        //// Flip the sprite to face the way we are moving.
        if (playerSpeed.X < 0)
            flip = SpriteEffects.FlipHorizontally;
       
        // Draw that sprite.
        _spriteBatch.Begin();
        if (onGround() || fall)
        {
           _spriteBatch.Draw(idleSheet, playerPos, standingAnimation[standingAnimationIndex], Color.White, 0f, new Vector2(0, 0), 1f, flip, 0f);

        }
        if (jump)
        {
            _spriteBatch.Draw(jumpFallSheet, playerPos, jumpFallAnimation[jumpFallAnimationIndex], Color.White, 0f, new Vector2(0, 0), 1f, flip, 0f);
        }
        if (attack)
        {
            _spriteBatch.Draw(attackSheet, playerPos, attackAnimation[attackAnimationIndex], Color.White, 0f, new Vector2(0, 0), 1f, flip, 0f);
        }
        // Draw still (non-animated) sprites
        _spriteBatch.Draw(mushroomTexture, mushroomPos, mushroomRectangle, Color.White);
        _spriteBatch.Draw(cactusTexture, cactusPos, cactusRectangle, Color.White);
        _spriteBatch.Draw(brickTexture, brickPos, brickRectangle, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);

        flip = SpriteEffects.None;
    }

    private void DrawRectangle(Rectangle Rec, Color color)
    {
        Vector2 pos = new Vector2(Rec.X, Rec.Y);
        _spriteBatch.Begin();
        _spriteBatch.Draw(texture, pos, Rec,
          color * 1.0f,
          0, Vector2.Zero, 1.0f,
          SpriteEffects.None, 0.00001f);
        _spriteBatch.End();
    }

    private void GetInput()
    {
        // Player speed
        ks = Keyboard.GetState();
        playerSpeed.X = ks.IsKeyDown(Keys.A) ? -baseSpeed : playerSpeed.X;
        playerSpeed.X = ks.IsKeyDown(Keys.D) ? baseSpeed : playerSpeed.X;
        playerSpeed.X = (!ks.IsKeyDown(Keys.A) && !ks.IsKeyDown(Keys.D)) ? 0 : playerSpeed.X;
        // vertical    
        playerSpeed.Y = ks.IsKeyDown(Keys.W) && onGround() ? jumpSpeed : playerSpeed.Y + gravity;
        if (!jump)
        {
            jump = ks.IsKeyDown(Keys.W) ? true : false;
        }
        if (!attack)
        {
            attack = ks.IsKeyDown(Keys.Space) ? true : false;
        }
    }
    private void Reset()
    {
        if (texture == null)
        {   //create texture to draw with if it does not exist
            texture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            texture.SetData<Color>(new Color[] { Color.White });
        }
    }
    private bool onGround()
    {
        var bounds = GraphicsDevice.Viewport.Bounds; 
        bool onGround = playerPos.Y == bounds.Bottom - spriteHeight ? true : false;
        return onGround; 
    }
}

