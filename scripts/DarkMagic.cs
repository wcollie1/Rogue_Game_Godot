using Godot;
using System;

public partial class DarkMagic : Area2D
{
	private AnimatedSprite2D sprite;
	private bool hasActivated = false;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		if (sprite == null)
		{
			GD.PrintErr("❌ Could not find AnimatedSprite2D!");
			return;
		}

		sprite.Play("cast");
		sprite.AnimationFinished += OnAnimationFinished;

		// Listen for collisions
		BodyEntered += OnBodyEntered;

		// Optional: disable collision until animation finishes
		SetDeferred("monitoring", false); // disables hit detection until spell is ready
	}

	private void OnAnimationFinished()
	{
		GD.Print("💥 Dark Magic activated at:", GlobalPosition);

		// Enable hitbox when animation ends
		hasActivated = true;
		Monitoring = true; // now we check for hits

		// Optional: short delay before spell disappears
		GetTree().CreateTimer(0.2f).Timeout += QueueFree;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!hasActivated)
			return; // ignore hits until animation finishes

		if (body.IsInGroup("Enemy"))
		{
			GD.Print("💀 Goblin hit by dark magic!");

			if (body is Goblin goblin)
			{
				goblin.TakeDamage(1); // Change this if your goblin uses a different system
			}
		}
	}
}
