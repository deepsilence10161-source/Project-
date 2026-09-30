"""Capability test: VRoid girl talks, blinks, smiles. Env: VRM, OUT, RES_X, RES_Y, FRAMES, SAMPLES"""
import bpy, math, os
from mathutils import Vector, Matrix
E = os.environ
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.preferences.addon_enable(module="bl_ext.user_default.vrm")
bpy.ops.import_scene.vrm(filepath=E["VRM"])
sc = bpy.context.scene
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
pb = arm.pose.bones
def world_rot(name, axis, deg):
    b = pb[name]; R = Matrix.Rotation(math.radians(deg), 4, axis)
    m = arm.matrix_world @ b.matrix; loc = m.to_translation()
    b.matrix = arm.matrix_world.inverted() @ (Matrix.Translation(loc) @ R @ Matrix.Translation(-loc) @ m)
    bpy.context.view_layer.update()
world_rot('J_Bip_L_UpperArm', 'Y', 70); world_rot('J_Bip_R_UpperArm', 'Y', -70)
kb = bpy.data.objects['Face'].data.shape_keys.key_blocks
N = int(E.get("FRAMES", 48)); fps = 24
sc.frame_start, sc.frame_end = 1, N; sc.render.fps = fps
hb = pb['J_Bip_C_Head']; hb.rotation_mode = 'XYZ'
sp = pb['J_Bip_C_Spine']; sp.rotation_mode = 'XYZ'
for f in range(1, N + 1):
    t = (f - 1) / fps
    hb.rotation_euler = (0, math.sin(t * 2.0) * 0.12, math.sin(t * 1.3) * 0.08); hb.keyframe_insert('rotation_euler', frame=f)
    sp.rotation_euler = (0, 0, math.sin(t * 1.1) * 0.05); sp.keyframe_insert('rotation_euler', frame=f)
    talk = f < N * 0.7
    kb['Fcl_MTH_A'].value = abs(math.sin(t * 9)) * 0.7 if talk else 0.0; kb['Fcl_MTH_A'].keyframe_insert('value', frame=f)
    kb['Fcl_EYE_Close'].value = 1.0 if (f % 60) in (28, 29, 30) else 0.0; kb['Fcl_EYE_Close'].keyframe_insert('value', frame=f)
    kb['Fcl_ALL_Joy'].value = 0.9 if not talk else 0.0; kb['Fcl_ALL_Joy'].keyframe_insert('value', frame=f)
head = arm.matrix_world @ pb['J_Bip_C_Head'].head
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam)
tgt = Vector((0, 0, head.z - 0.12)); cam.location = Vector((0, -1.35, tgt.z + 0.12))
cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler(); sc.camera = cam
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN')); sun.data.energy = 3
sun.rotation_euler = (math.radians(50), 0, math.radians(20)); sc.collection.objects.link(sun)
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes['Background'].inputs[0].default_value = (0.05, 0.07, 0.18, 1)
sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = int(E.get("SAMPLES", 16)); sc.cycles.use_denoising = False
sc.render.resolution_x, sc.render.resolution_y = int(E["RES_X"]), int(E["RES_Y"])
sc.render.filepath = E["OUT"] + "/f_"
bpy.ops.render.render(animation=True)
