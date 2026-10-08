extends SceneTree

# Keep scene geometry measured from the visible alpha, never the image canvas.
# The orbit animation itself stays in the existing NWeaponVfx controller.
const WEAPONS = [
	["SeaCalmingStaff", 174.0, "1, 0.65, 0.15"],
	["GalaxyTrajectoryCannon", 180.0, "0.65, 0.25, 1"],
	["HolyCodex", 142.0, "1, 0.9, 0.45"],
	["RoyalBrassKnuckles", 152.0, "1, 0.45, 0.1"],
]

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1:
		push_error("Expected workspace root")
		quit(1)
		return
	var workspace: String = args[0].replace("\\", "/").trim_suffix("/")
	for spec: Array in WEAPONS:
		var name: String = spec[0]
		var texture_path := "res://MoreWeaponsRegentExtend/images/vfx/unified/%s.png" % name
		var image := Image.load_from_file(workspace + "/MoreWeaponsRegentExtend/images/vfx/unified/%s.png" % name)
		if image == null or not image.detect_alpha():
			push_error("Missing transparent sprite: " + name)
			quit(1)
			return
		var bounds := _bounds(image)
		var ratio: float = spec[1] / bounds.size.x
		var center := Vector2(bounds.position) + Vector2(bounds.size) * 0.5 - Vector2(image.get_size()) * 0.5
		var half_size := Vector2(bounds.size) * ratio * 0.5 + Vector2(6, 6)
		var orbit := """[gd_scene load_steps=2 format=3]

[ext_resource type="Texture2D" path="%s" id="1_weapon"]

[node name="%sOrbit" type="Node2D"]

[node name="Visual" type="Node2D" parent="."]

[node name="Weapon" type="Sprite2D" parent="Visual"]
scale = Vector2(%.8f, %.8f)
offset = Vector2(%.3f, %.3f)
texture = ExtResource("1_weapon")

[node name="Hitbox" type="Control" parent="Visual"]
layout_mode = 0
offset_left = %.3f
offset_top = %.3f
offset_right = %.3f
offset_bottom = %.3f
mouse_filter = 2
""" % [texture_path, name, ratio, ratio, -center.x, -center.y, -half_size.x, -half_size.y, half_size.x, half_size.y]
		_write(workspace + "/MoreWeaponsRegentExtend/scenes/orbit/%s.tscn" % name, orbit)
		var cast := _cast_header(name, spec[2], texture_path)
		var cast_scale := 220.0 / bounds.size.x
		cast += """
[node name="Icon" type="Sprite2D" parent="."]
offset = Vector2(%.3f, %.3f)
scale = Vector2(%.8f, %.8f)
texture = ExtResource("1_icon")
""" % [-center.x, -center.y, cast_scale, cast_scale]
		cast += _sparks()
		if name == "GalaxyTrajectoryCannon":
			cast += """
[node name="Beam" type="Line2D" parent="."]
material = SubResource("Material_add")
points = PackedVector2Array(75, 0, 380, 0)
width = 18.0
default_color = Color(0.56, 0.12, 0.9, 0.65)
antialiased = true

[node name="BeamCore" type="Line2D" parent="."]
material = SubResource("Material_add")
points = PackedVector2Array(75, 0, 380, 0)
width = 5.0
default_color = Color(0.86, 0.65, 1, 0.95)
antialiased = true
"""
		_write(workspace + "/MoreWeaponsRegentExtend/scenes/cast/%s.tscn" % name, cast)
		print("SCENE_CREATED ", name, " alpha_bounds=", bounds, " visible_size=", Vector2(bounds.size) * ratio)
	var charge := _cast_header("CannonCharge", "0.65, 0.25, 1", "")
	charge += """
[node name="Icon" type="Node2D" parent="."]

[node name="Frame" type="Polygon2D" parent="Icon"]
polygon = PackedVector2Array(-65, -18, -53, -26, 53, -26, 65, -18, 65, 18, 53, 26, -53, 26, -65, 18)
color = Color(0.7, 0.75, 0.84, 1)

[node name="EnergyCell" type="Polygon2D" parent="Icon"]
polygon = PackedVector2Array(-49, -14, 49, -14, 49, 14, -49, 14)
color = Color(0.25, 0.05, 0.45, 1)

[node name="Segment1" type="Polygon2D" parent="Icon"]
material = SubResource("Material_add")
polygon = PackedVector2Array(-45, -10, -18, -10, -18, 10, -45, 10)
color = Color(0.65, 0.25, 1, 0.95)

[node name="Segment2" type="Polygon2D" parent="Icon"]
material = SubResource("Material_add")
polygon = PackedVector2Array(-13, -10, 13, -10, 13, 10, -13, 10)
color = Color(0.75, 0.4, 1, 0.95)

[node name="Segment3" type="Polygon2D" parent="Icon"]
material = SubResource("Material_add")
polygon = PackedVector2Array(18, -10, 45, -10, 45, 10, 18, 10)
color = Color(0.65, 0.25, 1, 0.95)
"""
	charge += _sparks()
	_write(workspace + "/MoreWeaponsRegentExtend/scenes/cast/CannonCharge.tscn", charge)
	print("SCENE_CREATED CannonCharge native_energy_cell")
	quit(0)

func _cast_header(name: String, color: String, texture_path: String) -> String:
	var header := "[gd_scene load_steps=%d format=3]\n\n" % (7 if texture_path != "" else 6)
	if texture_path != "":
		header += '[ext_resource type="Texture2D" path="%s" id="1_icon"]\n' % texture_path
	header += """[ext_resource type="Texture2D" path="res://MoreWeaponsRegentExtend/images/vfx/cast/impact.svg" id="2_accent"]
[ext_resource type="Texture2D" path="res://MoreWeaponsRegentExtend/images/vfx/update_spark.svg" id="3_spark"]
[ext_resource type="Texture2D" path="res://MoreWeaponsRegentExtend/images/vfx/update_halo.svg" id="4_halo"]

[sub_resource type="CanvasItemMaterial" id="Material_add"]
blend_mode = 1

[sub_resource type="Gradient" id="Gradient_fade"]
offsets = PackedFloat32Array(0, 0.08, 0.45, 1)
colors = PackedColorArray(%s, 0, %s, 1, 1, 1, 1, 0.9, %s, 0)

[node name="%sCastVfx" type="Node2D"]
z_index = 2

[node name="Halo" type="Sprite2D" parent="."]
material = SubResource("Material_add")
modulate = Color(%s, 0.5)
scale = Vector2(0.4, 0.4)
texture = ExtResource("4_halo")

[node name="Slash" type="Sprite2D" parent="."]
material = SubResource("Material_add")
modulate = Color(%s, 0.8)
scale = Vector2(0.5, 0.32)
texture = ExtResource("2_accent")
""" % [color, color, color, name, color, color]
	return header

func _sparks() -> String:
	return """
[node name="Sparks" type="CPUParticles2D" parent="."]
material = SubResource("Material_add")
emitting = false
amount = 18
lifetime = 0.52
one_shot = true
explosiveness = 1.0
texture = ExtResource("3_spark")
direction = Vector2(0, 1)
spread = 55.0
gravity = Vector2(0, 100)
initial_velocity_min = 85.0
initial_velocity_max = 200.0
scale_amount_min = 0.08
scale_amount_max = 0.2
color_ramp = SubResource("Gradient_fade")
emission_shape = 1
emission_sphere_radius = 10.0
"""

func _bounds(image: Image) -> Rect2i:
	var low := image.get_size()
	var high := Vector2i(-1, -1)
	for y in image.get_height():
		for x in image.get_width():
			if image.get_pixel(x, y).a >= 0.05:
				low.x = mini(low.x, x)
				low.y = mini(low.y, y)
				high.x = maxi(high.x, x)
				high.y = maxi(high.y, y)
	return Rect2i(low, high - low + Vector2i.ONE)

func _write(path: String, content: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string(content)
