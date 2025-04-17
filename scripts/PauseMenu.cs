using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	public override void _Ready()
	{
		GetNode<Button>("Panel/VBoxContainer/ResumeButton").Pressed += OnResumePressed;
		GetNode<Button>("Panel/VBoxContainer/QuitButton").Pressed += OnQuitPressed;
	}

	public override void _Process(double delta)
	{
		// Toggle pause with Escape
		if (Input.IsActionJustPressed("ui_cancel")) // Default 'Esc' key
		{
			TogglePause();
		}
	}

	private void TogglePause()
	{
		GetTree().Paused = !GetTree().Paused;
		Visible = GetTree().Paused;
	}

	private void OnResumePressed()
	{
		TogglePause();
	}

	private void OnQuitPressed()
	{
		GetTree().Quit(); // Or change scene back to menu
	}
}
