extends SceneTree

var assertions := 0

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() < 3:
		_fail("Usage: -- <formal.pck> <repository-root> <workshop-preview-image> [card-frame-prototypes...]")
		return
	if not _check(ProjectSettings.load_resource_pack(args[0]), "formal resource PCK loads"):
		return
	var tables := ["cards", "powers", "card_keywords", "events"]
	var english := {}
	for locale: String in ["zhs", "eng"]:
		for table: String in tables:
			var relative := "MoreWeaponsRegentExtend/localization/%s/%s.json" % [locale, table]
			var packed := "res://" + relative
			var source := args[1].path_join(relative)
			if not _check(FileAccess.file_exists(packed), "packaged localization " + relative):
				return
			if not _check(FileAccess.get_sha256(packed) == FileAccess.get_sha256(source), "exact source SHA256 " + relative):
				return
			var texts = JSON.parse_string(FileAccess.get_file_as_string(packed))
			if not _check(texts is Dictionary, "valid packaged JSON " + relative):
				return
			if locale == "eng":
				english[table] = texts
	for entry: Array in [["cards", 40], ["powers", 19], ["card_keywords", 16]]:
		if not _check(english[entry[0]].size() == entry[1], "complete " + entry[0] + " keys=" + str(entry[1])):
			return
	for table: String in tables:
		var zhs = JSON.parse_string(FileAccess.get_file_as_string("res://MoreWeaponsRegentExtend/localization/zhs/%s.json" % table))
		for key: String in zhs:
			if not _check(english[table].has(key) and english[table][key] is String and not english[table][key].is_empty(), "English key " + key):
				return
	var cards := ["ApocalypseLongbow", "Bullet", "CannonCharge", "CrystalDagger", "GalaxyTrajectoryCannon", "HandyWeapon", "HolyCodex", "NeptuneTrident", "RoyalBrassKnuckles", "SeaCalmingStaff", "SovereignAxe", "SovereignBludgeon", "SovereignCrystalBlade", "SovereignDagger", "SovereignGun", "SovereignKatana", "SovereignScythe", "SovereignShield", "SovereignSpear", "SovereignWings"]
	for card: String in cards:
		var texture := load("res://MoreWeaponsRegentExtend/images/cards/%s.png" % card) as Texture2D
		if not _check(texture != null and texture.get_width() > 0, "actual packaged card portrait " + card):
			return
	for page: String in ["Initial", "Light", "Heavy", "Ranged", "Special"]:
		var texture := load("res://MoreWeaponsRegentExtend/images/events/DepartureArmory%s.png" % page) as Texture2D
		if not _check(texture != null and texture.get_width() >= 512, "actual packaged armory illustration " + page):
			return
	var cover := Image.new()
	if not _check(cover.load(args[2]) == OK and cover.get_width() >= 512 and cover.get_height() >= 512, "native workshop preview image loads at readable resolution"):
		return
	var file := FileAccess.open(args[2], FileAccess.READ)
	if not _check(file != null and file.get_length() < 1000000, "workshop preview is strictly below one megabyte"):
		return
	print("WORKSHOP_PREVIEW ", args[2], " ", cover.get_size(), " bytes=", file.get_length())
	for frame_path: String in args.slice(3):
		var frame := Image.new()
		if not _check(frame.load(frame_path) == OK and frame.get_width() > 0 and frame.get_height() > 0, "native prototype image loads " + frame_path):
			return
		print("FRAME_PROTOTYPE ", frame_path, " ", frame.get_size(), " format=", frame.get_format())
	print("RELEASE_PACK_SUCCESS assertions=", assertions, " locales=2 tables=8 cards=20 powers=8 keywords=8 armory_portraits=5")
	quit(0)

func _check(passed: bool, label: String) -> bool:
	assertions += 1
	if not passed:
		_fail(label)
		return false
	print("PASS ", assertions, " ", label)
	return true

func _fail(message: String) -> void:
	push_error(message)
	quit(1)
