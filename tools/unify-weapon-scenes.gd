extends SceneTree

# Rebind generated PNG assets without modifying their pixels. Sprite transforms
# use the visible alpha bounds, so differently sized source canvases display alike.
const WEAPONS = {
	"SovereignBludgeon": 150.0,
	"ApocalypseLongbow": 160.0,
	"SovereignCrystalBlade": 170.0,
	"SovereignShield": 138.0,
	"SovereignSpear": 182.0,
	"SovereignAxe": 160.0,
	"SovereignScythe": 180.0,
	"SovereignGun": 148.0,
	"CrystalDagger": 100.0,
	"Bullet": 72.0,
	"NeptuneTrident": 182.0,
	"SovereignKatana": 178.0,
	"SovereignWings": 180.0,
	"SovereignDagger": 115.0,
}
const NEW_CASTS = ["NeptuneTrident", "SovereignKatana", "SovereignWings", "SovereignDagger"]
const MIRROR_ORBITS = ["ApocalypseLongbow", "SovereignBludgeon", "SovereignScythe"]
const RIGHT_ORBITS = ["NeptuneTrident", "SovereignKatana", "SovereignDagger", "SovereignCrystalBlade", "CrystalDagger", "Bullet", "SovereignSpear"]

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1:
		push_error("Usage: -- <repository-root>")
		quit(1)
		return
	var repo := args[0].replace("\\", "/").trim_suffix("/")
	var measurements := {}
	for weapon: String in WEAPONS:
		var relative := "MoreWeaponsRegentExtend/images/vfx/unified/%s.png" % weapon
		var image := Image.load_from_file(repo.path_join(relative))
		if image == null or image.is_empty():
			push_error("Missing weapon: " + relative)
			quit(1)
			return
		var bounds := _visible_bounds(image)
		if bounds.size.x <= 0 or bounds.size.y <= 0 or not image.detect_alpha():
			push_error("Weapon needs visible pixels and genuine transparency: " + relative)
			quit(1)
			return
		measurements[weapon] = {"width": image.get_width(), "height": image.get_height(), "bounds": bounds}
	for weapon: String in WEAPONS:
		var m: Dictionary = measurements[weapon]
		var bounds: Rect2i = m.bounds
		var size := Vector2(bounds.size)
		var center := Vector2(bounds.position) + size * 0.5
		var offset := Vector2(m.width, m.height) * 0.5 - center
		var scale: float = WEAPONS[weapon] / maxf(size.x, size.y)
		var visual_size := size * scale
		if weapon in MIRROR_ORBITS:
			offset.x = -offset.x
		if weapon in RIGHT_ORBITS:
			visual_size = Vector2(visual_size.y, visual_size.x)
		var texture := "res://MoreWeaponsRegentExtend/images/vfx/unified/%s.png" % weapon
		var orbit := "[gd_scene load_steps=2 format=3]\n\n"
		orbit += "[ext_resource type=\"Texture2D\" path=\"%s\" id=\"1_weapon\"]\n\n" % texture
		orbit += "[node name=\"%sOrbit\" type=\"Node2D\"]\n\n" % weapon
		orbit += "[node name=\"Visual\" type=\"Node2D\" parent=\".\"]\n\n"
		orbit += "[node name=\"Weapon\" type=\"Sprite2D\" parent=\"Visual\"]\n"
		if weapon in MIRROR_ORBITS:
			orbit += "flip_h = true\n"
		if weapon in RIGHT_ORBITS:
			orbit += "rotation = 1.5707963\n"
		orbit += "scale = Vector2(%.7f, %.7f)\noffset = Vector2(%.3f, %.3f)\ntexture = ExtResource(\"1_weapon\")\n\n" % [scale, scale, offset.x, offset.y]
		orbit += "[node name=\"Hitbox\" type=\"Control\" parent=\"Visual\"]\nlayout_mode = 0\n"
		orbit += "offset_left = %.3f\noffset_top = %.3f\noffset_right = %.3f\noffset_bottom = %.3f\nmouse_filter = 2\n" % [-visual_size.x * 0.5 - 6, -visual_size.y * 0.5 - 6, visual_size.x * 0.5 + 6, visual_size.y * 0.5 + 6]
		_write(repo.path_join("MoreWeaponsRegentExtend/scenes/orbit/%s.tscn" % weapon), orbit)
		var cast_dir := "MoreWeaponsRegentExtend/scenes/" if weapon in NEW_CASTS else "MoreWeaponsRegentExtend/scenes/cast/"
		var cast_path := repo.path_join(cast_dir + weapon + ".tscn")
		var cast := FileAccess.get_file_as_string(cast_path)
		if weapon == "SovereignWings":
			cast = _update_wings(cast, texture, m)
		else:
			cast = _update_icon(cast, texture, m, weapon)
		_write(cast_path, cast)
		print("UNIFIED ", weapon, " alpha bounds ", bounds, " orbit ", visual_size)
	quit()

func _visible_bounds(image: Image) -> Rect2i:
	# Ignore almost transparent export fringes when positioning the actual weapon.
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

func _write(path: String, content: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	assert(file != null, "Cannot write " + path)
	file.store_string(content)

func _replace_property(scene: String, node_header: String, property: String, value: String) -> String:
	var start := scene.find(node_header)
	assert(start >= 0, "Missing scene node " + node_header)
	var body_start := scene.find("\n", start) + 1
	var end := scene.find("\n[", body_start)
	if end < 0:
		end = scene.length()
	var body := scene.substr(body_start, end - body_start)
	var lines := body.split("\n")
	var found := false
	for i in lines.size():
		if lines[i].begins_with(property + " = "):
			lines[i] = property + " = " + value
			found = true
	if not found:
		lines.insert(0, property + " = " + value)
	return scene.substr(0, body_start) + "\n".join(lines) + scene.substr(end)

func _replace_texture(scene: String, id: String, texture: String) -> String:
	var lines := scene.split("\n")
	for i in lines.size():
		if lines[i].begins_with("[ext_resource ") and lines[i].ends_with("id=\"%s\"]" % id):
			lines[i] = "[ext_resource type=\"Texture2D\" path=\"%s\" id=\"%s\"]" % [texture, id]
	return "\n".join(lines)

func _update_icon(scene: String, texture: String, m: Dictionary, weapon: String) -> String:
	# Keep one-shot motion, particles and timing; normalize only the weapon sprite.
	var desired := 210.0
	if weapon in ["NeptuneTrident", "SovereignSpear", "SovereignCrystalBlade", "SovereignKatana", "SovereignScythe"]:
		desired = 245.0
	elif weapon in ["SovereignDagger", "CrystalDagger"]:
		desired = 160.0
	elif weapon == "Bullet":
		desired = 110.0
	var bounds: Rect2i = m.bounds
	var center := Vector2(bounds.position) + Vector2(bounds.size) * 0.5
	var offset := Vector2(m.width, m.height) * 0.5 - center
	var scale := desired / maxf(bounds.size.x, bounds.size.y)
	scene = _replace_texture(scene, "1_icon", texture)
	var header := "[node name=\"Icon\" type=\"Sprite2D\" parent=\".\"]"
	scene = _replace_property(scene, header, "scale", "Vector2(%.7f, %.7f)" % [scale, scale])
	return _replace_property(scene, header, "offset", "Vector2(%.3f, %.3f)" % [offset.x, offset.y])

func _update_wings(scene: String, texture: String, m: Dictionary) -> String:
	# Both halves reference regions of the same intact PNG. This keeps the accepted
	# LeftWing/RightWing flap pivots while matching the orbit asset exactly.
	var bounds: Rect2i = m.bounds
	var center := Vector2(bounds.position) + Vector2(bounds.size) * 0.5
	var split := roundi(center.x)
	var scale := 340.0 / bounds.size.x
	# The connecting star sits below the center of the spread wings. Swing both
	# halves around that shared hinge, keeping their connection together.
	var hinge := Vector2(center.x, bounds.position.y + bounds.size.y * 0.64)
	scene = _replace_texture(scene, "1_wing", texture)
	scene = _replace_property(scene, "[node name=\"Icon\" type=\"Node2D\" parent=\".\"]", "scale", "Vector2(1, 1)")
	for side: String in ["LeftWing", "RightWing"]:
		var left := side == "LeftWing"
		var x := 0 if left else split
		var width: int = split if left else m.width - split
		var pivot_header := "[node name=\"%s\" type=\"Node2D\" parent=\"Icon\"]" % side
		var pivot := (hinge - center) * scale
		scene = _replace_property(scene, pivot_header, "position", "Vector2(%.3f, %.3f)" % [pivot.x, pivot.y])
		var header := "[node name=\"Feathers\" type=\"Sprite2D\" parent=\"Icon/%s\"]" % side
		var position := (Vector2(x + width * 0.5, m.height * 0.5) - hinge) * scale
		scene = _replace_property(scene, header, "position", "Vector2(%.3f, %.3f)" % [position.x, position.y])
		scene = _replace_property(scene, header, "scale", "Vector2(%.7f, %.7f)" % [scale, scale])
		scene = _replace_property(scene, header, "flip_h", "false")
		scene = _replace_property(scene, header, "region_enabled", "true")
		scene = _replace_property(scene, header, "region_rect", "Rect2(%d, 0, %d, %d)" % [x, width, m.height])
	return scene
