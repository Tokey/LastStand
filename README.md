Last Stand is a two-player networked FPS game implemented with the Unity game engine, specifically designed to study the impacts of latency and latency compensation techniques on player performance and QoE. The game is set in a dense jungle environment, featuring numerous tall trees, thick foliage, an open hangar at the center, and uneven terrain formations. 
The gameplay features rounds of deathmatch 1v1 play with unlimited lives. The player character is equipped with fluid procedural animations for actions such as sprinting, jumping, aiming, and leaning. The game equips players with a single-fire rifle that holds a customizable number (default 11) of bullets per magazine (unlimited magazines) and a customizable fire rate (default 250 rounds per minute). 

A central feature of the game is the configurable experimental harness. This feature allows for launching game rounds with different amounts of delay for each player. Also included is a logging system that captures player data every game tick, provides round summaries, and records details of each projectile shot, facilitating detailed analysis of player performance under different latency conditions. After each round, the game shows customizable qualitative questions to players - the defaults inquires about perceived lag and acceptability. User answers are logged in the round summary.
