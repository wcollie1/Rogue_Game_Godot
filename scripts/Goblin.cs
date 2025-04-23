using Godot;
using System;

public partial class Goblin : CharacterBody2D
{
	private AnimatedSprite2D _animatedSprite;
	private Area2D _attackHitbox;

	// Movement-related exports
	[Export] public float Speed = 30f;
	[Export] public float PatrolTime = 2.0f;

	// Boundaries (to avoid wandering off-screen)
	[Export] public float MinX = 0f;
	[Export] public float MaxX = 1024f;
	[Export] public float MinY = 0f;
	[Export] public float MaxY = 600f;

	// Internal variables
	private Vector2 _direction = Vector2.Zero;
	private bool _isAttacking = false;
	private bool _isIdle = false;

	// Patrol/idle timers
	private float _patrolTimer = 0f;
	private float _idleTimer = 0f;
	private float _idleDuration = 0f;

	// Chase state
	private bool _chasePlayer = false;
	private float _chaseTimer = 0f;
	private float _chaseSpeedBuff = 1.5f;  // +50% speed
	private Node2D _player;               // Reference to player that triggered the hitbox

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_attackHitbox = GetNode<Area2D>("AttackHitbox");

		// Always monitor so we can detect the player entering
		_attackHitbox.Monitoring = true;
		_attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;

		GD.Print("Goblin ready");

		GD.Randomize();
		PickRandomDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		// If attacking, face the player (if known) and do nothing else
		if (_isAttacking)
		{
			FacePlayerIfKnown();
			// Reset run animation speed if we’re attacking
			_animatedSprite.SpeedScale = 1.0f;
			return;
		}

		// If chasing
		if (_chasePlayer)
		{
			// Speed up run animation by 50%
			_animatedSprite.SpeedScale = 1.5f;

			// Count down the chase timer
			_chaseTimer -= (float)delta;
			if (_chaseTimer <= 0f)
			{
				// End chase mode
				_chasePlayer = false;
				// Reset patrol logic
				_patrolTimer = 0f;
			}
			else
			{
				// Move towards the player’s position
				if (_player != null && _player.IsInsideTree())
				{
					_direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				}

				// Apply 50% speed buff
				Vector2 velocity = _direction * Speed * _chaseSpeedBuff;
				Velocity = velocity;
				MoveAndSlide();

				// Clamp to bounds but DO NOT invert direction (avoids flipping bug)
				ClampToBounds(doBounce: false);

				// Flip horizontally if needed
				_animatedSprite.FlipH = (_direction.X < 0);

				// Play run animation
				_animatedSprite.Play("run");
				return;
			}
		}

		// If not chasing, reset run animation speed to normal
		_animatedSprite.SpeedScale = 1.0f;

		// If idle
		if (_isIdle)
		{
			_animatedSprite.Play("idle");
			_idleTimer += (float)delta;
			if (_idleTimer >= _idleDuration)
			{
				// Done idling, go back to patrol
				_isIdle = false;
				_idleTimer = 0f;
				_patrolTimer = 0f;
			}
			return;
		}

		// Otherwise, do normal patrol
		_patrolTimer += (float)delta;
		if (_patrolTimer >= PatrolTime)
		{
			_patrolTimer = 0f;
			// Random chance to idle
			if (GD.Randf() < 0.3f)
			{
				_isIdle = true;
				_idleDuration = (float)GD.RandRange(1.0, 2.5);
				_idleTimer = 0f;
			}
			else
			{
				// Pick a new random direction
				PickRandomDirection();
			}
		}

		Vector2 patrolVelocity = _direction * Speed;
		Velocity = patrolVelocity;
		MoveAndSlide();

		// Clamp with bounces in normal patrol mode
		ClampToBounds(doBounce: true);

		// Flip sprite horizontally if walking left
		_animatedSprite.FlipH = (_direction.X < 0);

		// Play run animation
		_animatedSprite.Play("run");
	}

	/// <summary>
	/// Called when the player enters the Goblin’s hitbox.
	/// </summary>
	private void OnAttackHitboxAreaEntered(Area2D area)
	{
		if (area.IsInGroup("Player"))
		{
			GD.Print("Player hit by Goblin!");

			// Store the player so we can chase them
			_player = (Node2D)area;

			// Start chasing with a speed boost for 5 seconds
			_chasePlayer = true;
			_chaseTimer = 5f;

			// Immediately do an attack if not already attacking
			if (!_isAttacking)
				StartAttack();
		}
	}

	/// <summary>
	/// Randomly choose attack1 or attack2, face the player, play the animation, then reset.
	/// </summary>
	private async void StartAttack()
	{
		_isAttacking = true;

		// 50/50 chance to pick attack1 or attack2
		string chosenAttack = (GD.Randi() % 2 == 0) ? "attack1" : "attack2";
		GD.Print($"Start Attack! Anim: {chosenAttack}");

		// Face the player
		FacePlayerIfKnown();

		// Play the chosen animation
		_animatedSprite.Play(chosenAttack);

		// Wait for the animation to finish
		await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);

		_isAttacking = false;
	}

	/// <summary>
	/// If we know the player’s location, flip sprite to face them horizontally.
	/// </summary>
private void FacePlayerIfKnown()
{

}
	/// <summary>
	/// Choose a new random direction in 360 degrees for wandering.
	/// </summary>
	private void PickRandomDirection()
	{
		float angle = (float)GD.RandRange(0, Math.PI * 2);
		_direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized();
	}

	/// <summary>
	/// Clamps the Goblin’s position to MinX/MaxX/MinY/MaxY.
	/// If doBounce == true and we hit a boundary, invert that axis so we bounce.
	/// If doBounce == false, we only clamp position (useful for chasing).
	/// </summary>
	private void ClampToBounds(bool doBounce)
	{
		Vector2 clamped = GlobalPosition;
		bool hitX = false;
		bool hitY = false;

		if (clamped.X < MinX)
		{
			clamped.X = MinX;
			hitX = true;
		}
		else if (clamped.X > MaxX)
		{
			clamped.X = MaxX;
			hitX = true;
		}

		if (clamped.Y < MinY)
		{
			clamped.Y = MinY;
			hitY = true;
		}
		else if (clamped.Y > MaxY)
		{
			clamped.Y = MaxY;
			hitY = true;
		}

		GlobalPosition = clamped;

		// Only bounce if doBounce is true
		if (doBounce)
		{
			if (hitX) _direction.X = -_direction.X;
			if (hitY) _direction.Y = -_direction.Y;
		}
	}
}
