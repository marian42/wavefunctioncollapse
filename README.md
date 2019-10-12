![](https://i.imgur.com/vL80izv.jpg)

# Wave Function Collapse

An infinite, procedurally generated city, assembled out of blocks using the Wave Function Collapse algorithm with backtracking.

Read more about this project [here](https://marian42.de/article/wfc/) and about the WFC algorithm [here](https://github.com/mxgmn/WaveFunctionCollapse).

## Play

Download the game on Itch.io: [https://marian42.itch.io/wfc](https://marian42.itch.io/wfc)  
Currently, there is no gameplay, you can only walk around and look at the scenery.

Keyboard Controls: WASD for walking, Space to jump, Shift to run, Ctrl to jetpack.  
XBOX Controls: Left Stick for walking, right stick for looking around, A to jump, LB to run, RB to jetpack

Flight mode: Use M to toggle between flight mode and normal mode. In flight mode, you fly across the world, without any controls.

## Opening the project in Unity

If you want to work on this project using the Unity Editor, you need Blender 2.79 installed on your computer.

## Editing the module set

By changing the module set, you can make some changes to the world generation without writing code.
You can disable or enable modules, change their spawn probability, their connectors, their neighbor rules or you can add new ones.
Here is how to do it:

1. Open the `Prototypes` scene.
2. Edit the blocks in the scene. You'll mostly change values in the `ModulePrototype` components.
3. Select the "Prototypes" game object in the hierarchy and apply your changes to the prefab (Overrides -> Apply all).
4. Select the file "ModuleData" in the asset folder.
5. Click "Create module data".
6. Optional: Click "Simplify module data". This takes some time, but will make world generation faster.
7. Save your work and go back to the `Game` scene. You can now use your updated module set.

## Generating worlds in the editor

There are different ways to generate worlds in the editor:

- Select the Map object. In the `MapBehaviour` component, select a size and click "Initialize NxN area".
- Select the "Area Selector" object.
Move and scale it to select an area, then use the "Generate" button to generate a map.
- Use the "Slot Inspector" object to show details about a single position.
It shows you which modules can be spawned at that position and lets you select modules manually.

If you want to enter Play mode without losing your map, disable the "Generate Map Near Player" and the "Occlusion culling" script.
Note that none of the components serialize, so you can't change the map once it has been serialized.
That means that you can't change your map in Play mode unless you initialized it in Play mode.
