using Godot;
using System;

public partial class HoverButton : TextureButton
{
	private AnimationPlayer animationPlayer;

	public override void _Ready()
	{
		// This line looks for the AnimationPlayer node named "HoverMove"
		animationPlayer = GetNode<AnimationPlayer>("HoverMove");

		GD.Print("HoverButton is ready");
	}

	public void _on_TextureButton_mouse_entered()
	{
		GD.Print("Hovered!");
		animationPlayer?.Play("HoverIn");  // <- This is the animation name
	}

	public void _on_TextureButton_mouse_exited()
	{
		GD.Print("Hover exit!");
		animationPlayer?.Play("HoverOut"); // <- Only if you made this one
	}
}
