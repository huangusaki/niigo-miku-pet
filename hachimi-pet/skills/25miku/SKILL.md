---
name: 25miku
description: 在用户输入 /25miku 或要求唤醒 25 时初音桌宠时，打开 Antigravity 桌面伴侣，展示当前对话和分身的进度。
---

# 唤醒 25 时初音桌宠（`/25miku`）

When the user activates `/25miku` or asks to summon Niigo Miku:
1. Check `http://127.0.0.1:17890/health`; if the pet is not yet running, launch it detached via `node "<plugin_dir>/scripts/launch-pet.mjs"` (using the plugin directory path of this skill).
2. Optionally call `hachimi_pet_notify` with a friendly greeting (`expression: "happy"`, `motion: "wave"`).
3. End your turn normally.
