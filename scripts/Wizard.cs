using Godot;
using System;

public partial class Wizard : CharacterBody2D
{
	[Export]
	public PackedScene DarkMagicScene;

	private AnimatedSprite2D animatedSprite;
	private bool isAttacking = false;
	private Vector2 queuedSpellPosition = Vector2.Zero;

	public override void _Ready()
	{
		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		if (animatedSprite == null)
		{
			GD.PrintErr("❌ AnimatedSprite2D not found!");
			return;
		}

		animatedSprite.AnimationFinished += OnAnimationFinished;
	}

	public override void _Process(double delta)
	{
		if (animatedSprite == null)
			return;

		// Cast spell if space (attack) is pressed
		if (!isAttacking && Input.IsActionJustPressed("attack"))
		{
			isAttacking = true;
			animatedSprite.Play("attack");

			// Lock in the cursor position now
			queuedSpellPosition = GetGlobalMousePosition();

			return;
		}

		// Prevent moving or casting while attacking
		if (isAttacking)
			return;

		// Movement input
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

	private void OnAnimationFinished()
	{
		if (animatedSprite.Animation == "attack")
		{
			isAttacking = false;
			animatedSprite.Play("idle");

			// Spawn the spell at the saved position
			if (DarkMagicScene != null)
			{
				var darkMagic = DarkMagicScene.Instantiate<Area2D>();
				darkMagic.GlobalPosition = queuedSpellPosition;
				GetParent().AddChild(darkMagic);

				GD.Print("✨ Dark Magic cast at:", queuedSpellPosition);
			}
			else
			{
				GD.PrintErr("❌ DarkMagicScene not assigned in Inspector.");
			}
		}
	}
}
