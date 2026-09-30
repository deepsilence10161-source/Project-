"""Two-shot still: heroine + boy at sunset. Env: GIRL, BOY, OUT, RES_X, RES_Y, SAMPLES"""
import bpy, math, os
from mathutils import Vector, Matrix
E = os.environ
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.preferences.addon_enable(module="bl_ext.user_default.vrm")
sc = bpy.context.scene

def load(path, x, yaw_deg):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.vrm(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    arm = [o for o in new if o.type == 'ARMATURE'][0]
    face = [o for o in new if o.type == 'MESH' and o.data.shape_keys and 'Fcl_ALL_Joy' in o.data.shape_keys.key_blocks][0]
    root = arm
    while root.parent: root = root.parent
    root.rotation_mode = 'XYZ'; root.location.x = x; root.rotation_euler.z = math.radians(yaw_deg)
    print('ROOT', root.name, root.type)
    bpy.context.view_layer.update()
    return arm, face

def world_rot(arm, name, axis, deg):
    b = arm.pose.bones[name]; R = Matrix.Rotation(math.radians(deg), 4, axis)
    m = arm.matrix_world @ b.matrix; loc = m.to_translation()
    b.matrix = arm.matrix_world.inverted() @ (Matrix.Translation(loc) @ R @ Matrix.Translation(-loc) @ m)
    bpy.context.view_layer.update()

def arms_down(arm):
    # rotate about the character's own forward axis
    fwd = (arm.matrix_world.to_3x3() @ Vector((0, 1, 0))).normalized()
    for n, d in (('J_Bip_L_UpperArm', 70), ('J_Bip_R_UpperArm', -70)):
        b = arm.pose.bones[n]; R = Matrix.Rotation(math.radians(d), 4, fwd)
        m = arm.matrix_world @ b.matrix; loc = m.to_translation()
        b.matrix = arm.matrix_world.inverted() @ (Matrix.Translation(loc) @ R @ Matrix.Translation(-loc) @ m)
        bpy.context.view_layer.update()

g_arm, g_face = load(E["GIRL"], -0.32, 30)   # VRM faces -Y; turn toward the boy
b_arm, b_face = load(E["BOY"], 0.32, -30)
arms_down(g_arm); arms_down(b_arm)
gk = g_face.data.shape_keys.key_blocks; bk = b_face.data.shape_keys.key_blocks
gk['Fcl_MTH_Joy'].value = 0.35; gk['Fcl_BRW_Joy'].value = 0.6; gk['Fcl_EYE_Joy'].value = 0.25
bk['Fcl_MTH_Fun'].value = 0.5; bk['Fcl_BRW_Fun'].value = 0.4
# head tilt: girl looks slightly down/shy
hb = g_arm.pose.bones['J_Bip_C_Head']; hb.rotation_mode = 'XYZ'; hb.rotation_euler = (0.12, 0, 0.1)

# ground
bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
gm = bpy.data.materials.new("Ground"); gm.use_nodes = True
pr = gm.node_tree.nodes['Principled BSDF']; pr.inputs['Base Color'].default_value = (0.22, 0.2, 0.24, 1); pr.inputs['Roughness'].default_value = 1.0; pr.inputs['Specular IOR Level'].default_value = 0.0
bpy.context.object.data.materials.append(gm)
# sunset sky (Nishita)
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
nt = w.node_tree; sky = nt.nodes.new('ShaderNodeTexSky'); sky.sky_type = 'NISHITA'
sky.sun_elevation = math.radians(3); sky.sun_rotation = math.radians(180); sky.air_density = 2.5; sky.dust_density = 3
nt.links.new(sky.outputs[0], nt.nodes['Background'].inputs[0]); nt.nodes['Background'].inputs[1].default_value = 0.6
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN')); sun.data.energy = 2.2
sun.data.color = (1.0, 0.72, 0.5); sun.rotation_euler = (math.radians(75), 0, math.radians(200)); sc.collection.objects.link(sun)
fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", 'SUN')); fill.data.energy = 1.2
fill.data.color = (0.75, 0.8, 1.0); fill.rotation_euler = (math.radians(60), 0, math.radians(15)); sc.collection.objects.link(fill)
# camera
head = b_arm.matrix_world @ b_arm.pose.bones['J_Bip_C_Head'].head
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); cam.data.lens = 45
tgt = Vector((0, 0, head.z - 0.35)); cam.location = Vector((0, -3.1, tgt.z + 0.05))
cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler(); sc.camera = cam
sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = int(E.get("SAMPLES", 24)); sc.cycles.use_denoising = False
sc.view_settings.view_transform = 'Standard'
sc.render.resolution_x, sc.render.resolution_y = int(E["RES_X"]), int(E["RES_Y"])
sc.render.filepath = E["OUT"]; bpy.ops.render.render(write_still=True); print("DONE")
