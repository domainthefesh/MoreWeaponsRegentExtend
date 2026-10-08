extends SceneTree

const PREFIX := "MORE_WEAPONS_REGENT_EXTEND_EVENT_DEPARTURE_ARMORY"
const PAGES := {
	"LIGHT": ["SOVEREIGNCRYSTALBLADE", "SOVEREIGNDAGGER", "NEPTUNETRIDENT", "SOVEREIGNKATANA", "SEACALMINGSTAFF"],
	"HEAVY": ["SOVEREIGNSCYTHE", "SOVEREIGNAXE", "SOVEREIGNBLUDGEON", "SOVEREIGNSPEAR", "SOVEREIGNSHIELD"],
	"RANGED": ["APOCALYPSELONGBOW", "SOVEREIGNGUN", "GALAXYTRAJECTORYCANNON"],
	"SPECIAL": ["SOVEREIGNWINGS", "HOLYCODEX", "ROYALBRASSKNUCKLES"]
}

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0]):
		_fail("Cannot load release resource pack")
		return
	for page: String in ["Initial", "Light", "Heavy", "Ranged", "Special"]:
		var texture := load("res://MoreWeaponsRegentExtend/images/events/DepartureArmory%s.png" % page) as Texture2D
		if texture == null or texture.get_width() < 512 or texture.get_height() < 512:
			_fail("Missing event illustration: " + page)
			return
		print("PASS armory illustration ", page, " ", texture.get_size())
	var texts = JSON.parse_string(FileAccess.get_file_as_string("res://MoreWeaponsRegentExtend/localization/zhs/events.json"))
	if not texts is Dictionary:
		_fail("Cannot parse event localization")
		return
	for key: String in ["title", "pages.INITIAL.description", "pages.DONE.description", "pages.EVERY_FORGE_DONE.description", "pages.NOT_REGENT.description"]:
		if not _require(texts, key):
			return
	if not texts[PREFIX + ".pages.INITIAL.description"].contains("[red][b]这次[/b][/red]"):
		_fail("Initial description is missing red bold emphasis")
		return
	for option: String in ["CHOOSE_WEAPON", "RANDOM_ONCE", "EVERY_FORGE"]:
		for suffix: String in ["title", "description"]:
			if not _require(texts, "pages.INITIAL.options.%s.%s" % [option, suffix]):
				return
	for page: String in PAGES:
		for suffix: String in ["title", "description"]:
			if not _require(texts, "pages.CATEGORIES.options.%s.%s" % [page, suffix]):
				return
		if not _require(texts, "pages.%s.description" % page):
			return
		for option: String in PAGES[page]:
			for suffix: String in ["title", "description"]:
				if not _require(texts, "pages.%s.options.%s.%s" % [page, option, suffix]):
					return
		for weapon: String in PAGES[page]:
			if not _require(texts, "weapons." + weapon):
				return
	if not _require(texts, "pages.CATEGORIES.description"):
		return
	for option: String in ["NEXT", "FIRST", "BACK"]:
		for suffix: String in ["title", "description"]:
			if not _require(texts, "pages.NAVIGATION.options.%s.%s" % [option, suffix]):
				return
	print("ARMORY_PACK_SUCCESS illustrations=5 initial_options=3 groups=4 weapons=16 navigation=3")
	quit(0)

func _require(texts: Dictionary, key: String) -> bool:
	var full_key := PREFIX + "." + key
	if not texts.has(full_key) or not texts[full_key] is String or texts[full_key].is_empty():
		_fail("Missing event text: " + full_key)
		return false
	return true

func _fail(message: String) -> void:
	push_error(message)
	quit(1)
