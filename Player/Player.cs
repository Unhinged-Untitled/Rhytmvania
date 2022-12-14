using Godot;
using System;

public class Player : KinematicBody2D
{
    private const float GRAVITY = 200; //"constant" downwards acceleration
    private const float JUMP_VELOCITY = 2000; //speed to apply when jumping

    private const float JUMP_REDUCER = 0.1f; //when is jump released, divide the velocity by this to jump lower (only if going up)

    private const float SPEED_UP = 100; //velocity to add when moving
    private const float SPEED_DOWN = 200f; //when player stops moving, this will decrease the horizontal speed;
    private const float MAX_SPEED = 700; //maximum horizontal speed;
    private const float COYOTE_TIME = 0.5f; // how long in seconds after falling can player coyote jump;
    private const float BUFFER_TIME = 0.5f; // how long in seconds before landing can player buffer jump;

    private Vector2 velocity = Vector2.Zero;
    private Timer coyote;
    private Timer buffer;

    private bool wasPreviouslyOnFloor; //used to check when to activate coyote and jump buffer
    private bool isJumping; // true only while going up after player jumped
    private bool coyoteReady; //should the coyote start? (false if just jumped)

    bool CanJump => //can start jumping during this frame 
        IsOnFloor() || CanCoyoteJump;

    bool CanCoyoteJump => //can perform coyote jump
        !IsOnFloor() && coyote.TimeLeft > 0;

    private bool ShouldBufferJump => //after landing on ground, will jump automatically if true
        Input.IsActionPressed("jump") && buffer.TimeLeft > 0;

    private bool JustEnteredFloor => //IsOnFloor() changed to true during this frame
        !wasPreviouslyOnFloor && IsOnFloor();

    private bool JustLeftFloor => //IsOnFloor() changed to false during this frame
        wasPreviouslyOnFloor && !IsOnFloor();

    //Dash system variables
    private Vector2 mousepos;
    private const double MAX_DASH_DISTANCE = 200; //How far you can dash by pressing space
    private const double DASH_DURATION = 0.4; //Each dash is this many seconds long
    private double dash_distance; //How far the current dash is set to take you
    private double dash_velocity; //How fast you're dashing in this frame
    private double dash_deceleration; //How fast you're slowing down during the dash
    private bool canDash; //Whether you can or can't dash right now
    private float dash_direction; // False if dashing left, true if right


    void Jump()
    {
        velocity.y = -JUMP_VELOCITY; //maybe will be changed in future 
        
        isJumping = true;
        coyoteReady = false;
        coyote.Stop();
        buffer.Stop();
    }



    public override void _Ready()
    {
        coyote = GetNode<Timer>("Coyote");
        buffer = GetNode<Timer>("Buffer");
    }

    public override void _PhysicsProcess(float delta) 
    {

        //Player can always move horizontally (if not, then change all of this i guess)
        if (IsOnWall())
        {
            velocity.x = 0;
        }

        if (IsOnCeiling())
        {
            velocity.y = 0;
        }

        bool acceleratedSideways = false;

        if (Input.IsActionPressed("move_left") && !Input.IsActionPressed("move_right"))
        {
            velocity.x -= SPEED_UP;
            velocity.x = Mathf.Clamp(velocity.x, -MAX_SPEED, MAX_SPEED);
            acceleratedSideways = true;
        }

        if (!Input.IsActionPressed("move_left") && Input.IsActionPressed("move_right"))
        {
            velocity.x += SPEED_UP;
            velocity.x = Mathf.Clamp(velocity.x, -MAX_SPEED, MAX_SPEED);
            acceleratedSideways = true;
        }


        //Slowing down
        if ((!acceleratedSideways) && velocity.x != 0)
        {
            if (Mathf.Abs(velocity.x) > SPEED_DOWN)
            {
                velocity.x -= SPEED_DOWN * Mathf.Sign(velocity.x);
            }
            else
            {
                velocity.x = 0;
            }
        }

        if (JustEnteredFloor)
        {
            velocity.y = 1;
            if (ShouldBufferJump)
            {
                Jump();                
            }
            else
            {
                buffer.Stop();
                coyoteReady = true;
            }
        }

        if (JustLeftFloor)
        {
            if (coyoteReady)
            {
                coyote.Start(COYOTE_TIME);
            }
        }

        if (!IsOnFloor())
        {
            velocity.y += GRAVITY;
            if (Input.IsActionJustPressed("jump"))
            {
                buffer.Start(BUFFER_TIME);
            }
        }

        if (CanJump && Input.IsActionJustPressed("jump"))
        {
            Jump();
        }

         // GD.Print(isJumping);
        if (isJumping)
        {
            //GD.Print(velocity.y);
            if (velocity.y >= 0) //If no longer going up
            {
                isJumping = false;
            }
            else
            {
                if (Input.IsActionJustReleased("jump"))
                {
                    //GD.Print("slow down");
                    velocity.y *= JUMP_REDUCER;
                    isJumping = false;
                }
            }
        }
        wasPreviouslyOnFloor = IsOnFloor();


        //Start of dash code

        if(canDash == true && Input.IsActionPressed("space")){ //This only triggers at the start of a dash
            Dash(); //Set the dash velocity to its starting speed, the function is coded below
            canDash = false;
        }
        if(dash_direction * dash_velocity > 0){ //
            canDash = false;
            dash_velocity -= dash_deceleration * delta * dash_direction;
        }
        else
        {
            dash_deceleration = 0;
            dash_velocity = 0;
            dash_distance = 0;
            dash_direction = 0;
            canDash = true;
        }
            
        
        canDash = (dash_velocity == 0);


            velocity.x += (float)dash_velocity;
        //End of dash code


        MoveAndSlide(velocity, Vector2.Up);
        
        //Dash debug

        GD.Print("=======");
        GD.Print("Distance: " + dash_distance);
        GD.Print("Velocity: " + dash_velocity);
        GD.Print("Deceleratiion: " + dash_deceleration);
        GD.Print("Delta: " + delta);
        GD.Print("Duration: " + DASH_DURATION);
        GD.Print("CanDash: " + canDash);
        GD.Print("LocalMousePos " + GetLocalMousePosition().x);
        GD.Print("Dash direction: " + dash_direction);
/*
        GD.Print("==================");
        GD.Print("velocity.y:    " + velocity.y);
        GD.Print("CoyoteTime:    " + coyote.TimeLeft);
        GD.Print("BufferTime:    " + buffer.TimeLeft);
        GD.Print("CanJump:       " + CanJump);
        GD.Print("CanCoyoteJump: " + CanCoyoteJump);
*/

//Dash function used in the dash code
    }
    void Dash() //Trigger the start of a dash.
    {
        dash_distance = GetLocalMousePosition().x; //Calcualte how far you'll dash
        dash_velocity = 2 * dash_distance / DASH_DURATION; // Set the starting dash velocity
        dash_deceleration = Math.Abs(dash_velocity / DASH_DURATION); //Calculate how much you'll slow down each frame
        if(dash_velocity < 0) {dash_direction = -1;} else {dash_direction = 1;} //Set which way you're dashing
    }
}