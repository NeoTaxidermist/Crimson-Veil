# Builds one creature in Blender: exec this with NAME set, after creature_kit.reset() in a previous call.
#   NAME = "shield_monk"; exec(open(r"C:\Dev\CrimsonVeil\ArtSource\build_creature.py").read())
import sys
import importlib
import bpy

ROOT = r"C:\Dev\CrimsonVeil\ArtSource"
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
import creature_kit as K
importlib.reload(K)

FBX = {
    "shield_monk": "ShieldMonk", "root_crawler": "RootCrawler", "hanged_man": "HangedMan", "crow": "Crow",
    "ash_moth": "AshMoth", "lantern_wisp": "LanternWisp", "paper_kite": "PaperKite",
}[NAME]
exec(open(ROOT + "\\creatures\\" + NAME + ".py").read(), {"__name__": "creature"})
K.export(r"C:\Dev\CrimsonVeil\Assets\_CrimsonVeil\Art\Enemy\\" + FBX + ".fbx")
bpy.ops.wm.save_as_mainfile(filepath=ROOT + "\\" + FBX + ".blend")
result = K.stats()
