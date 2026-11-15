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
# Built as a sculpted loft: each section's half width / depth vary round the ring (a narrow waist, a sunken belly, the
# ribcage flaring, the collarbones and the shoulder blades standing out).
RING = 32


def torso_disp(i, k, p, c):
    th = 2 * math.pi * k / RING
    fy = -math.sin(th)                       # +1 front
    z = p.z
    d = V(0, 0, 0)
    # ribs: ridges round the front and sides of the ribcage
    if 1.18 < z < 1.48:
        rib = 0.5 + 0.5 * math.cos((z - 1.18) * 2 * math.pi / 0.05)
        d = d + (p - c).normalized() * (0.008 * rib * max(0.0, 0.3 + fy))
    # the sunken belly under the ribs
    d = d + V(0, 0.035 * math.exp(-((z - 1.08) / 0.06) ** 2) * max(0.0, fy) ** 2, 0)
    # the breastbone groove and collarbones
    d = d + V(0, 0.01 * math.exp(-((z - 1.52) / 0.03) ** 2) * max(0.0, fy), 0)
    # shoulder blades jutting at the back
    d = d + V(0, 0.02 * math.exp(-((z - 1.45) / 0.06) ** 2) * max(0.0, -fy) * abs(math.cos(th)) * 2, 0)
    return d


TORSO = [(0.90, 0.145, 0.10), (0.97, 0.14, 0.095), (1.04, 0.125, 0.085), (1.10, 0.12, 0.08), (1.18, 0.13, 0.09),
         (1.26, 0.14, 0.1), (1.34, 0.15, 0.105), (1.42, 0.16, 0.105), (1.48, 0.165, 0.1), (1.53, 0.16, 0.09),
         (1.57, 0.12, 0.07), (1.60, 0.07, 0.05), (1.62, 0.045, 0.04)]
loft([(V(0, 0.01, z), rx, ry) for z, rx, ry in TORSO], zone, n=RING, disp=torso_disp)
# the shirt: hanging loose off one shoulder, torn open at the front, its folds falling to ragged tails
SHIRT = [(1.60, 0.07, 0.06), (1.56, 0.15, 0.1), (1.50, 0.178, 0.112), (1.40, 0.172, 0.115), (1.26, 0.158, 0.112),
         (1.12, 0.148, 0.105), (0.98, 0.168, 0.118)]
_shirt_folds = folds(7, 0.022, 1, RING, len(SHIRT), 0.4)


def shirt_disp(i, k, p, c):
    d = _shirt_folds(i, k, p, c)
    th = 2 * math.pi * k / RING
    if i == len(SHIRT) - 1:
        d = d + V(0, 0, -0.06 * ((k * 7) % 3) / 2.0 - 0.02 * math.sin(k))
    # torn open down the front: the edges of the rip pulled apart, showing the ribs
    rip = max(0.0, -math.sin(th)) ** 6 * (1.0 if 1 <= i <= 4 else 0.0)
    return d + V(0, 0.08 * rip, 0)


loft([(V(0, 0.01, z), rx, ry) for z, rx, ry in SHIRT], zone, n=RING, cap0=False, cap1=False, disp=shirt_disp)
n_strip = 5
for k in range(6):
    ang = math.radians(-160 + k * 60)
    top = V(0.18 * math.cos(ang), 0.01 + 0.12 * math.sin(ang), 0.98)
    chain_ribbon("Tatter.", n_strip, [top.lerp(top + V(0.01, 0.02, -0.22 - 0.05 * (k % 3)), j / 2.0) for j in range(3)],
                 0.07, 0.01, "Hips", along=(math.sin(ang), math.cos(ang)))
    n_strip += 1
# crimson noose round the neck, a thick coil of rope with its hangman's knot behind the ear
for strand in range(2):
    loft([(V(0.062 * math.cos(a) + 0.006 * strand, 0.015 + 0.057 * math.sin(a), 1.65 + 0.022 * math.cos(a) + 0.018 * strand), 0.013, 0.013)
          for a in [2 * math.pi * k / 24 for k in range(25)]], W("Tatter.4.0"), mat=EYES, n=8, cap0=False, cap1=False)
kn = V(-0.06, 0.035, 1.70)
for j in range(5):
    loft([(kn + V(0.03 * math.cos(a), 0.03 * math.sin(a), 0.016 * j), 0.012, 0.012) for a in [2 * math.pi * q / 12 for q in range(13)]],
         W("Tatter.4.0"), mat=EYES, n=6, cap0=False, cap1=False)


def seg_w(n, k):
    return lambda p, s: {"Tatter.%d.%d" % (n, k): 1.0}


def joint_w(n, k):
    return lambda p, s: {"Tatter.%d.%d" % (n, k): 0.5, "Tatter.%d.%d" % (n, k + 1): 0.5}


def limb(pts, radii, w, n=14, cap0=True, cap1=True, disp=None):
    """A limb section by section: (u along pts[0]->pts[1], radius x, radius y)."""
    loft([(pts[0].lerp(pts[1], u), rx, ry) for u, rx, ry in radii], w, n=n, cap0=cap0, cap1=cap1, disp=disp)


# ---------------------------------------------------------------- neck and head (fallen to one side), a gaunt face
limb((HEAD[0], HEAD[1]), [(0.0, 0.045, 0.042), (0.5, 0.036, 0.038), (1.0, 0.04, 0.04)], seg_w(4, 0), n=12)
hc = HEAD[1].lerp(HEAD[2], 0.6)
hd = (HEAD[2] - HEAD[1]).normalized()        # the skull's up
fw = V(0, -1, 0)                             # its front


def skull_disp(i, k, p, c):
    return V(0, 0, 0)


blob(hc, 0.1, seg_w(4, 1), seg=20, rings=16, scale=(0.8, 0.95, 1.1))                  # the cranium
blob(hc + fw * 0.06 - hd * 0.03, 0.06, seg_w(4, 1), seg=14, rings=10, scale=(1.05, 0.8, 0.9))   # the face
blob(hc + fw * 0.085 + hd * 0.01, 0.045, seg_w(4, 1), seg=12, rings=8, scale=(1.4, 0.5, 0.45))  # the brow ridge
for sx in (1, -1):
    blob(hc + fw * 0.07 + V(0.05 * sx, 0, 0) - hd * 0.05, 0.03, seg_w(4, 1), seg=10, rings=8, scale=(0.8, 0.8, 1.0))  # cheekbones
blob(hc + fw * 0.11 - hd * 0.03, 0.018, seg_w(4, 1), seg=10, rings=8, scale=(0.8, 1.2, 1.6))   # the nose
# the slack jaw hanging open, the tongue's crimson in the gap
blob(hc + fw * 0.055 - hd * 0.12, 0.05, seg_w(4, 1), seg=14, rings=10, scale=(1.0, 0.95, 0.65))
blob(hc + fw * 0.085 - hd * 0.09, 0.02, seg_w(4, 1), mat=EYES, seg=8, rings=6, scale=(1.4, 0.8, 0.5))
for sx in (1, -1):
    blob(hc + V(0.085 * sx, 0, 0) - hd * 0.02, 0.022, seg_w(4, 1), seg=8, rings=6, scale=(0.5, 1.0, 1.4))   # ears
for k in range(13):   # lank hair hanging down off the skull, in clumps
    a = math.radians(-80 + k * 22)
    top = hc + V(0.085 * math.cos(a), 0.065 * math.sin(a) + 0.035, 0.06)
    L = 0.17 + 0.05 * ((k * 3) % 3)
    loft([(top.lerp(top + V(0.035 * math.cos(a), 0.03, -L), u), 0.014 * (1 - 0.8 * u), 0.008 * (1 - 0.8 * u)) for u in (0.0, 0.35, 0.7, 1.0)],
         seg_w(4, 1), n=6)

# ---------------------------------------------------------------- arms (wasted, the sleeves torn off at the elbow)
for n, side in ((0, "L"), (1, "R")):
    p = ARM[side]
    limb((p[0], p[1]), [(0.0, 0.058, 0.058), (0.5, 0.052, 0.052), (1.0, 0.05, 0.05)], seg_w(n, 0), n=20, cap1=False,
         disp=lambda i, k, pp, c: (pp - c) * (0.12 * math.sin(k * 5 * math.pi / 20 + i)) + (V(0, 0, -0.035 * ((k * 5) % 3) / 2.0) if i == 2 else V(0, 0, 0)))
    limb((p[0], p[1]), [(0.0, 0.045, 0.045), (0.3, 0.04, 0.042), (0.75, 0.033, 0.035), (1.0, 0.03, 0.03)], seg_w(n, 0), n=12)
    blob(p[1], 0.033, joint_w(n, 0), seg=10, rings=8, scale=(1.0, 1.1, 1.0))   # a knobbly elbow
    limb((p[1], p[2]), [(0.0, 0.03, 0.03), (0.25, 0.034, 0.03), (0.7, 0.026, 0.022), (1.0, 0.021, 0.018)], seg_w(n, 1), n=12)
    # a limp hand: the palm, four long fingers curling a little, the thumb
    d = (p[3] - p[2]).normalized()
    limb((p[2], p[3]), [(0.0, 0.02, 0.03), (0.5, 0.017, 0.038), (1.0, 0.015, 0.035)], seg_w(n, 2), n=10)
    for f in range(4):
        b0 = p[3] + V(0, -0.027 + 0.018 * f, 0)
        L = 0.075 - 0.008 * abs(f - 1.5)
        pts = [b0, b0 + d * L * 0.45 + V(0, -0.004, 0), b0 + d * L * 0.8 + V(0, -0.012, 0), b0 + d * L + V(0, -0.024, 0)]
        loft([(q, 0.0075 - 0.0012 * j, 0.0075 - 0.0012 * j) for j, q in enumerate(pts)], seg_w(n, 2), n=6)
    tb = p[2].lerp(p[3], 0.6) + V(0, -0.03, 0)
    loft([(tb, 0.008, 0.008), (tb + V(0, -0.025, -0.03), 0.007, 0.007), (tb + V(0, -0.035, -0.055), 0.005, 0.005)], seg_w(n, 2), n=6)

# ---------------------------------------------------------------- legs in ragged breeches, bare bony feet
for n, side in ((2, "L"), (3, "R")):
    p = LEG[side]
    limb((p[0], p[1]), [(0.0, 0.1, 0.095), (0.3, 0.092, 0.09), (0.7, 0.08, 0.08), (1.0, 0.075, 0.075)], seg_w(n, 0), n=20,
         disp=lambda i, k, pp, c: (pp - c) * (0.1 * math.sin(k * 5 * math.pi / 20 + 2 * i)))
    blob(p[1], 0.062, joint_w(n, 0), seg=12, rings=8)
    limb((p[1], p[2]), [(0.0, 0.08, 0.08), (0.25, 0.076, 0.076), (0.36, 0.07, 0.07)], seg_w(n, 1), n=20, cap1=False,
         disp=lambda i, k, pp, c: (pp - c) * (0.1 * math.sin(k * 5 * math.pi / 20 + i)) + (V(0, 0, -0.06 * ((k * 3) % 4) / 3.0) if i == 2 else V(0, 0, 0)))
    # the bare shin: the calf behind, the shinbone sharp in front, the ankle bones
    limb((p[1], p[2]), [(0.2, 0.045, 0.05), (0.45, 0.042, 0.055), (0.75, 0.032, 0.035), (1.0, 0.028, 0.028)], seg_w(n, 1), n=12,
         disp=lambda i, k, pp, c: V(0, 0.012 * max(0.0, math.sin(2 * math.pi * k / 12)) * (1 if 1 <= i <= 2 else 0), 0))
    for sx in (1, -1):
        blob(p[2] + V(0.026 * sx, 0, 0.005), 0.014, seg_w(n, 1), seg=8, rings=6)
    # the foot: heel, arch, the toes splayed and hanging
    d = (p[3] - p[2]).normalized()
    limb((p[2], p[3]), [(0.0, 0.033, 0.035), (0.5, 0.038, 0.025), (1.0, 0.04, 0.018)], seg_w(n, 2), n=12)
    blob(p[2] + V(0, 0.03, -0.02), 0.03, seg_w(n, 2), seg=8, rings=6)          # heel
    for t in range(5):
        b0 = p[3] + V(-0.026 + 0.013 * t, 0, 0)
        L = 0.04 - 0.005 * t if t else 0.045
        loft([(b0, 0.009, 0.008), (b0 + d * L * 0.6 + V(0, 0, -0.006), 0.008, 0.007), (b0 + d * L + V(0, 0, -0.016), 0.006, 0.005)], seg_w(n, 2), n=6)
# a shackle round the left ankle, a broken chain hanging from it
a0 = LEG["L"][2] + V(0, 0, 0.06)
loft([(a0 + V(0.05 * math.cos(a), 0.05 * math.sin(a), 0), 0.013, 0.016) for a in [2 * math.pi * k / 20 for k in range(21)]],
     seg_w(2, 1), n=8, cap0=False, cap1=False)
for j in range(5):
    c = a0 + V(0.05, 0, -0.065 * (j + 1))
    rot = j % 2
    loft([(c + (V(0.025 * math.cos(a), 0.0, 0.035 * math.sin(a)) if rot else V(0.0, 0.025 * math.cos(a), 0.035 * math.sin(a))), 0.0075, 0.0075)
          for a in [2 * math.pi * k / 14 for k in range(15)]], seg_w(2, 2), n=6, cap0=False, cap1=False)

result = finish("HangedMan", [("HangedInk", (0.07, 0.07, 0.08, 1)), ("HangedCrimson", (0.9, 0.02, 0.04, 1))],
                "HangedMan.fbx", "HangedMan.blend", export=globals().get("EXPORT", True))
