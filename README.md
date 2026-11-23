<div align="center">
  <img src="./assets/logo.png" alt="Niigo Miku Pet" width="120" height="120" />
  <h1>Niigo Miku Pet</h1>
  <p>25 时初音未来 Antigravity 桌面伴侣插件</p>
</div>

<div align="center">
  <img src="./assets/preview.png" alt="Preview" width="800" />
</div>

## 安装

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

- 在 Antigravity 对话框输入 **`/hachimi`** 唤醒桌宠
- 右键系统托盘图标切换配色、音效与漫步模式
- 按 **`Alt + Shift + W`** 开关点击穿透

## License

[AGPL-3.0-or-later](./LICENSE)
