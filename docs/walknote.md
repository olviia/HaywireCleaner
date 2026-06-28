import bpy

# Clear the default scene (cube, camera, light)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

# Cylinder: radius=0.5 (diameter=1), depth=0.1 (height)
bpy.ops.mesh.primitive_cylinder_add(
vertices=16,
radius=0.5,
depth=0.1,
location=(0, 0, 0)
)

obj = bpy.context.active_object
obj.name = "bot_collision_hull"

# Freeze all transforms so export is clean
bpy.ops.object.transform_apply(location=True, rotation=True,
scale=True)