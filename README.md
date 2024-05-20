**Overview**

Last Stand is a two-player networked FPS game implemented with the Unity game engine, specifically designed to study the impacts of latency and latency compensation techniques on player performance and QoE. The game is set in a dense jungle environment, featuring numerous tall trees, thick foliage, an open hangar at the center, and uneven terrain formations. 
The gameplay features rounds of deathmatch 1v1 play with unlimited lives. The player character is equipped with fluid procedural animations for actions such as sprinting, jumping, aiming, and leaning. The game equips players with a single-fire rifle that holds a customizable number (default 11) of bullets per magazine (unlimited magazines) and a customizable fire rate (default 250 rounds per minute). 

A central feature of the game is the configurable experimental harness. This feature allows for launching game rounds with different amounts of delay for each player. Also included is a logging system that captures player data every game tick, provides round summaries, and records details of each projectile shot, facilitating detailed analysis of player performance under different latency conditions. After each round, the game shows customizable qualitative questions to players - the defaults inquires about perceived lag and acceptability. User answers are logged in the round summary.

**Configs**
Last Stand needs to be configured before experiments are run. At least 2 computers are required to run the game, where 1 will act as server/host and 1 as a client. Inside the *Data/* folder there are 3 configuration files. 

- Game Config
The first *GameConfig.csv* is for setting the game parameters and takes in 1 row of data, each with 5 columns defining the global game configuration: Is Host, IP, Port, Enable Ping Display and Round Duration.
  - Is Host Value can be TRUE or FALSE, indicating if the game is run in host mode or client mode.
  - IP For the host, it can be kept as 127.0.0.1. For clients, it must be changed to the host's IP address.
  - Port Must be the same on host and clients. By default it is 7777.
  - Ping Value can be TRUE or FALSE. When TRUE the players' current latency is shown in the top center of the screen.
  - Round duration indicates how long each round will last
 
Example File:
| IsHost  | IP  | Port  | Enable Ping Disply | Round Duration |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| FALSE	| 127.0.0.1| 	7777 |	FALSE |	FALSE |	80 |

*P.S: The main GameConfig.csv must not have any header, just values.*


- The second file *PlayerConfig.csv* defines the configuration for both of the players' delay settings each round. It can have **n** rows, indicating **n+1** rounds. The first row of the game is played twice: initially as the 'Practice Round' and then again at a random point in the session. Before each session, the round settings are shuffled to ensure variability in the experiences of the players. This file is read only on the host side and the settings are propagated to the client at the start of each round. There are 8 columns in each row. For the columns, these are:
  - Client 1 base delay,
  - Client 1 target delay,
  - Client 1 adaptive time delay increase rate, 
  - Client 1 adaptive time delay decrease rate.
    
  The next 4 columns are similar, but for the Client 2.

  Example File:
  | Client 1  base delay | Client 1 target delay  | Client 1 adaptive time delay increase rate | Client 1 adaptive time delay decrease rate | Client 2 base delay | Client 2 target delay | Client 2  adaptive time delay increase rate | Client 2 adaptive time delay decrease rate|
  | ------------- | ------------- | ------------- | ------------- | ------------- | ------------- | ------------- | ------------- |
  
  
