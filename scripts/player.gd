class_name Player extends CharacterBody2D

@onready var tile_map = $"../TileMap"
@onready var ray_cast_2d = $RayCast2D
@onready var isAttacking = false
@onready var animatedSprite = $AnimatedSprite2D

const tile_size: Vector2 = Vector2(16,16)
var sprite_node_pos_tween: Tween
var player_direction_check

#Additional Variables for Click Based Movement
var astar_grid: AStarGrid2D
var current_id_path: Array[Vector2i]
var current_point_path: PackedVector2Array
var target_position: Vector2
var is_moving: bool #part of logic to allow clicking when moving

func _physics_process(_delta: float) -> void:
	#make sure tween is not currently moving the sprite
	if !sprite_node_pos_tween or !sprite_node_pos_tween.is_running():
		#make sure attack animation is not playing
		if !isAttacking:
			#IF ID PATH IS EMPTY. ALLOW WASD TO MOVE
			if current_id_path.is_empty():
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
			
			#IF ID PATH IS NOT EMPTY. MOVE PLAYER TO TARGET POSITION
			if is_moving == false:
				target_position = tile_map.map_to_local(current_id_path.front())
				is_moving = true
				animatedSprite.play("run");

			global_position = global_position.move_toward(target_position, 1)

			if global_position == target_position:
				
				current_id_path.pop_front()
				if current_id_path.is_empty() == false:
					target_position = tile_map.map_to_local(current_id_path.front())
					#Check Direction and Flip Sprite Accordingly
					player_direction_check = global_position.x - target_position.x
					if player_direction_check > 0:
						animatedSprite.flip_h = true
					elif player_direction_check < 0:
						animatedSprite.flip_h = false
				else:
					is_moving = false
		

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
	if tile_data.get_custom_data("walkable") == false:
		return

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
	#Play Idle Animation on Player Start
	animatedSprite.play("idle");
	
	#Setup Astar Grid
	astar_grid = AStarGrid2D.new()
	astar_grid.region = tile_map.get_used_rect()
	astar_grid.cell_size = tile_size
	astar_grid.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_NEVER
	astar_grid.update()
	
	for x in tile_map.get_used_rect().size.x:
		for y in tile_map.get_used_rect().size.y:
			var tile_position = Vector2i(
				x + tile_map.get_used_rect().position.x,
				y + tile_map.get_used_rect().position.y			
			)
			
			var tile_data = tile_map.get_cell_tile_data(0, tile_position)
			
			if tile_data == null or tile_data.get_custom_data("walkable") == false:
				astar_grid.set_point_solid(tile_position)

func _input(event):
	if event.is_action_pressed("move") == false:
		return
	
	var id_path
	
	if is_moving: #If moving, make sure next target square coordinates are grabbed and not current player coord.
		id_path = astar_grid.get_id_path(
			tile_map.local_to_map(target_position),
			tile_map.local_to_map(get_global_mouse_position())
		)
	else:
		id_path = astar_grid.get_id_path(
			tile_map.local_to_map(global_position),
			tile_map.local_to_map(get_global_mouse_position())
		).slice(1)

	if id_path.is_empty() == false:
		current_id_path = id_path
		
		current_point_path = astar_grid.get_point_path(
			tile_map.local_to_map(target_position),
			tile_map.local_to_map(get_global_mouse_position())
		)

		for i in current_point_path.size():
			current_point_path[i] = current_point_path[i] + tile_size / 2
