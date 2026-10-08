extends SceneTree

const CARDS = ["HandyWeapon", "NeptuneTrident", "SovereignKatana", "SovereignWings", "SovereignDagger", "SovereignBludgeon", "ApocalypseLongbow", "SovereignCrystalBlade", "SovereignShield", "SovereignSpear", "SovereignAxe", "SovereignScythe", "SovereignGun", "CrystalDagger", "Bullet", "SeaCalmingStaff", "GalaxyTrajectoryCannon", "HolyCodex", "RoyalBrassKnuckles", "CannonCharge"]
const POWERS = ["HighGroundPower", "SovereignShieldPower", "BleedPower", "KingGunPower", "HeavyPressurePower", "StaffStunPower", "MartialFanaticPower", "AdaptationPower"]
const NEW_CASTS = ["HandyWeapon", "NeptuneTrident", "SovereignKatana", "SovereignWings", "SovereignDagger"]
const CAST_ONLY = ["HandyWeapon", "CannonCharge"]

func _initialize() -> void:
	call_deferred("verify")

func verify() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0]):
		_fail("Cannot load release resource pack")
		return
	var orbit_curve := load("res://MoreWeaponsRegentExtend/scenes/orbit/SovereignWeaponOrbit.tres") as Curve2D
	if orbit_curve == null or orbit_curve.point_count != 5 or orbit_curve.get_baked_length() <= 0:
		_fail("Missing original weapon orbit curve in release pack")
		return
	print("PASS original five-point weapon orbit curve in release pack")
	for card: String in CARDS:
		var portrait := load("res://MoreWeaponsRegentExtend/images/cards/%s.png" % card) as Texture2D
		if portrait == null or portrait.get_width() < 100 or portrait.get_height() < 100:
			_fail("Missing portrait: " + card)
			return
		# Keep the established 1250:950 portrait framing without resampling assets.
		var ratio := float(portrait.get_width()) / portrait.get_height()
		if absf(ratio - 1250.0 / 950.0) > 0.06:
			_fail("Portrait aspect ratio changed: " + card + " " + str(ratio))
			return
		print("PASS B portrait ", card, " ", portrait.get_size())
		var cast_dir := "" if card in NEW_CASTS else "cast/"
		if not await _scene(card, cast_dir, false):
			return
		if card not in CAST_ONLY:
			if not await _scene(card, "orbit/", true):
				return
	for power: String in POWERS:
		var icon := load("res://MoreWeaponsRegentExtend/images/powers/%s.png" % power) as Texture2D
		if icon == null or icon.get_width() < 1 or not icon.get_image().detect_alpha():
			_fail("Missing transparent D status icon: " + power)
			return
		print("PASS D status icon ", power)
	var powers = JSON.parse_string(FileAccess.get_file_as_string("res://MoreWeaponsRegentExtend/localization/zhs/powers.json"))
	if not powers is Dictionary:
		_fail("Cannot parse power localization")
		return
	var text: String = powers.get("MORE_WEAPONS_REGENT_EXTEND_POWER_HIGH_GROUND_POWER.description", "")
	if text.count("50%") != 2 or text.contains("{"):
		_fail("HighGround tooltip contains unresolved words: " + text)
		return
	print("ART_PACK_SUCCESS portraits=20 casts=20 orbits=18 status_icons=8 tooltip=1")
	quit(0)

func _scene(card: String, directory: String, orbit: bool) -> bool:
	var scene := load("res://MoreWeaponsRegentExtend/scenes/%s%s.tscn" % [directory, card]) as PackedScene
	if scene == null or not scene.can_instantiate():
		_fail("Cannot instantiate " + directory + card)
		return false
	var instance := scene.instantiate() as Node2D
	root.add_child(instance)
	for child in instance.find_children("*", "", true, false):
		if child is Sprite2D or child is CPUParticles2D:
			if child.texture == null:
				_fail("Missing scene texture: " + str(child.get_path()))
				return false
	if orbit:
		if not instance.has_node("Visual/Weapon") or not instance.has_node("Visual/Hitbox"):
			_fail("Missing hover/motion nodes: " + card)
			return false
		var visual := instance.get_node("Visual") as Node2D
		var sprite := visual.get_node("Weapon") as Sprite2D
		var hitbox := visual.get_node("Hitbox") as Control
		var image := sprite.texture.get_image()
		if visual.position != Vector2.ZERO or visual.rotation != 0 or visual.scale != Vector2.ONE:
			_fail("Invalid orbit rest transform: " + card)
			return false
		if instance.z_index != 0 or instance.is_set_as_top_level() or not instance.z_as_relative:
			_fail("Orbit escapes combat layering: " + card)
			return false
		if not image.detect_alpha():
			_fail("Orbit weapon has an opaque background: " + card)
			return false
		var bounds := _visible_bounds(image)
		var size := Vector2(bounds.size) * sprite.scale
		var center_delta := Vector2(bounds.position) + Vector2(bounds.size) * 0.5 - Vector2(image.get_size()) * 0.5
		if sprite.flip_h:
			center_delta.x = -center_delta.x
		var center := ((center_delta + sprite.offset) * sprite.scale).rotated(sprite.rotation)
		var should_mirror := card in ["ApocalypseLongbow", "SovereignBludgeon", "SovereignScythe"]
		var should_turn := card in ["NeptuneTrident", "SovereignKatana", "SovereignDagger", "SovereignCrystalBlade", "CrystalDagger", "Bullet", "SovereignSpear"]
		if sprite.flip_h != should_mirror or absf(sprite.rotation - (PI * 0.5 if should_turn else 0.0)) > 0.001:
			_fail("Incorrect weapon orientation: " + card)
			return false
		if should_turn:
			size = Vector2(size.y, size.x)
		if center.length() > 0.1 or maxf(size.x, size.y) > 185 or minf(size.x, size.y) <= 0:
			_fail("Orbit size/centering inconsistent: " + card)
			return false
		if hitbox.size.x + 0.1 < size.x or hitbox.size.y + 0.1 < size.y:
			_fail("Hover hitbox does not cover weapon: " + card)
			return false
		await process_frame
		await process_frame
		if not is_instance_valid(instance):
			_fail("Orbit unexpectedly freed itself: " + card)
			return false
	else:
		var required := ["Icon", "Halo", "Sparks"]
		if directory == "cast/" or card in ["SovereignKatana", "SovereignDagger"]:
			required.append("Slash")
		if card == "HandyWeapon":
			required.append("Icon/LidPivot")
		if card == "NeptuneTrident":
			required.append("Lightning")
		if card == "SovereignWings":
			required.append_array(["Icon/LeftWing/Feathers", "Icon/RightWing/Feathers"])
		if card == "SovereignDagger":
			required.append("Coins")
		if card == "CannonCharge":
			required.append_array(["Icon/Frame", "Icon/EnergyCell"])
		for node: String in required:
			if not instance.has_node(node):
				_fail("Missing cast animation node: " + card + "/" + node)
				return false
		if card == "CannonCharge":
			var icon := instance.get_node("Icon")
			if not icon is Node2D or icon is Sprite2D:
				_fail("Charge icon is not a native energy cell: " + card)
				return false
			var energy_cell := icon.get_node("EnergyCell") as Polygon2D
			if energy_cell == null or energy_cell.polygon.size() < 3 or energy_cell.color.a <= 0:
				_fail("Charge energy cell is not a visible native polygon: " + card)
				return false
		elif card != "HandyWeapon":
			var sprite := instance.get_node("Icon/LeftWing/Feathers" if card == "SovereignWings" else "Icon") as Sprite2D
			if sprite == null or sprite.texture == null or not sprite.texture.resource_path.ends_with("/unified/%s.png" % card):
				_fail("Cast and orbit refer to different weapon identities: " + card)
				return false
	print("PASS C scene ", directory, card)
	instance.queue_free()
	await process_frame
	return true

func _visible_bounds(image: Image) -> Rect2i:
	var minimum := image.get_size()
	var maximum := Vector2i(-1, -1)
	for y in image.get_height():
		for x in image.get_width():
			if image.get_pixel(x, y).a >= 0.05:
				minimum.x = mini(minimum.x, x)
				minimum.y = mini(minimum.y, y)
				maximum.x = maxi(maximum.x, x)
				maximum.y = maxi(maximum.y, y)
	return Rect2i(minimum, maximum - minimum + Vector2i.ONE)

func _fail(message: String) -> void:
	push_error(message)
	quit(1)
