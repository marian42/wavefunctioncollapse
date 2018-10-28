![](https://i.imgur.com/nudwW5W.jpg)

# Wave Function Collapse

An infinite, procedurally generated city, assembled out of blocks using the Wave Function Collapse algorithm.
Currently, there is no gameplay, you can only walk around and look at the scenery.

Controls: WASD for walking, Shift to run, Ctrl to jetpack.

Read more about the WFC algorithm [here](https://github.com/mxgmn/WaveFunctionCollapse).

Play the game on Itch.io: [https://marian42.itch.io/wfc](https://marian42.itch.io/wfc)

## Unity project

To view the project in Unity, import it and download the Postprocessing stack from the Asset Store. Alternatively, you can remove the Post Processing Behaviour from the Main Camera. 

## Known issues

- Sometimes the WFC algorithm fails. In this case, you'll see white cubes in the world
- The generated world is never unloaded, resulting in worse performance if you explore longer
