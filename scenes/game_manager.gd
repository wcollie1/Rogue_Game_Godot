extends Node

# Game Manager is responsible for managing the game state
#   1. Starting and Stopping the game
#   2. Handling player turns
#   3. Handling AI turns

@export var player_character: CharacterBody2D
@export var ai_characters: Array = [] # Updated to hold a list of AI characters

@export var current_character: CharacterBody2D

@export var in_combat: bool = false

func _ready():
    current_character = $Player
    # Populate the list of AI characters dynamically if needed
    ai_characters = get_tree().get_nodes_in_group("Enemy") # Assumes AI characters are in a group
    pass

func start_combat():
    # Initialize combat state
    in_combat = true
    current_character = null

    print("Combat has Started.")

    # Start the first turn
    next_turn()

func end_combat():
    # End combat state
    in_combat = false
    current_character = null

    # Optionally, you can trigger any end of combat UI or logic here
    print("Combat has ended.")

func next_turn():
    # If not in combat, do not proceed to the next turn
    if !in_combat:
        return

    if current_character == null:
        # If no current turn is set, start with the player character
        current_character = player_character
        return
    else:
        current_character.end_turn()
        # If current turn is set, switch to the next character in line
        if current_character == player_character:
            # Cycle through AI characters
            for ai_character in ai_characters:
                current_character = ai_character

                # Disable Player Controls / UI
                player_character.disable_controls()

                # Trigger AI turn
                var ai_thinking_time = randf_range(0.5, 2.0)
                await get_tree().create_timer(ai_thinking_time).timeout
                ai_character.start_turn()
                await get_tree().create_timer(ai_thinking_time).timeout

                # Turn has completed, trigger next turn
                next_turn()
        else:
            current_character = player_character

            # Enable Player Controls / UI
            player_character.enable_controls()

            # Start Player turn
            player_character.start_turn()

func player_cast_combat_action(action: String, target: CharacterBody2D):
    if !in_combat:
        return

    if current_character != player_character:
        return

    # Player casts a combat action
    player_character.cast_combat_action(action, target)

    # Proceed to next turn
    next_turn()
    
func ai_decide_combat_action():
    if !in_combat:
        return
    if ai_characters.find(current_character, 0) == -1:
        return

    # AI decides on a combat action
    current_character.decide_combat_action()

    # Proceed to next turn
    next_turn()