# Builds the hanged man (HangedMan.blend -> Assets/_CrimsonVeil/Art/Enemy/HangedMan.fbx): a gaunt corpse in a torn
# shirt and ragged breeches, barefoot, an iron shackle with a broken chain at one ankle, a crimson noose round the
# neck, the head fallen to one side. Built standing, feet at the origin, the neck at 1.7 m (the game hangs him there).
# Limbs and head are loose bone chains (Tatter.<n>.<k>, SpringBones): they trail the swing and flop when he falls.
#   exec(open(r"C:\Dev\CrimsonVeil\ArtSource\hanged_build.py").read())
# Blender space: Z up, faces -Y, his left is +X.
exec(open(r"C:\Dev\CrimsonVeil\ArtSource\rig_kit.py").read())

bone("Hips", (0, 0.0, 0.92), (0, 0.0, 1.12), None)
bone("Spine", (0, 0.0, 1.12), (0, 0.01, 1.34), "Hips")
bone("Chest", (0, 0.01, 1.34), (0, 0.02, 1.58), "Spine")
# loose chains: 0 / 1 the arms (upper arm, forearm, hand), 2 / 3 the legs (thigh, shin, foot), 4 the neck and head
ARM = {"L": [V(0.19, 0.02, 1.53), V(0.22, 0.03, 1.24), V(0.23, 0.0, 0.98), V(0.235, -0.01, 0.84)]}
ARM["R"] = [V(-p.x, p.y, p.z) for p in ARM["L"]]
LEG = {"L": [V(0.09, 0.0, 0.93), V(0.1, 0.0, 0.5), V(0.105, 0.02, 0.09), V(0.11, -0.12, 0.03)]}
LEG["R"] = [V(-0.09, 0.0, 0.93), V(-0.085, 0.01, 0.51), V(-0.08, 0.04, 0.1), V(-0.08, -0.1, 0.02)]
HEAD = [V(0, 0.02, 1.58), V(0.03, 0.0, 1.70), V(0.10, -0.03, 1.86)]   # the neck broken, the head fallen to his left
for n, pts in ((0, ARM["L"]), (1, ARM["R"]), (2, LEG["L"]), (3, LEG["R"])):
    root = "Chest" if n < 2 else "Hips"
    parent = root
    for k in range(3):
        bone("Tatter.%d.%d" % (n, k), tuple(pts[k]), tuple(pts[k + 1]), parent)
        parent = "Tatter.%d.%d" % (n, k)
bone("Tatter.4.0", tuple(HEAD[0]), tuple(HEAD[1]), "Chest")
bone("Tatter.4.1", tuple(HEAD[1]), tuple(HEAD[2]), "Tatter.4.0")


def zone(p, s):
    z = p.z
    if z > 1.36:
        return {"Chest": 1.0}
    if z > 1.18:
        t = (z - 1.18) / 0.18
        return {"Spine": 1 - t, "Chest": t}
    if z > 1.0:
        t = (z - 1.0) / 0.18
        return {"Hips": 1 - t, "Spine": t}
    return {"Hips": 1.0}


# ---------------------------------------------------------------- torso: emaciated, ribs under a torn shirt
TORSO = [(0.90, 0.15, 0.10), (1.00, 0.14, 0.09), (1.12, 0.12, 0.08), (1.24, 0.13, 0.085), (1.36, 0.15, 0.095),
         (1.46, 0.16, 0.095), (1.52, 0.15, 0.085), (1.57, 0.10, 0.065), (1.61, 0.05, 0.045)]  # sloping, bony shoulders
loft([(V(0, 0.01, z), rx, ry) for z, rx, ry in TORSO], zone, n=20,
     disp=lambda i, k, p, c: (p - c).normalized() * (0.008 * (0.5 + 0.5 * math.cos(p.z * 2 * math.pi / 0.055))) if 3 <= i <= 5 else V(0, 0, 0))
# the shirt: hanging loose off the shoulders, torn, its tails ragged strips
SHIRT = [(1.60, 0.07, 0.06), (1.56, 0.15, 0.10), (1.50, 0.19, 0.115), (1.40, 0.185, 0.12), (1.26, 0.17, 0.115),
         (1.12, 0.165, 0.115), (0.98, 0.18, 0.12)]
loft([(V(0, 0.01, z), rx, ry) for z, rx, ry in SHIRT], zone, n=22, cap0=False, cap1=False,
     disp=lambda i, k, p, c: V(0, 0, -0.06 * ((k * 7) % 3) / 2.0) if i == 6 else V(0, 0, 0))
n_strip = 5
for k in range(6):
    ang = math.radians(-160 + k * 60)
    top = V(0.17 * math.cos(ang), 0.01 + 0.11 * math.sin(ang), 0.98)
    chain_ribbon("Tatter.", n_strip, [top.lerp(top + V(0.01, 0.02, -0.22 - 0.05 * (k % 3)), j / 2.0) for j in range(3)],
                 0.07, 0.01, "Hips")
    n_strip += 1
# crimson noose round the neck, its knot behind the ear
loft([(V(0.06 * math.cos(a), 0.015 + 0.055 * math.sin(a), 1.66 + 0.02 * math.cos(a)), 0.016, 0.016)
      for a in [2 * math.pi * k / 20 for k in range(21)]], W("Tatter.4.0"), mat=EYES, n=8, cap0=False, cap1=False)
blob(V(-0.06, 0.03, 1.69), 0.03, W("Tatter.4.0"), mat=EYES, seg=10, rings=8)


def seg_w(n, k):
    return lambda p, s: {"Tatter.%d.%d" % (n, k): 1.0}


def joint_w(n, k):
    return lambda p, s: {"Tatter.%d.%d" % (n, k): 0.5, "Tatter.%d.%d" % (n, k + 1): 0.5}


# ---------------------------------------------------------------- neck and head (fallen to one side)
loft([(HEAD[0].lerp(HEAD[1], u), 0.038, 0.038) for u in (0.0, 1.0)], seg_w(4, 0), n=10)
hc = HEAD[1].lerp(HEAD[2], 0.6)
blob(hc, 0.1, seg_w(4, 1), seg=16, rings=12, scale=(0.82, 0.95, 1.12))
blob(hc + V(0.0, -0.06, -0.06), 0.05, seg_w(4, 1), seg=10, rings=8, scale=(1.0, 1.0, 0.8))   # the slack jaw
for k in range(9):   # lank hair hanging down off the skull
    a = math.radians(-60 + k * 30)
    top = hc + V(0.08 * math.cos(a), 0.06 * math.sin(a) + 0.03, 0.06)
    loft([(top.lerp(top + V(0.03 * math.cos(a), 0.03, -0.17), u), 0.012 * (1 - 0.7 * u), 0.012 * (1 - 0.7 * u)) for u in (0.0, 0.5, 1.0)],
         seg_w(4, 1), n=6)

# ---------------------------------------------------------------- arms (thin, the sleeves torn off at the elbow)
for n, side in ((0, "L"), (1, "R")):
    p = ARM[side]
    loft([(p[0].lerp(p[1], u), r, r) for u, r in ((0.0, 0.062), (0.5, 0.055), (1.0, 0.05))], seg_w(n, 0), n=12,
         disp=lambda i, k, pp, c: V(0, 0, -0.03 * ((k * 5) % 3) / 2.0) if i == 2 else V(0, 0, 0), cap1=False)   # torn sleeve
    loft([(p[0].lerp(p[1], u), r, r) for u, r in ((0.0, 0.04), (1.0, 0.032))], seg_w(n, 0), n=10)
    blob(p[1], 0.034, joint_w(n, 0), seg=10, rings=8)
    loft([(p[1].lerp(p[2], u), r, r) for u, r in ((0.0, 0.03), (0.6, 0.026), (1.0, 0.022))], seg_w(n, 1), n=10)
    # a limp hand, fingers hanging
    loft([(p[2].lerp(p[3], u), 0.018, 0.035) for u in (0.0, 0.5, 1.0)], seg_w(n, 2), n=10)
    for f in range(4):
        b = p[3] + V(0, -0.025 + 0.017 * f, 0)
        loft([(b, 0.008, 0.008), (b + V(0, 0.005, -0.07), 0.006, 0.006)], seg_w(n, 2), n=6)

# ---------------------------------------------------------------- legs in ragged breeches, bare feet
for n, side in ((2, "L"), (3, "R")):
    p = LEG[side]
    loft([(p[0].lerp(p[1], u), r, r) for u, r in ((0.0, 0.09), (0.5, 0.075), (1.0, 0.07))], seg_w(n, 0), n=14)
    blob(p[1], 0.06, joint_w(n, 0), seg=10, rings=8)
    loft([(p[1].lerp(p[2], u), r, r) for u, r in ((0.0, 0.075), (0.25, 0.07), (0.35, 0.05), (1.0, 0.035))], seg_w(n, 1), n=12,
         disp=lambda i, k, pp, c: V(0, 0, -0.05 * ((k * 3) % 3) / 2.0) if i == 1 else V(0, 0, 0))   # torn below the knee
    loft([(p[2], 0.032, 0.03), (p[2].lerp(p[3], 0.5), 0.035, 0.03), (p[3], 0.03, 0.022)], seg_w(n, 2), n=10)
# a shackle round the left ankle, a broken chain hanging from it
a0 = LEG["L"][2] + V(0, 0, 0.06)
loft([(a0 + V(0.05 * math.cos(a), 0.05 * math.sin(a), 0), 0.012, 0.012) for a in [2 * math.pi * k / 16 for k in range(17)]],
     seg_w(2, 1), n=6, cap0=False, cap1=False)
for j in range(4):
    c = a0 + V(0.05, 0, -0.07 * (j + 1))
    loft([(c + V(0.025 * math.cos(a), 0.012 * math.sin(a), 0.035 * math.sin(a)), 0.007, 0.007) for a in [2 * math.pi * k / 12 for k in range(13)]],
         seg_w(2, 2), n=6, cap0=False, cap1=False)

result = finish("HangedMan", [("HangedInk", (0.07, 0.07, 0.08, 1)), ("HangedCrimson", (0.9, 0.02, 0.04, 1))],
                "HangedMan.fbx", "HangedMan.blend", export=globals().get("EXPORT", True))
