# TOJam2026

Working title: The game is not complete yet.
Engine: Unity 6.3.14  
Genre: Puzzle

You play as a tester who discovers a game developer working on a heavily unfinished Unity project. The player’s role is to test and exploit broken mechanics to help the developer finish the game. Each solved puzzle gradually assembles the empty Unity scene into a coherent, playable world.

Key Features
Meta puzzle design where broken mechanics are the core challenge.

Progressive scene construction: solving puzzles adds assets, lighting, and polish to the scene.

Playful developer character who gives tasks, hints, and commentary.

Modular level system that makes it easy to add new broken mechanics and puzzles.

Accessible controls and clear feedback so players can experiment without frustration.

Gameplay Example
Level concept  
The dev asks you to shoot a zombie and collect all coins. One coin is placed too high to reach, and the gun increases the zombie’s size instead of damaging it.

Player objective  
Collect every coin and complete the level.

Solution outline

Observe that bullets scale the zombie rather than harm it.

Exploit the mechanic by shooting the zombie repeatedly until it becomes giant.

Use the giant zombie as a moving platform or to trigger a physics interaction that dislodges the high coin.

Collect the coin and remaining coins to finish the level.

Result: Completing the puzzle triggers the scene to receive new assets and a developer reaction.

Installation and Setup
Requirements
Unity Editor version 6.3.14

OS: Windows 10 or later, macOS 11 or later

Disk space: 2 GB minimum for project files and assets

Getting the project
Clone the repository to your local machine.

Open Unity Hub and add the project folder.

Open the project in Unity 6.3.14.

Allow Unity to import assets and compile scripts.

First run
Open the scene Scenes/DevPlayground.unity.

Press Play in the editor to start the prototype.

Use the Console to view debug messages and developer hints.

Controls and UX
Movement: WASD or Arrow keys

Interact: E or Left Mouse Button

Shoot: Left Mouse Button (when a gun is equipped)

Inventory / Objectives: Tab or I

Restart Level: R

UX notes

Hints appear as developer comments in the top-left HUD.

Visual feedback is provided for broken mechanics (glitch shader, debug text).

Puzzle completion triggers a short scene assembly animation and a save checkpoint.

Project Structure
Assets/

Scripts/ — gameplay logic, puzzle controllers, dev dialogue

Scenes/ — main scenes and level templates

Prefabs/ — modular objects: zombies, coins, guns, UI elements

Art/ — placeholder art and final assets as they are added

Audio/ — SFX and music placeholders

ProjectSettings/ — Unity project settings

Docs/ — design notes, puzzle bank, and level flow diagrams

Development Notes
Puzzle Design Guidelines
Single-bug focus: Each puzzle should center on one broken mechanic.

Clear affordances: Players must be able to discover the mechanic through experimentation.

Multiple solutions: Design puzzles so creative exploitation is possible.

Progress feedback: Completing a puzzle should visibly improve the scene.

Art and Audio
Start with placeholder assets to iterate quickly.

Replace placeholders progressively as puzzles are finalized.

Keep audio cues short and informative to avoid cluttering the meta-narrative.

Testing
Use the DevConsole to toggle mechanics and simulate player actions.

Add unit tests for core systems where feasible.

Playtest for discoverability and frustration points; iterate on hints and affordances.

Known Issues
Some physics interactions can be unstable when objects scale beyond expected ranges.

AI pathfinding may fail for extremely large NPC sizes; add size clamps or alternate behaviors.

UI layout may break at uncommon resolutions; responsive fixes are in the roadmap.

Roadmap
Short term

Stabilize scaling physics and add size-aware AI behaviors.

Implement 5 additional puzzle templates.

Mid term

Replace placeholder art for the first act.

Add a save/load system and basic analytics for playtesting.

Long term

Expand narrative beats and developer backstory.

Polish scene assembly animations and final audio mix.

How to Contribute
Report issues using the repository issue tracker with steps to reproduce.

Propose puzzles by adding a design doc in Docs/Puzzles/ describing mechanics, objectives, and success states.

Submit PRs for code and assets. Keep changes modular and include a short description of the problem solved.

Playtest and leave feedback on difficulty, clarity, and fun factor.

Credits
Design: Angelica and team

Prototype Programming: Core team contributors

Art and Audio: Placeholder assets until final contributors are credited

Playtesters: Early testers and community volunteers

License
MIT License  
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files to deal in the Software without restriction, subject to the conditions in the LICENSE file.

Contact
Repository: https://github.com/AngelicaCuadrado/TOJam2026  
Lead: Angelica

Quick start checklist

Open Unity 6.3.14 → Scenes/DevPlayground.unity → Press Play

Use WASD and Left Mouse Button to experiment with mechanics

Report issues and add puzzle proposals to Docs/Puzzles/
