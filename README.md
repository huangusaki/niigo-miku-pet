# Niigo Miku Pet

25 时初音未来 Antigravity 桌宠插件

## 安装

已装过旧版的话，先退出桌宠，把插件目录里的 `hachimi-pet` 文件夹移到别处，再复制新版并重启 Antigravity。

**Windows (PowerShell):**

```powershell
git clone https://github.com/huangusaki/niigo-miku-pet.git "$env:TEMP\niigo-miku-pet"
Copy-Item -Recurse -Force "$env:TEMP\niigo-miku-pet\hachimi-pet" "$HOME\.gemini\config\plugins\hachimi-pet"
```

**macOS / Linux:**

```bash
git clone https://github.com/huangusaki/niigo-miku-pet.git /tmp/niigo-miku-pet
mkdir -p ~/.gemini/config/plugins
cp -R /tmp/niigo-miku-pet/hachimi-pet ~/.gemini/config/plugins/hachimi-pet
```

## 使用

- 在 Antigravity 对话框输入 **`/25miku`** 唤醒桌宠
- 右键系统托盘图标切换配色、音效与漫步模式
- 按 **`Alt + Shift + W`** 开关点击穿透

## License

[AGPL-3.0-or-later](./LICENSE)
