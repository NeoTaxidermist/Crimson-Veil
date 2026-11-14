# Builds one environment model in Blender: exec with NAME set, after creature_kit.reset() in a previous call.
#   NAME = "Aqueduct"; exec(open(r"C:\Dev\CrimsonVeil\ArtSource\build_env.py").read())
import sys
import importlib
import bpy

ROOT = r"C:\Dev\CrimsonVeil\ArtSource"
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
import creature_kit as K
importlib.reload(K)

SCRIPTS = {"Aqueduct": "aqueduct", "Castle": "castle", "CliffRuin": "cliff_ruin",
           "Bramble": "vegetation", "Weeds": "vegetation", "Chain": "vegetation", "LeafPile": "vegetation",
           "RuinWall": "ruin_pieces", "Tower": "ruin_pieces", "RuinStairs": "ruin_pieces",
           "GrappleRing": "grapple",
           "DeadTree": "background", "Statue": "background", "Cage": "background", "Spires": "background"}
exec(open(ROOT + "\\env\\" + SCRIPTS[NAME] + ".py").read(), {"__name__": "env", "VEG_PART": NAME})
K.export(r"C:\Dev\CrimsonVeil\Assets\_CrimsonVeil\Art\Ruins" + "\\" + NAME + ".fbx")
bpy.ops.wm.save_as_mainfile(filepath=ROOT + "\\env_" + NAME + ".blend")
result = K.stats()
