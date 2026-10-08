extends SceneTree
## Packs the mod's content and its imported texture remaps without invoking
## Godot's C# export plugin. Run from a native Godot project with:
## godot --headless --path NATIVE_PROJECT --script /path/to/pack-resources.gd -- ROOT OUTPUT_PCK

const CONTENT_DIRECTORY := "MoreWeaponsRegentExtend"
const IMPORT_PREFIX := "res://.godot/imported/"

var _source_root: String
var _files: Dictionary = {}
var _failed := false
var _import_count := 0

func _initialize() -> void:
    var arguments := OS.get_cmdline_user_args()
    if arguments.size() != 2:
        _fail("Usage: pack-resources.gd -- rootPath outPck")
        return

    _source_root = arguments[0].replace("\\", "/").simplify_path().trim_suffix("/")
    var output_pack := arguments[1].replace("\\", "/").simplify_path()
    if not _source_root.is_absolute_path() or not output_pack.is_absolute_path():
        _fail("rootPath and outPck must be absolute paths.")
        return
    if not output_pack.begins_with(_source_root + "/"):
        _fail("The output package must stay inside rootPath.")
        return

    var content_path := _source_root.path_join(CONTENT_DIRECTORY)
    if not DirAccess.dir_exists_absolute(content_path):
        _fail("Missing mod resource directory: " + content_path)
        return
    _collect_directory(content_path)
    if _failed:
        return

    var destination_error := DirAccess.make_dir_recursive_absolute(output_pack.get_base_dir())
    if destination_error != OK:
        _fail("Cannot create package directory: " + error_string(destination_error))
        return

    var packer := PCKPacker.new()
    var start_error := packer.pck_start(output_pack)
    if start_error != OK:
        _fail("Cannot create package: " + error_string(start_error))
        return

    var paths := _files.keys()
    paths.sort()
    for resource_path in paths:
        var add_error := packer.add_file(resource_path, _files[resource_path])
        if add_error != OK:
            _fail("Cannot pack " + resource_path + ": " + error_string(add_error))
            return

    var flush_error := packer.flush()
    if flush_error != OK:
        _fail("Cannot finish package: " + error_string(flush_error))
        return
    var packed_file := FileAccess.open(output_pack, FileAccess.READ)
    if packed_file == null:
        _fail("Cannot reopen the finished package.")
        return
    print("PACK_SUCCESS files=", _files.size(), " imported_files=", _import_count,
        " bytes=", packed_file.get_length(), " output=", output_pack)
    quit(0)

func _collect_directory(directory_path: String) -> void:
    var directory := DirAccess.open(directory_path)
    if directory == null:
        _fail("Cannot read resource directory: " + directory_path)
        return
    directory.list_dir_begin()
    var item_name := directory.get_next()
    while not item_name.is_empty() and not _failed:
        var physical_path := directory_path.path_join(item_name)
        if directory.current_is_dir():
            if item_name != ".git":
                _collect_directory(physical_path)
        else:
            var resource_path := "res://" + physical_path.substr(_source_root.length() + 1)
            _files[resource_path] = physical_path
            if item_name.ends_with(".import"):
                _collect_import_remaps(physical_path)
            elif item_name.ends_with(".tscn"):
                _collect_scene_scripts(physical_path)
        item_name = directory.get_next()
    directory.list_dir_end()

func _collect_scene_scripts(scene_path: String) -> void:
    # A compiled C# class still needs its Script resource when a scene attaches it.
    # Include only this mod's C# Script resources referenced by packaged scenes.
    # References to vanilla scripts continue to resolve from the game's own pack.
    var reference_pattern := RegEx.new()
    reference_pattern.compile('\\[ext_resource type="Script"[^\\]]* path="(res://[^"]+)"[^\\]]*\\]')
    for reference in reference_pattern.search_all(FileAccess.get_file_as_string(scene_path)):
        var resource_path: String = reference.get_string(1)
        if not resource_path.begins_with("res://Scripts/") or not resource_path.ends_with(".cs"):
            continue
        var physical_path := _source_root.path_join(resource_path.trim_prefix("res://")).simplify_path()
        if not physical_path.begins_with(_source_root + "/"):
            _fail("Scene script must stay inside rootPath: " + resource_path)
            return
        if not FileAccess.file_exists(physical_path):
            _fail("Missing scene script: " + physical_path)
            return
        _files[resource_path] = physical_path

func _collect_import_remaps(import_path: String) -> void:
    var configuration := ConfigFile.new()
    var config_error := configuration.load(import_path)
    if config_error != OK:
        _fail("Cannot parse import remap: " + import_path)
        return
    if not configuration.has_section("remap"):
        return
    for key in configuration.get_section_keys("remap"):
        if key != "path" and not key.begins_with("path."):
            continue
        var mapped_path = configuration.get_value("remap", key)
        if not mapped_path is String or not mapped_path.begins_with(IMPORT_PREFIX):
            _fail("Unsupported import remap in " + import_path + ": " + str(mapped_path))
            return
        var imported_path: String = _source_root.path_join(mapped_path.trim_prefix("res://"))
        if not FileAccess.file_exists(imported_path):
            _fail("Missing imported resource: " + imported_path)
            return
        if not _files.has(mapped_path):
            _files[mapped_path] = imported_path
            _import_count += 1

func _fail(message: String) -> void:
    _failed = true
    push_error(message)
    quit(1)
