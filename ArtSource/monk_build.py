# Builds the shield monk (ShieldMonk.blend -> Assets/_CrimsonVeil/Art/Enemy/ShieldMonk.fbx): a broad, heavy warrior
# monk - a deep hood with two crimson eyes in its shadow, a shoulder mantle with torn ends over a heavy robe that
# falls to the ground in ragged strips, a rope belt with hanging ends, a rosary hanging from his right hand - and his
# great kite shield (a separate solid object "Shield", which the game moves: raised, bashing, torn away).
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\monk_build.py").read())
# Blender space: Z up, faces -Y, its left is +X. Cloth ends, belt cords and beads are Tatter.<n>.<k> chains.
exec(open(r"C:\Dev\CrimsonVeil\ArtSource\rig_kit.py").read())

# ---------------------------------------------------------------- skeleton
bone("Hips", (0, 0.02, 0.85), (0, 0.0, 1.15), None)
bone("Spine", (0, 0.0, 1.15), (0, -0.03, 1.42), "Hips")
bone("Chest", (0, -0.03, 1.42), (0, -0.08, 1.72), "Spine")
bone("Neck", (0, -0.08, 1.72), (0, -0.12, 1.84), "Chest")
bone("Head", (0, -0.12, 1.84), (0, -0.14, 2.08), "Neck")
for side, sx in (("L", 1.0), ("R", -1.0)):
    bone("Shoulder." + side, (0.08 * sx, -0.06, 1.66), (0.26 * sx, -0.05, 1.66), "Chest")
if True:  # left arm forward to the shield's grip, right arm hanging with the rosary
    bone("UpperArm.L", (0.26, -0.05, 1.66), (0.32, -0.16, 1.36), "Shoulder.L")
    bone("LowerArm.L", (0.32, -0.16, 1.36), (0.22, -0.44, 1.22), "UpperArm.L")
    bone("Hand.L", (0.22, -0.44, 1.22), (0.18, -0.54, 1.20), "LowerArm.L")
    bone("UpperArm.R", (-0.26, -0.05, 1.66), (-0.32, -0.08, 1.34), "Shoulder.R")
    bone("LowerArm.R", (-0.32, -0.08, 1.34), (-0.32, -0.2, 1.08), "UpperArm.R")
    bone("Hand.R", (-0.32, -0.2, 1.08), (-0.31, -0.24, 0.97), "LowerArm.R")


def zone(p, s):
    """Robe weights by height: hips below the belt, the spine through the belly, the chest above."""
    z = p.z
    if z > 1.45:
        return {"Chest": 1.0}
    if z > 1.25:
        t = (z - 1.25) / 0.2
        return {"Spine": 1 - t, "Chest": t}
    if z > 1.05:
        t = (z - 1.05) / 0.2
        return {"Hips": 1 - t, "Spine": t}
    return {"Hips": 1.0}


# ---------------------------------------------------------------- robe
# Heavy and broad, a little bell at the hem; it falls straight (he walks), slightly back at the bottom.
# (z, half width, half depth, centre y)
ROBE = [(1.70, 0.20, 0.15, -0.07), (1.60, 0.27, 0.18, -0.05), (1.45, 0.28, 0.19, -0.03), (1.28, 0.26, 0.18, -0.01),
        (1.12, 0.25, 0.18, 0.0), (0.95, 0.28, 0.20, 0.01), (0.75, 0.31, 0.22, 0.02), (0.55, 0.34, 0.24, 0.03),
        (0.35, 0.36, 0.26, 0.04)]


def robe_hem(i, k, p, c):
    if i != len(ROBE) - 1:
        return V(0, 0, 0)
    return V(0, 0, -0.06 * ((k * 5) % 3) / 2.0)


loft([(V(0, cy, z), rx, ry) for z, rx, ry, cy in ROBE], zone, n=28, cap0=False, cap1=False, disp=robe_hem)
# folds: a few heavy vertical pleats down the front and the back
for fx in (-0.12, 0.0, 0.12):
    for fy, sgn in ((-0.20, 1), (0.21, -1)):
        loft([(V(fx, fy * (1.0 + 0.15 * (1.1 - z)), z), 0.018, 0.02) for z in (1.05, 0.8, 0.55, 0.38)], zone, n=8)

# The torn hem: strips of the robe down to the ground, swinging as he walks.
n_strip = 0
for k in range(10):
    ang = math.radians(-180 + k * 36 + (8 if k % 2 else 0))
    top = V(0.33 * math.cos(ang), 0.04 + 0.24 * math.sin(ang), 0.38)
    length = 0.28 + 0.1 * ((k * 3) % 4) / 3.0
    bottom = top + V(0.03 * math.cos(ang), 0.04, -length)
    chain_ribbon("Tatter.", n_strip, [top.lerp(bottom, j / 3.0) for j in range(4)], 0.11, 0.012, "Hips")
    n_strip += 1

# ---------------------------------------------------------------- mantle (a short cape over the shoulders)
MANTLE = [(1.76, 0.17, 0.13, -0.08), (1.70, 0.30, 0.19, -0.06), (1.58, 0.36, 0.22, -0.04), (1.44, 0.37, 0.23, -0.02)]
loft([(V(0, cy, z), rx, ry) for z, rx, ry, cy in MANTLE], W("Chest"), n=28, cap0=False, cap1=False,
     disp=lambda i, k, p, c: V(0, 0, -0.05 * ((k * 7) % 3) / 2.0) if i == 3 else V(0, 0, 0))
for sx in (1, -1):  # its torn tails behind
    top = V(0.18 * sx, 0.2, 1.46)
    chain_ribbon("Tatter.", n_strip, [top.lerp(top + V(0.04 * sx, 0.1, -0.62), j / 3.0) for j in range(4)], 0.13, 0.012, "Chest")
    n_strip += 1

# ---------------------------------------------------------------- belt: a rope, its ends hanging
loft([(V(0.31 * math.cos(a), 0.0 + 0.2 * math.sin(a), 1.12), 0.022, 0.022) for a in [2 * math.pi * k / 24 for k in range(25)]],
     W("Hips"), mat=EYES, n=8, cap0=False, cap1=False)
for dx in (0.06, 0.12):
    top = V(0.18 + dx * 0.4, -0.2, 1.1)
    chain_ribbon("Tatter.", n_strip, [top.lerp(top + V(dx * 0.3, -0.03, -0.48), j / 3.0) for j in range(4)], 0.035, 0.02,
                 "Hips", mat=EYES, taper=0.2, point=0.8)
    n_strip += 1

# ---------------------------------------------------------------- hood
loft([(bone_head("Neck").lerp(bone_tail("Neck"), u), 0.06, 0.06) for u in (0.0, 1.0)], W("Neck"), n=10)
HOOD = [(1.70, 0.22, 0.17, -0.08), (1.78, 0.19, 0.17, -0.11), (1.86, 0.17, 0.18, -0.15), (1.94, 0.165, 0.185, -0.18),
        (2.02, 0.155, 0.175, -0.19), (2.09, 0.13, 0.15, -0.18), (2.14, 0.09, 0.11, -0.16), (2.17, 0.035, 0.04, -0.14)]


def hood_w(p, s):
    if p.z < 1.76:
        return {"Chest": 0.6, "Neck": 0.4}
    if p.z < 1.84:
        return {"Neck": 0.5, "Head": 0.5}
    return {"Head": 1.0}


def hood_brim(i, k, p, c):
    th = 2 * math.pi * k / 24
    front = max(0.0, -math.sin(th)) ** 2
    return V(0, -0.07 * front * math.exp(-((i - 5.0) / 1.2) ** 2) + 0.05 * front * math.exp(-((i - 3.0) / 0.9) ** 2),
             -0.02 * front * math.exp(-((i - 5.0) / 1.2) ** 2))


_h0 = len(verts)
loft([(V(0, cy, z), rx, ry) for z, rx, ry, cy in HOOD], hood_w, n=24, disp=hood_brim, cap0=False)
_hood = verts[_h0:]
for sx in (1, -1):
    near = [v.y for v in _hood if abs(v.x - 0.055 * sx) < 0.035 and abs(v.z - 1.93) < 0.045]
    blob(V(0.055 * sx, min(near) - 0.004, 1.93), 0.022, W("Head"), mat=EYES, seg=10, rings=8, scale=(1.2, 0.6, 0.75))

# ---------------------------------------------------------------- arms in wide sleeves, heavy hands
for side in ("L", "R"):
    ua, la, ha = "UpperArm." + side, "LowerArm." + side, "Hand." + side
    a, b = bone_head(ua), bone_tail(ua)
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.085), (0.5, 0.08), (1.0, 0.075))], along(a, b, ua, la, 0.8), n=14)
    a, b = bone_head(la), bone_tail(la)
    loft([(a.lerp(b, u), r, r) for u, r in ((0.0, 0.075), (0.6, 0.09), (1.0, 0.115))], along(a, b, la, ha, 0.9), n=16, cap1=False,
         disp=lambda i, k, p, c: V(0, 0, -0.04 * ((k * 5) % 3) / 2.0) if i == 2 else V(0, 0, 0))
    a, b = bone_head(ha), bone_tail(ha)
    blob(a.lerp(b, 0.55), 0.05, W(ha), seg=12, rings=8, scale=(0.7, 1.0, 1.0))      # a big gloved fist

# ---------------------------------------------------------------- rosary from the right hand
hr = bone_tail("Hand.R")
pts = [hr + V(0, -0.01 * j, -0.11 * j) for j in range(6)]
chain_ribbon("Tatter.", n_strip, pts, 0.008, 0.008, "Hand.R", mat=EYES, taper=0.0, point=1.0)
for j in range(1, 6):
    blob(pts[j], 0.022 if j < 5 else 0.035, (lambda p, s, j=j: {"Tatter.%d.%d" % (n_strip, min(j - 1, 4)): 1.0}), mat=EYES, seg=8, rings=6)
n_strip += 1

# ---------------------------------------------------------------- finish the body, then the shield as its own object
res = finish("ShieldMonk", [("MonkInk", (0.07, 0.07, 0.08, 1)), ("MonkCrimson", (0.9, 0.02, 0.04, 1))],
             "ShieldMonk.fbx", "ShieldMonk.blend", export=False)
rig = bpy.data.objects["ShieldMonkRig"]

verts.clear(); faces.clear(); fmat.clear(); vweights.clear()
# Kite shield, front facing -y: outline in (x, z) about its centre, bulging forward, a raised rim and a boss, a
# crimson eye weeping down its face.
# simpler: build the kite as a lofted profile across x
ROWS = []
for k in range(15):
    z = 0.42 - k * 0.07          # from the top down to the point
    half = 0.28 * (1.0 if z > 0.1 else max(0.02, (z + 0.56) / 0.66)) * (0.92 + 0.08 * math.sqrt(max(0.0, 1 - ((z - 0.25) / 0.2) ** 2)) if z > 0.25 else 1.0)
    ROWS.append((z, half))
w0 = lambda p, s: {"Shield": 1.0}
for k in range(len(ROWS) - 1):
    for j in range(12):
        def P(row, jj, front):
            z, half = ROWS[row]
            x = -half + 2 * half * jj / 12.0
            bulge = 0.05 * (1 - (x / max(half, 1e-3)) ** 2)
            return V(x, -(0.03 + bulge) if front else 0.03, z)
        for front in (True, False):
            a, b, c, d = P(k, j, front), P(k, j + 1, front), P(k + 1, j + 1, front), P(k + 1, j, front)
            ia, ib, ic, id_ = [add_vert(q, w0(q, 0)) for q in (a, b, c, d)]
            faces.append((ia, ib, ic, id_) if front else (id_, ic, ib, ia)); fmat.append(INK)
# rim: a tube along the outline
rim = [V(-h, -0.04, z) for z, h in ROWS] + [V(h, -0.04, z) for z, h in reversed(ROWS)]
loft([(p, 0.018, 0.018) for p in rim], w0, n=8)
# boss and the crimson eye with drips
blob(V(0, -0.1, 0.2), 0.06, w0, seg=12, rings=8, scale=(1, 0.6, 1))
loft([(V(0.07 * math.cos(a), -0.105, 0.2 + 0.07 * math.sin(a)), 0.012, 0.012) for a in [2 * math.pi * k / 20 for k in range(21)]], w0, mat=EYES, n=6, cap0=False, cap1=False)
blob(V(0, -0.15, 0.2), 0.028, w0, mat=EYES, seg=10, rings=6, scale=(1, 0.5, 1))
for dx, ln in ((-0.03, 0.32), (0.0, 0.48), (0.035, 0.26)):
    loft([(V(dx, -0.1 + 0.04 * (u * u), 0.13 - ln * u), 0.012 * (1 - 0.6 * u), 0.008) for u in (0.0, 0.3, 0.7, 1.0)], w0, mat=EYES, n=6)
sm = bpy.data.meshes.new("Shield")
sm.from_pydata([tuple(v) for v in verts], [], faces)
sm.update(); sm.shade_smooth()
for mname in ("MonkInk", "MonkCrimson"):
    sm.materials.append(bpy.data.materials[mname])
for poly, m in zip(sm.polygons, fmat):
    poly.material_index = m
shield = bpy.data.objects.new("Shield", sm)
bpy.context.scene.collection.objects.link(shield)
shield.parent = rig
shield.location = (-0.36, -0.30, 1.1)  # held out towards the camera, in front of the body
shield.rotation_euler = (0, 0, math.radians(-90))  # its face to the camera side (as the game shows him)

bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\ArtSource\ShieldMonk.blend")
if globals().get("EXPORT", True):
    ob = bpy.data.objects["ShieldMonk"]
    for o in bpy.context.scene.objects:
        o.select_set(o in (rig, ob, shield))
    win = bpy.context.window_manager.windows[0]
    with bpy.context.temp_override(window=win, screen=win.screen, active_object=rig, selected_objects=[rig, ob, shield]):
        bpy.ops.export_scene.fbx(filepath=ROOT + r"\Assets\_CrimsonVeil\Art\Enemy\ShieldMonk.fbx", use_selection=True,
                                 object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, primary_bone_axis='Y',
                                 secondary_bone_axis='X', axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                                 bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
result = res
