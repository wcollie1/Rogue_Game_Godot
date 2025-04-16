using Godot;
using System;

public partial class Sprite2d : CharacterBody2D
{
	private AnimatedSprite2D animatedSprite;
	private bool isAttacking = false;

	// Update this path to match your actual scene hierarchy!
	private const string ANIMATION_PATH = "AnimatedSprite2D";  // Change to "Graphics/AnimatedSprite2D" if needed

	public override void _Ready()
	{
		animatedSprite = GetNodeOrNull<AnimatedSprite2D>(ANIMATION_PATH);
		if (animatedSprite == null)
		{
			GD.PrintErr($"❌ Could not find AnimatedSprite2D at path: '{ANIMATION_PATH}'");
			return;
		}

		animatedSprite.AnimationFinished += OnAnimationFinished;
	}

	private void OnAnimationFinished()
	{
		if (animatedSprite.Animation == "attack")
		{
			isAttacking = false;
			animatedSprite.Play("idle");
		}
	}

	public override void _Process(double delta)
	{
		if (animatedSprite == null)
			return; // Avoid null errors

		if (!isAttacking && Input.IsActionJustPressed("attack"))
		{
			isAttacking = true;
			animatedSprite.Play("attack");
			return;
		}

		if (isAttacking)
			return;

		Vector2 direction = Vector2.Zero;
		float speed = 250;

		if (Input.IsKeyPressed(Key.W)) direction.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) direction.Y += 1;
		if (Input.IsKeyPressed(Key.A)) direction.X -= 1;
		if (Input.IsKeyPressed(Key.D)) direction.X += 1;

		if (direction != Vector2.Zero)
		{
			Position += direction.Normalized() * speed * (float)delta;
			animatedSprite.FlipH = direction.X < 0;

			if (animatedSprite.Animation != "run")
				animatedSprite.Play("run");
		}
		else
		{
			if (animatedSprite.Animation != "idle")
				animatedSprite.Play("idle");
		}
	}
}
