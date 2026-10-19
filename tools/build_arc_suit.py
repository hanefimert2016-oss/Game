import bpy, math
from mathutils import Vector
from pathlib import Path

ROOT=Path(__file__).resolve().parent
OUT=ROOT.parent/'ArcSquadUnity'/'Assets'/'ArcSquad'/'Art'/'Models'
OUT.mkdir(parents=True,exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)

def mat(name,hexrgb,metal=.35,rough=.3,emiss=0):
    rgb=tuple(int(hexrgb[k:k+2],16)/255 for k in (0,2,4))
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*rgb,1)
    m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*rgb,1)
    p.inputs['Metallic'].default_value=metal
    p.inputs['Roughness'].default_value=rough
    p.inputs['Emission Color'].default_value=(*rgb,1)
    p.inputs['Emission Strength'].default_value=emiss
    return m

red=mat('ARMOR RUBY // IRIDESCENT','E72C54',.55,.22)
reddeep=mat('ARMOR BURGUNDY','6E183C',.57,.23)
orange=mat('ARMOR GOLD','FFC64B',.66,.22)
offwhite=mat('ARMOR FACEPLATE','F8E9AC',.45,.24)
black=mat('INNER CARBON','1B263E',.45,.32)
cyan=mat('NEON CYAN','5AFFFF',.08,.16,6.0)
blue=mat('NEON COBALT','3BA6FF',.05,.17,3.8)

parts=[]
def finish(o,material,name,bev=0.0):
    o.name=name
    if material:o.data.materials.append(material)
    if bev>0:
        mod=o.modifiers.new('rounded machined surfaces','BEVEL')
        mod.width=bev;mod.segments=3
        mod.affect='EDGES'
        mod=o.modifiers.new('weighted normals','WEIGHTED_NORMAL')
        mod.keep_sharp=True
    for face in o.data.polygons:face.use_smooth=True
    parts.append(o);return o

def box(name,loc,scale,material,bev=.12):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,material,name,bev)

def uv(name,loc,scale,material,segments=24,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    o=bpy.context.object;o.scale=scale
    return finish(o,material,name)

def tube(name,a,b,radius,material,verts=16):
    a,b=Vector(a),Vector(b)
    v=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=v.length,location=(a+b)/2)
    o=bpy.context.object
    o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
    return finish(o,material,name,.04)

def disk(name,loc,r,depth,material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=36,radius=r,depth=depth,location=loc)
    o=bpy.context.object
    o.rotation_euler[0]=math.pi/2 # emitter points towards viewer/front
    return finish(o,material,name,.03)

# Torso and floating anatomical hard-surface shell.
box('ARMOR CHEST / main plating',(0,0,.55),(2.75,1.58,3.1),red,.31)
box('carbon waist',(0,.0,-1.03),(1.95,1.25,.75),black,.2)
box('ARMOR sternum faceplate',(0,-.87,.88),(1.75,.22,2.25),reddeep,.25)
box('ARMOR lower abdomen',(0,-.82,-.23),(1.8,.20,.72),red,.12)
for x in [-1.02,1.02]:
    box('GOLD lateral chest',(x,-.68,1.3),(.58,.33,1.02),orange,.15)
    box('ARMOR rib armor',(x,-.72,.2),(.6,.30,.77),red,.12)
    tube('INNER shoulder strut',(x*.9,.12,1.65),(x*1.55,.10,1.37),.3,black)
# Arc reactor with layered rings and clear core.
disk('reactor carbon bezel',(0,-.99,.71),.75,.18,black)
disk('reactor gold ring',(0,-1.11,.71),.63,.14,orange)
disk('REACTOR NEON CYAN CORE',(0,-1.23,.71),.47,.14,cyan)
disk('reactor aperture',(0,-1.33,.71),.24,.08,offwhite)

tube('INNER neck',(0,0,1.85),(0,0,2.58),.38,black)
uv('ARMOR HELMET shell',(0,0,3.0),(.99,.82,1.12),red,32,18)
box('FACEPLATE champagne',(0,-.69,3.0),(1.52,.32,1.32),offwhite,.27)
box('ARMOR brow',(0,-.76,3.60),(1.56,.35,.30),red,.1)
box('chin alloy',(0,-.69,2.46),(1.11,.38,.22),orange,.1)
for sign in [-1,1]:
    box('VISOR NEON EYE',(sign*.39,-.878,3.24),(.50,.09,.17),cyan,.045)
    box('helmet cheek',(sign*.77,-.52,2.81),(.25,.3,.64),red,.06)

# True multi-part gauntlets, shoulders, arm blasters.
for sign in [-1,1]:
    sx=sign
    uv('ARMOR SHOULDER joint',(sx*1.77,0,1.36),(.85,.78,.67),red,28,14)
    box('GOLD shoulder inset',(sx*1.78,-.40,1.49),(1.04,.16,.52),orange,.12)
    tube('INNER upper arm',(sx*1.86,0,1.15),(sx*2.05,0,-.15),.37,black)
    box('ARMOR forearm gauntlet',(sx*2.09,-.05,-.1),(.92,1.01,1.42),red,.24)
    box('GOLD gauntlet inset',(sx*2.08,-.6,-.02),(.63,.2,.85),orange,.13)
    uv('ARMOR wrist',(sx*2.11,-.05,-1.02),(.53,.53,.56),red,24,12)
    disk('LEFT REPULSOR' if sx<0 else 'RIGHT REPULSOR',(sx*2.1,-.62,-1.03),.29,.12,cyan)
    # Palm/ring orient in forward direction, visible in front view.
    disk('gold emitter retainer',(sx*2.1,-.53,-1.03),.39,.12,orange)
    uv('ARMOR thigh',(sx*.74,-.01,-2.08),(.67,.68,1.25),red,26,14)
    box('GOLD kneeguard',(sx*.74,-.58,-2.45),(.81,.26,.5),orange,.13)
    tube('INNER calf',(sx*.74,0,-2.7),(sx*.74,0,-3.95),.43,black)
    box('ARMOR shinplate',(sx*.76,-.38,-3.35),(.88,.38,1.48),red,.18)
    box('ARMOR armored boot',(sx*.74,-.26,-4.26),(1.01,1.35,.78),orange,.21)
    disk('THRUSTER NEON',(sx*.74,-.35,-4.77),.38,.15,cyan)
    box('ARMOR side fin',(sx*1.2,.31,-3.65),(.19,.78,1.13),reddeep,.07)

# Small technological details.
for x in [-.6,.6]:
    for z in [1.56,1.35]:
        box('ARMOR vent',(x,-.89,z),(.25,.12,.08),black,.015)
box('ARMOR BACK THRUSTER',(0,.92,.2),(1.36,.35,1.37),reddeep,.18)
uv('LUMINOUS SPINE',(0,1.07,.2),(.48,.2,.5),blue)

# Export only actual character meshes — no camera/background.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.export_scene.fbx(
    filepath=str(OUT/'ArcSquad-Suit-Mk1.fbx'), use_selection=True,
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    add_leaf_bones=False, bake_anim=False, use_mesh_modifiers=True,
    path_mode='AUTO'
)
assert (OUT/'ArcSquad-Suit-Mk1.fbx').stat().st_size > 20000

# Create studio product shot from the SAME modeled asset, not a game screenshot.
bpy.ops.object.select_all(action='DESELECT')
def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(10,-17,5))
camera=bpy.context.object;aim(camera,(0,0,-.5));camera.data.type='ORTHO';camera.data.ortho_scale=13
bpy.context.scene.camera=camera
for name,loc,power,size,color in [
    ('Key', (3,-8,9),1500,8,(.63,.91,1)),
    ('Fill',(-7,-3,4),920,7,(1,.54,.39)),
    ('Rim',(2,6,7),1400,5,(.33,.55,1))]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size
    o.data.color=color;aim(o,(0,0,0))
world=bpy.context.scene.world
world.color=(.018,.027,.08)
scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.samples=20
scene.render.resolution_x=760;scene.render.resolution_y=760;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(OUT/'ArcSuit-studio-render.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ArcSuit-Mk1.blend'))
print('ARC_SUIT_EXPORT_OK', (OUT/'ArcSquad-Suit-Mk1.fbx').stat().st_size)
