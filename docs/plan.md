# Plan


## Two tracks, running in parallel

**Dev track** — make the game
**Community track** — build a comunity/personal brand. familiarize people with my game

## What to do

done comunity:
+ Make a page on itch
+ export to web 
+ publish on itch
+ - make linkedin and reddit post with a short video, link to itch, and urge them to share their opinion - if movement is intuitive, is flashlight satisfying, is cleaning dust satisfying, are random beads nice.
- - add description to itch page what controls are in this prototype  and what happens there
- start making tiktoks
- - combine first protytype scene and second prototype scene, build for web and publish, with the possibility to switch between modes with numbers: 1, 2
- - start making an mda, plan architecture

in progress community:
- keep making tiktoks
- - start making devlogs on itch monthly
- - start making an mvp with correct architecture, so that this will be the core that can be a foundation, and everything else to be built on top of it.

to do community:
 
- 

todo/in progress  dev:

--the bot leaves its station area and         appears in the dusty room. this is the full next step: first, load game, so    when i am testing, i dont have to repeat anything, i just click 'load' with    my already saved progress. then the dusty shader. when the bot goes through    the dust, it leaves clean path behind itself. then the cutscene that rises     the camera and showes this clean path. then main quest remains 'explore        outside' and the sidequest emerges saying 'clean the room'. then tutorial      popup that says 'press tab to open inventory', and then when inventory is      opened, says 'press Space/x to start tracking quest'. then inventory system    to gather dust, every like 0.1 distance some amount of dust is collected. it   has to be not hardcoded, because in the future the dust will accumulate        gradually, that will be reflected by shader. or even we should do it           straight away. this is my next epic
- 
- ◻ Build minimal Settings screen
- Add audio minimal sounds


◻ Build minimal save/load seam (slowly building when other components are added)
make cutscene images slighty longer

done dev:

- tutorial prompt
-  build base ui for the quest system
- awakening sequence - quest reveal
- quest system
-awakening sequence - text reveal
- - charging station - interactable
- door animation - interactable
- awakening sequence - dizzying overlay
+ add 1 popup for when light appears
+ add 1 popup for when we collect all dust
+ - - start making the second prototype
- ◻ Define Core/SceneState seam (Bootstrap, SceneStateMachine, SceneLoader)
- ◻ Build minimal Title screen (first GameFlow seam proof)
- ◻ Design data-driven Cutscene system (CutsceneDef + player)
- start of input: move and interact for player
-   - swappable dummy
- modules that i drop on the dummy that define it's behaviour
- - player module
    - tags
- figure out how modules affect animations on the dummy? and shrinking/changing its mesh?