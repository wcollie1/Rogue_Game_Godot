class_name Player extends CharacterBody2D

@onready var tile_map = $"../TileMap"
@onready var ray_cast_2d = $RayCast2D
@onready var isAttacking = false
@onready var animatedSprite = $AnimatedSprite2D

const tile_size: Vector2 = Vector2(16,16)
var sprite_node_pos_tween: Tween
 
func _physics_process(_delta: float) -> void:
	#make sure tween is not currently moving the sprite
	if !sprite_node_pos_tween or !sprite_node_pos_tween.is_running():
		#make sure attack animation is not playing
		if !isAttacking:
			#return to idle position animation
			animatedSprite.play("idle");
			
			#check for user inputs
			if Input.is_action_pressed("up"):
				_move(Vector2(0,-1))
			elif Input.is_action_pressed("down"):
				_move(Vector2(0,1))
			elif Input.is_action_pressed("left"):
				_move(Vector2(-1,0))
			elif Input.is_action_pressed("right"):
				_move(Vector2(1,0))
			elif !isAttacking && Input.is_action_just_pressed("attack"):
				pass #Code to do the Attack Animation Stuff
			return;

func _move(direction: Vector2):
	# Get current tile Vector2i
	var current_tile: Vector2i = tile_map.local_to_map(global_position)
	# Get target tile Vector2i
	var target_tile: Vector2i = Vector2i(
		current_tile.x + direction.x,
		current_tile.y + direction.y
	)
	# Get custom data layer from the target tile
	var tile_data: TileData = tile_map.get_cell_tile_data(0, target_tile)
	
# MAKE SURE TILE IS WALKABLE FIRST
#	if tile_data.get_custom_data("walkable") == false:
#		return

# MAKE SURE NO BODY Target is detected by the RAYCAST
	ray_cast_2d.target_position = direction * 16
	ray_cast_2d.force_raycast_update()
	if ray_cast_2d.is_colliding():
		return
	
	#Check Direction and Flip Sprite Accordingly
	if direction.x < 0:
		animatedSprite.flip_h = true
	elif direction.x > 0:
		animatedSprite.flip_h = false

	# Move player
	global_position = tile_map.map_to_local(target_tile)
	$AnimatedSprite2D.global_position = tile_map.map_to_local(current_tile)
	
	if sprite_node_pos_tween:
		sprite_node_pos_tween.kill()
	animatedSprite.play("run");
	sprite_node_pos_tween = create_tween()
	sprite_node_pos_tween.set_process_mode(Tween.TWEEN_PROCESS_PHYSICS)
	sprite_node_pos_tween.tween_property($AnimatedSprite2D, "global_position", global_position, .3).set_trans(Tween.TRANS_SINE)
	
#start player animation when game starts
func _ready() -> void:
	animatedSprite.play("idle");
