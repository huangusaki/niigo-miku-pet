---
name: hachimi
description: Launch or summon the lightweight Hachimi Starry Catgirl Desktop Pet HUD (/hachimi) to display real-time agent status, subagent/Swarm catgirl pets, context window usage, and token throughput on the desktop.
---

# Hachimi Desktop Pet HUD (`/hachimi`)

When the user activates `/hachimi` (or asks to launch/summon the Hachimi desktop pet):
1. Check `http://127.0.0.1:17890/health`; if the pet is not yet running, launch it detached via `node "<plugin_dir>/scripts/launch-pet.mjs"` (using the plugin directory path of this skill).
2. Optionally call `hachimi_pet_notify` with a friendly greeting (`expression: "happy"`, `motion: "wave"`).
3. End your turn normally.
