using Godot;
using System;

public partial class Goblin : CharacterBody2D
{
	private AnimatedSprite2D _animatedSprite;
	private Area2D _attackHitbox;
	private Node2D _player;

	// Movement-related exports
	[Export] public float Speed = 30f;
	[Export] public float PatrolTime = 2.0f;

	// Bounding box for random patrol (so Goblin won’t wander off)
	[Export] public float MinX = 0f;
	[Export] public float MaxX = 1024f;
	[Export] public float MinY = 0f;
	[Export] public float MaxY = 600f;

	// Goblin health system
	[Export] public int MaxHealth = 3; // Dies after 3 hits
	private int _currentHealth;

	// Internal state
	private bool _isAttacking = false;
	private bool _isIdle = false;
	private bool _isTakingDamage = false;
	private bool _isDead = false;
	private bool _chasePlayer = false;

	// Timers
	private float _patrolTimer = 0f;
	private float _idleTimer = 0f;
	private float _idleDuration = 0f;

	// Chasing logic
	private float _chaseTimer = 0f;
	private float _chaseSpeedBuff = 1.5f;  // 50% speed bonus

	// Current movement direction
	private Vector2 _direction = Vector2.Zero;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_attackHitbox = GetNode<Area2D>("AttackHitbox");

		// Enable hit detection for the Goblin’s AttackHitbox
		_attackHitbox.Monitoring = true;
		_attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;

		_currentHealth = MaxHealth;

		GD.Randomize();
		PickRandomDirection();

		GD.Print("Goblin is ready with MaxHealth=", MaxHealth);
	}

	public override void _PhysicsProcess(double delta)
	{
		// If dead or in the middle of “take_damage”, do nothing else.
		if (_isDead || _isTakingDamage)
		{
			return;
		}

		// If attacking, face the player but skip normal movement
		if (_isAttacking)
		{
			FacePlayerIfKnown();
			return;
		}

		// If chasing
		if (_chasePlayer)
		{
			// Speed up run animation while chasing
			_animatedSprite.SpeedScale = 1.5f;

			_chaseTimer -= (float)delta;
			if (_chaseTimer <= 0f)
			{
				// Stop chasing, reset to normal
				_chasePlayer = false;
				_patrolTimer = 0f;
				_animatedSprite.SpeedScale = 1.0f; // reset anim speed
			}
			else
			{
				// Move toward the player
				if (_player != null && _player.IsInsideTree())
				{
					_direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				}

				// Move with speed buff
				Vector2 velocity = _direction * Speed * _chaseSpeedBuff;
				Velocity = velocity;
				MoveAndSlide();

				// Clamp but don’t bounce in chase mode (avoids rapid flip at edges)
				ClampToBounds(doBounce: false);

				FacePlayerIfKnown();
				_animatedSprite.Play("run");
				return;
			}
		}

		// If idle
		if (_isIdle)
		{
			_animatedSprite.Play("idle");
			_idleTimer += (float)delta;
			if (_idleTimer >= _idleDuration)
			{
				_isIdle = false;
				_idleTimer = 0f;
				_patrolTimer = 0f;
			}
			return;
		}

		// Otherwise, do normal patrol (random direction)
		_patrolTimer += (float)delta;
		if (_patrolTimer >= PatrolTime)
		{
			_patrolTimer = 0f;
			// 30% chance to idle
			if (GD.Randf() < 0.3f)
			{
				_isIdle = true;
				_idleDuration = (float)GD.RandRange(1.0, 2.5);
				_idleTimer = 0f;
			}
			else
			{
				PickRandomDirection();
			}
		}

		Vector2 patrolVel = _direction * Speed;
		Velocity = patrolVel;
		MoveAndSlide();

		// Bounce if hitting boundary
		ClampToBounds(doBounce: true);

		// Flip horizontally if going left
		_animatedSprite.FlipH = (_direction.X < 0);

		// Normal running
		_animatedSprite.SpeedScale = 1.0f;
		_animatedSprite.Play("run");
	}

	/// <summary>
	/// If the Player enters the Goblin’s AttackHitbox, chase them & do an attack.
	/// </summary>
	private void OnAttackHitboxAreaEntered(Area2D area)
	{
		if (area.IsInGroup("Player"))
		{
			GD.Print("Player hit by Goblin!");
			_player = (Node2D)area;

			// Start chase with 5s timer
			_chasePlayer = true;
			_chaseTimer = 5f;

			// Attack if not already
			if (!_isAttacking)
				StartAttack();
		}
	}

	/// <summary>
	/// Randomly choose attack1 or attack2, face the player, then resume.
	/// </summary>
	private async void StartAttack()
	{
		if (_isDead) return; // do not attack if dead

		_isAttacking = true;
		FacePlayerIfKnown();

		// 50/50 chance for attack1 or attack2
		string chosenAttack = (GD.Randi() % 2 == 0) ? "attack1" : "attack2";
		_animatedSprite.Play(chosenAttack);
		GD.Print($"Goblin uses {chosenAttack}");

		// Wait for anim
		await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);

		_isAttacking = false;
	}

	/// <summary>
	/// Called by spells, bullets, etc. Subtract HP, play “take_damage” or “death” anim.
	/// </summary>
	public async void TakeDamage(int damageAmount)
	{
		if (_isDead)
			return; // Already dead? Do nothing.

		_currentHealth -= damageAmount;
		GD.Print($"Goblin took {damageAmount} damage; HP now {_currentHealth}");

		if (_currentHealth <= 0)
		{
			_isDead = true;
			_animatedSprite.Play("death");
			GD.Print("Goblin is dead. Playing 'death' animation...");

			// Wait for the death anim to finish, then remove from scene
			await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
			QueueFree();
		}
		else
		{
			// Show “take_damage” anim
			_isTakingDamage = true;
			_animatedSprite.Play("take_damage");

			// Wait for that anim, then go back
			await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
			_isTakingDamage = false;
		}
	}

	/// <summary>
	/// Face the player horizontally (skip flipping if X difference is tiny).
	/// </summary>
	private void FacePlayerIfKnown()
	{
		if (_player != null && _player.IsInsideTree())
		{
			Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
			if (Mathf.Abs(toPlayer.X) > 0.1f)
			{
				_animatedSprite.FlipH = (toPlayer.X < 0);
			}
		}
	}

	/// <summary>
	/// Pick a new random direction for patrol (360 degrees).
	/// </summary>
	private void PickRandomDirection()
	{
		float angle = (float)GD.RandRange(0, Math.PI * 2);
		_direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized();
	}

	/// <summary>
	/// Clamps the Goblin’s position to the area; if doBounce is true, invert direction on edges.
	/// </summary>
	private void ClampToBounds(bool doBounce)
	{
		Vector2 clamped = GlobalPosition;
		bool hitX = false;
		bool hitY = false;

		if (clamped.X < MinX) { clamped.X = MinX; hitX = true; }
		else if (clamped.X > MaxX) { clamped.X = MaxX; hitX = true; }

		if (clamped.Y < MinY) { clamped.Y = MinY; hitY = true; }
		else if (clamped.Y > MaxY) { clamped.Y = MaxY; hitY = true; }

		GlobalPosition = clamped;

		if (doBounce)
		{
			if (hitX) _direction.X = -_direction.X;
			if (hitY) _direction.Y = -_direction.Y;
		}
	}
}
