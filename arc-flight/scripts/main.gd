extends Node3D

const CITY_HALF := 72.0
const GROUND_Y := 0.0
const MISSION_TARGET := 15

var player: CharacterBody3D
var suit_visual: Node3D
var camera: Camera3D
var hud_label: Label
var help_label: Label
var crosshair: Label
var health_bar: ProgressBar
var energy_bar: ProgressBar
var message_label: Label

var thrusters: Array[Node3D] = []
var enemies: Array = []
var projectiles: Array = []

var flying := true
var mouse_locked := true
var yaw := 0.0
var pitch := -0.16
var energy := 100.0
var health := 100.0
var score := 0
var kills := 0
var elapsed := 0.0
var repulsor_cooldown := 0.0
var missile_cooldown := 0.0
var damage_cooldown := 0.0
var message_time := 0.0
var shot_side := 1.0

var red_mat: StandardMaterial3D
var gold_mat: StandardMaterial3D
var dark_mat: StandardMaterial3D
var cyan_mat: StandardMaterial3D
var glass_mat: StandardMaterial3D

func _ready() -> void:
	seed(424242)
	_create_materials()
	_create_environment()
	_create_city()
	_create_player()
	_create_camera()
	_create_hud()
	for i in range(10):
		_spawn_drone()
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	_show_message("AEGIS SYSTEM ONLINE", 2.0)
	_update_camera(1.0)

func _create_materials() -> void:
	red_mat = _material(Color(0.42, 0.025, 0.035), Color(0.0, 0.0, 0.0), 0.0, 0.82, 0.24)
	gold_mat = _material(Color(0.88, 0.53, 0.10), Color(0.0, 0.0, 0.0), 0.0, 0.78, 0.22)
	dark_mat = _material(Color(0.035, 0.045, 0.06), Color(0.0, 0.0, 0.0), 0.0, 0.65, 0.32)
	cyan_mat = _material(Color(0.02, 0.30, 0.42), Color(0.0, 0.95, 1.0), 4.0, 0.15, 0.18)
	glass_mat = _material(Color(0.035, 0.08, 0.12), Color(0.0, 0.55, 0.75), 1.8, 0.25, 0.15)

func _material(albedo: Color, emission: Color, emission_energy: float, metallic: float, roughness: float) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = albedo
	mat.metallic = metallic
	mat.roughness = roughness
	if emission_energy > 0.0:
		mat.emission_enabled = true
		mat.emission = emission
		mat.emission_energy_multiplier = emission_energy
	return mat

func _create_environment() -> void:
	var world_env := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.025, 0.045, 0.085)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.28, 0.34, 0.48)
	env.ambient_light_energy = 0.72
	world_env.environment = env
	add_child(world_env)

	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-52.0, -28.0, 0.0)
	sun.light_energy = 1.25
	sun.shadow_enabled = true
	add_child(sun)

	var fill := OmniLight3D.new()
	fill.position = Vector3(0.0, 18.0, 0.0)
	fill.omni_range = 55.0
	fill.light_energy = 2.2
	fill.light_color = Color(0.18, 0.42, 0.82)
	add_child(fill)

func _create_city() -> void:
	var ground := StaticBody3D.new()
	ground.name = "Ground"
	ground.collision_layer = 1
	ground.collision_mask = 2
	add_child(ground)

	var plane_mesh := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(180.0, 180.0)
	plane_mesh.mesh = plane
	plane_mesh.material_override = _material(Color(0.055, 0.065, 0.075), Color(0,0,0), 0.0, 0.1, 0.88)
	ground.add_child(plane_mesh)

	var ground_collision := CollisionShape3D.new()
	var ground_shape := BoxShape3D.new()
	ground_shape.size = Vector3(180.0, 0.2, 180.0)
	ground_collision.shape = ground_shape
	ground_collision.position.y = -0.1
	ground.add_child(ground_collision)

	# Roads.
	var road_mat := _material(Color(0.018, 0.021, 0.026), Color(0,0,0), 0.0, 0.0, 0.92)
	for i in range(-3, 4):
		var road_x := _box_mesh(Vector3(7.0, 0.035, 150.0), Vector3(float(i) * 20.0, 0.025, 0.0), road_mat)
		add_child(road_x)
		var road_z := _box_mesh(Vector3(150.0, 0.035, 7.0), Vector3(0.0, 0.028, float(i) * 20.0), road_mat)
		add_child(road_z)

	# Procedural skyline.
	for gx in range(-3, 4):
		for gz in range(-3, 4):
			if abs(gx) <= 0 and abs(gz) <= 0:
				continue
			var base_x := float(gx) * 20.0
			var base_z := float(gz) * 20.0
			for lot in range(2):
				var height := randf_range(5.5, 22.0)
				var sx := randf_range(5.0, 8.5)
				var sz := randf_range(5.0, 8.5)
				var offset := Vector3((-4.8 if lot == 0 else 4.8), height * 0.5, randf_range(-4.0, 4.0))
				var tint := Color(randf_range(0.06, 0.12), randf_range(0.08, 0.15), randf_range(0.12, 0.22))
				var bmat := _material(tint, Color(0.0, 0.10, 0.18), 0.32, 0.32, 0.7)
				var building := _box_mesh(Vector3(sx, height, sz), Vector3(base_x, 0.0, base_z) + offset, bmat)
				add_child(building)
				# A glowing rooftop beacon.
				var beacon := _sphere_mesh(0.16, Vector3(base_x, height + 0.35, base_z) + Vector3(offset.x, 0.0, offset.z), cyan_mat)
				add_child(beacon)

func _create_player() -> void:
	player = CharacterBody3D.new()
	player.name = "AegisSuit"
	player.position = Vector3(0.0, 4.0, 10.0)
	player.collision_layer = 2
	player.collision_mask = 1
	add_child(player)

	var collision := CollisionShape3D.new()
	var capsule := CapsuleShape3D.new()
	capsule.radius = 0.42
	capsule.height = 1.85
	collision.shape = capsule
	collision.position.y = 0.95
	player.add_child(collision)

	suit_visual = Node3D.new()
	suit_visual.name = "SuitVisual"
	player.add_child(suit_visual)

	# Torso armor.
	_mesh_box_child(suit_visual, Vector3(0.86, 1.05, 0.42), Vector3(0, 1.18, 0), red_mat)
	_mesh_box_child(suit_visual, Vector3(0.58, 0.34, 0.46), Vector3(0, 1.54, -0.01), gold_mat)
	_mesh_box_child(suit_visual, Vector3(0.74, 0.38, 0.40), Vector3(0, 0.68, 0), dark_mat)

	# Helmet.
	_mesh_sphere_child(suit_visual, 0.34, Vector3(0, 1.98, 0), red_mat, Vector3(1.0, 1.1, 0.92))
	_mesh_box_child(suit_visual, Vector3(0.47, 0.22, 0.08), Vector3(0, 1.96, -0.29), gold_mat)
	_mesh_box_child(suit_visual, Vector3(0.14, 0.045, 0.035), Vector3(-0.12, 2.02, -0.335), cyan_mat)
	_mesh_box_child(suit_visual, Vector3(0.14, 0.045, 0.035), Vector3(0.12, 2.02, -0.335), cyan_mat)

	# Arms.
	for side in [-1.0, 1.0]:
		_mesh_box_child(suit_visual, Vector3(0.26, 0.82, 0.28), Vector3(0.58 * side, 1.18, 0.0), red_mat)
		_mesh_box_child(suit_visual, Vector3(0.28, 0.30, 0.31), Vector3(0.58 * side, 0.72, -0.01), gold_mat)
		var palm := _sphere_mesh(0.11, Vector3(0.58 * side, 0.55, -0.11), cyan_mat)
		suit_visual.add_child(palm)
		palm.position = Vector3(0.58 * side, 0.55, -0.11)

	# Legs and boots.
	for side in [-1.0, 1.0]:
		_mesh_box_child(suit_visual, Vector3(0.30, 0.82, 0.34), Vector3(0.23 * side, 0.05, 0.0), red_mat)
		_mesh_box_child(suit_visual, Vector3(0.33, 0.34, 0.48), Vector3(0.23 * side, -0.47, -0.06), gold_mat)
		var thruster := _sphere_mesh(0.12, Vector3.ZERO, cyan_mat)
		suit_visual.add_child(thruster)
		thruster.position = Vector3(0.23 * side, -0.69, 0.02)
		thruster.scale = Vector3(0.8, 1.8, 0.8)
		thrusters.append(thruster)

	# Arc-style energy core, intentionally original geometry.
	var core := MeshInstance3D.new()
	var core_mesh := CylinderMesh.new()
	core_mesh.top_radius = 0.17
	core_mesh.bottom_radius = 0.17
	core_mesh.height = 0.055
	core.mesh = core_mesh
	core.material_override = cyan_mat
	core.rotation_degrees.x = 90.0
	core.position = Vector3(0, 1.22, -0.235)
	suit_visual.add_child(core)

func _create_camera() -> void:
	camera = Camera3D.new()
	camera.name = "ChaseCamera"
	camera.fov = 78.0
	camera.current = true
	camera.position = Vector3(0.0, 5.0, 17.0)
	add_child(camera)

func _create_hud() -> void:
	var canvas := CanvasLayer.new()
	canvas.layer = 10
	add_child(canvas)

	hud_label = Label.new()
	hud_label.position = Vector2(20, 18)
	hud_label.size = Vector2(390, 120)
	hud_label.add_theme_font_size_override("font_size", 22)
	canvas.add_child(hud_label)

	health_bar = ProgressBar.new()
	health_bar.position = Vector2(20, 116)
	health_bar.size = Vector2(300, 18)
	health_bar.max_value = 100.0
	health_bar.value = 100.0
	health_bar.show_percentage = false
	canvas.add_child(health_bar)

	energy_bar = ProgressBar.new()
	energy_bar.position = Vector2(20, 141)
	energy_bar.size = Vector2(300, 18)
	energy_bar.max_value = 100.0
	energy_bar.value = 100.0
	energy_bar.show_percentage = false
	canvas.add_child(energy_bar)

	var htxt := Label.new()
	htxt.text = "ARMOR"
	htxt.position = Vector2(328, 112)
	canvas.add_child(htxt)
	var etxt := Label.new()
	etxt.text = "ARC"
	etxt.position = Vector2(328, 137)
	canvas.add_child(etxt)

	help_label = Label.new()
	help_label.anchor_left = 1.0
	help_label.anchor_right = 1.0
	help_label.offset_left = -440.0
	help_label.offset_right = -18.0
	help_label.offset_top = 18.0
	help_label.offset_bottom = 238.0
	help_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	help_label.add_theme_font_size_override("font_size", 16)
	help_label.text = "WASD  Move\nSPACE / CTRL  Up / Down\nSHIFT  Boost\nF  Flight toggle\nLMB  Repulsor\nRMB or Q  Micro missile\nR  Reset suit\nESC  Release / capture mouse"
	canvas.add_child(help_label)

	crosshair = Label.new()
	crosshair.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	crosshair.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	crosshair.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	crosshair.text = "+"
	crosshair.add_theme_font_size_override("font_size", 30)
	canvas.add_child(crosshair)

	message_label = Label.new()
	message_label.anchor_left = 0.5
	message_label.anchor_right = 0.5
	message_label.offset_left = -350.0
	message_label.offset_right = 350.0
	message_label.offset_top = 86.0
	message_label.offset_bottom = 140.0
	message_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	message_label.add_theme_font_size_override("font_size", 26)
	canvas.add_child(message_label)

func _physics_process(delta: float) -> void:
	if player == null or camera == null:
		return

	var cam_forward := -camera.global_transform.basis.z
	cam_forward.y = 0.0
	if cam_forward.length_squared() < 0.001:
		cam_forward = Vector3.FORWARD
	else:
		cam_forward = cam_forward.normalized()

	var cam_right := camera.global_transform.basis.x
	cam_right.y = 0.0
	if cam_right.length_squared() > 0.001:
		cam_right = cam_right.normalized()

	var x_input := float(Input.is_key_pressed(KEY_D)) - float(Input.is_key_pressed(KEY_A))
	var z_input := float(Input.is_key_pressed(KEY_W)) - float(Input.is_key_pressed(KEY_S))
	var wish := cam_right * x_input + cam_forward * z_input
	if wish.length() > 1.0:
		wish = wish.normalized()

	var boost := flying and Input.is_key_pressed(KEY_SHIFT) and energy > 0.5
	var speed := 31.0 if boost else (14.0 if flying else 9.0)

	if flying:
		var vertical := float(Input.is_key_pressed(KEY_SPACE)) - float(Input.is_key_pressed(KEY_CTRL) or Input.is_key_pressed(KEY_C))
		var flight_wish := wish + Vector3.UP * vertical
		if flight_wish.length() > 1.0:
			flight_wish = flight_wish.normalized()
		var target_velocity := flight_wish * speed
		player.velocity = player.velocity.lerp(target_velocity, minf(1.0, delta * 5.8))
		player.move_and_slide()
		if boost and flight_wish.length_squared() > 0.01:
			energy = maxf(0.0, energy - 17.0 * delta)
		else:
			energy = minf(100.0, energy + 5.5 * delta)
	else:
		if not player.is_on_floor():
			player.velocity.y -= 24.0 * delta
		if Input.is_key_pressed(KEY_SPACE) and player.is_on_floor():
			player.velocity.y = 8.0
		player.velocity.x = move_toward(player.velocity.x, wish.x * speed, 26.0 * delta)
		player.velocity.z = move_toward(player.velocity.z, wish.z * speed, 26.0 * delta)
		player.move_and_slide()
		energy = minf(100.0, energy + 12.0 * delta)

	player.rotation.y = lerp_angle(player.rotation.y, yaw, minf(1.0, delta * 7.0))

	var horizontal_speed := Vector2(player.velocity.x, player.velocity.z).length()
	var target_lean := -0.78 if flying and horizontal_speed > 3.0 else 0.0
	suit_visual.rotation.x = lerpf(suit_visual.rotation.x, target_lean, minf(1.0, delta * 5.0))
	for t in thrusters:
		t.visible = flying
		var pulse := 1.0 + sin(elapsed * 18.0) * 0.18
		t.scale = Vector3(0.8, (2.7 if boost else 1.8) * pulse, 0.8)

	if player.global_position.y < -12.0 or player.global_position.length() > 170.0:
		_reset_suit()

func _process(delta: float) -> void:
	elapsed += delta
	repulsor_cooldown = maxf(0.0, repulsor_cooldown - delta)
	missile_cooldown = maxf(0.0, missile_cooldown - delta)
	damage_cooldown = maxf(0.0, damage_cooldown - delta)
	message_time = maxf(0.0, message_time - delta)
	if message_time <= 0.0 and message_label != null:
		message_label.text = ""

	_update_camera(delta)
	_handle_weapons()
	_update_drones(delta)
	_update_projectiles(delta)
	_update_hud()

	if health <= 0.0:
		_show_message("SUIT CRITICAL — AUTO RECOVERY", 1.6)
		score = maxi(0, score - 250)
		_reset_suit()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and mouse_locked:
		yaw -= event.relative.x * 0.0032
		pitch = clampf(pitch - event.relative.y * 0.0032, -1.05, 0.45)

	if event is InputEventKey and event.pressed and not event.echo:
		var code := event.physical_keycode if event.physical_keycode != 0 else event.keycode
		match code:
			KEY_F:
				flying = not flying
				if flying:
					player.velocity.y = maxf(player.velocity.y, 0.0)
				_show_message("FLIGHT MODE: " + ("ON" if flying else "OFF"), 1.0)
			KEY_Q:
				_fire_missile()
			KEY_R:
				_reset_suit()
			KEY_ESCAPE:
				mouse_locked = not mouse_locked
				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED if mouse_locked else Input.MOUSE_MODE_VISIBLE)

	if event is InputEventMouseButton and event.pressed and not mouse_locked:
		mouse_locked = true
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var orbit := Basis(Vector3.UP, yaw) * Basis(Vector3.RIGHT, pitch)
	var target := player.global_position + Vector3(0.0, 1.15, 0.0)
	var desired := target + orbit * Vector3(0.0, 1.55, 7.2)
	camera.global_position = camera.global_position.lerp(desired, 1.0 - exp(-8.5 * delta))
	var aim_point := target + orbit * Vector3(0.0, 0.0, -12.0)
	camera.look_at(aim_point, Vector3.UP)

func _handle_weapons() -> void:
	if not mouse_locked:
		return
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		_fire_repulsor()
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		_fire_missile()

func _fire_repulsor() -> void:
	if repulsor_cooldown > 0.0 or energy < 2.0 or camera == null:
		return
	repulsor_cooldown = 0.13
	energy -= 2.0
	shot_side *= -1.0
	var dir := -camera.global_transform.basis.z.normalized()
	var origin := player.global_position + Vector3(0.0, 1.15, 0.0) + camera.global_transform.basis.x * (0.47 * shot_side) + dir * 0.9
	_spawn_projectile("repulsor", origin, dir * 58.0, 1.4, cyan_mat)

func _fire_missile() -> void:
	if missile_cooldown > 0.0 or energy < 10.0 or camera == null:
		return
	missile_cooldown = 1.05
	energy -= 10.0
	var dir := -camera.global_transform.basis.z.normalized()
	var origin := player.global_position + Vector3(0.0, 1.45, 0.0) + dir * 1.0
	var orange := _material(Color(0.45, 0.13, 0.02), Color(1.0, 0.28, 0.02), 4.0, 0.35, 0.3)
	_spawn_projectile("missile", origin, dir * 34.0, 4.0, orange)

func _spawn_projectile(kind: String, origin: Vector3, velocity: Vector3, ttl: float, mat: Material) -> void:
	var visual := MeshInstance3D.new()
	var mesh := SphereMesh.new()
	mesh.radius = 0.09 if kind == "repulsor" else 0.14
	mesh.height = mesh.radius * 2.0
	visual.mesh = mesh
	visual.material_override = mat
	visual.global_position = origin
	add_child(visual)
	projectiles.append({
		"node": visual,
		"velocity": velocity,
		"ttl": ttl,
		"kind": kind
	})

func _update_projectiles(delta: float) -> void:
	for i in range(projectiles.size() - 1, -1, -1):
		var p: Dictionary = projectiles[i]
		var node: Node3D = p["node"]
		if not is_instance_valid(node):
			projectiles.remove_at(i)
			continue

		var velocity: Vector3 = p["velocity"]
		if p["kind"] == "missile":
			var target := _nearest_drone(node.global_position)
			if target != null:
				var desired := (target.global_position - node.global_position).normalized() * maxf(34.0, velocity.length())
				velocity = velocity.lerp(desired, minf(1.0, delta * 4.5))
				p["velocity"] = velocity

		node.global_position += velocity * delta
		p["ttl"] = float(p["ttl"]) - delta
		projectiles[i] = p

		var hit_radius := 0.75 if p["kind"] == "repulsor" else 1.45
		var hit := false
		for e in range(enemies.size()):
			var enemy_node: Node3D = enemies[e]["node"]
			if is_instance_valid(enemy_node) and node.global_position.distance_to(enemy_node.global_position) < hit_radius:
				_damage_enemy(e, 42.0 if p["kind"] == "repulsor" else 100.0)
				hit = true
				break

		if hit or float(p["ttl"]) <= 0.0 or node.global_position.length() > 220.0:
			if is_instance_valid(node):
				node.queue_free()
			projectiles.remove_at(i)

func _spawn_drone() -> void:
	var drone := Node3D.new()
	drone.name = "InterceptorDrone"
	var angle := randf_range(0.0, TAU)
	var radius := randf_range(25.0, 58.0)
	drone.position = Vector3(cos(angle) * radius, randf_range(5.0, 14.0), sin(angle) * radius)
	add_child(drone)

	_mesh_sphere_child(drone, 0.72, Vector3.ZERO, dark_mat, Vector3(1.2, 0.55, 1.2))
	_mesh_box_child(drone, Vector3(2.1, 0.16, 0.32), Vector3.ZERO, red_mat)
	_mesh_box_child(drone, Vector3(0.36, 0.12, 1.85), Vector3.ZERO, dark_mat)
	_mesh_sphere_child(drone, 0.16, Vector3(0.0, 0.0, -0.64), cyan_mat, Vector3.ONE)

	enemies.append({
		"node": drone,
		"hp": 100.0,
		"phase": randf_range(0.0, TAU),
		"speed": randf_range(2.3, 3.6)
	})

func _update_drones(delta: float) -> void:
	for i in range(enemies.size()):
		var data: Dictionary = enemies[i]
		var drone: Node3D = data["node"]
		if not is_instance_valid(drone):
			continue
		var to_player := (player.global_position + Vector3(0.0, 2.0, 0.0)) - drone.global_position
		var distance := to_player.length()
		if distance > 0.01:
			var tangent := Vector3(-to_player.z, 0.0, to_player.x).normalized()
			var chase := to_player.normalized() * float(data["speed"])
			drone.global_position += (chase + tangent * 1.15) * delta
			drone.global_position.y += sin(elapsed * 2.0 + float(data["phase"])) * 0.012
			drone.look_at(player.global_position + Vector3(0.0, 1.0, 0.0), Vector3.UP)

		if distance < 2.4 and damage_cooldown <= 0.0:
			damage_cooldown = 0.42
			health = maxf(0.0, health - 8.0)
			_show_message("ARMOR IMPACT", 0.35)

func _damage_enemy(index: int, damage: float) -> void:
	if index < 0 or index >= enemies.size():
		return
	var data: Dictionary = enemies[index]
	data["hp"] = float(data["hp"]) - damage
	if float(data["hp"]) <= 0.0:
		var node: Node3D = data["node"]
		if is_instance_valid(node):
			node.queue_free()
		enemies.remove_at(index)
		kills += 1
		score += 100
		if kills == MISSION_TARGET:
			_show_message("MISSION COMPLETE — AIRSPACE SECURED", 4.0)
		elif kills < MISSION_TARGET:
			_show_message("DRONE DOWN  %d/%d" % [kills, MISSION_TARGET], 0.8)
		call_deferred("_spawn_drone")
	else:
		enemies[index] = data

func _nearest_drone(from: Vector3) -> Node3D:
	var best: Node3D = null
	var best_distance := INF
	for data in enemies:
		var node: Node3D = data["node"]
		if is_instance_valid(node):
			var d := from.distance_squared_to(node.global_position)
			if d < best_distance:
				best_distance = d
				best = node
	return best

func _update_hud() -> void:
	if hud_label == null:
		return
	health_bar.value = health
	energy_bar.value = energy
	var mode := "FLIGHT" if flying else "GROUND"
	var boost_text := "  BOOST" if flying and Input.is_key_pressed(KEY_SHIFT) and energy > 0.5 else ""
	hud_label.text = "AEGIS // %s%s\nSCORE  %06d\nMISSION  DRONES %d/%d" % [mode, boost_text, score, mini(kills, MISSION_TARGET), MISSION_TARGET]

func _show_message(text: String, seconds: float) -> void:
	if message_label != null:
		message_label.text = text
	message_time = maxf(message_time, seconds)

func _reset_suit() -> void:
	if player == null:
		return
	player.global_position = Vector3(0.0, 5.0, 10.0)
	player.velocity = Vector3.ZERO
	health = 100.0
	energy = 100.0
	flying = true

func _box_mesh(size: Vector3, pos: Vector3, mat: Material) -> MeshInstance3D:
	var mesh_instance := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	mesh_instance.mesh = mesh
	mesh_instance.position = pos
	mesh_instance.material_override = mat
	return mesh_instance

func _sphere_mesh(radius: float, pos: Vector3, mat: Material) -> MeshInstance3D:
	var mesh_instance := MeshInstance3D.new()
	var mesh := SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius * 2.0
	mesh_instance.mesh = mesh
	mesh_instance.position = pos
	mesh_instance.material_override = mat
	return mesh_instance

func _mesh_box_child(parent: Node3D, size: Vector3, pos: Vector3, mat: Material) -> MeshInstance3D:
	var node := _box_mesh(size, pos, mat)
	parent.add_child(node)
	return node

func _mesh_sphere_child(parent: Node3D, radius: float, pos: Vector3, mat: Material, scale_value: Vector3) -> MeshInstance3D:
	var node := _sphere_mesh(radius, pos, mat)
	node.scale = scale_value
	parent.add_child(node)
	return node
