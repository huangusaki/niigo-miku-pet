# Hachimi Pet Companion Rules

When the `hachimi-pet` MCP server is connected:
- `hachimi_pet_notify`: Optionally send a brief speech bubble message, facial expression (`happy`, `excited`, `thinking`, `love`, `wink`, `determined`, `flustered`, `surprised`), or motion (`wave`, `hop`, `jump`, `nod`, `dance`, `bow`) to the user's Starry Catgirl desktop pet.
- Never block or delay turn completion waiting on the desktop pet; lifecycle hooks and transcript watchers sync live progress, context window usage, token throughput, and subagent states automatically.
