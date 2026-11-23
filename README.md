<div align="center">
  <img src="./assets/logo.png" alt="Niigo Miku Pet Logo" width="128" height="128" />
  <h1>Niigo Miku Desktop Pet (`niigo-miku-pet`)</h1>
  <p><strong>基于 Antigravity 桌面伴侣插件魔改的「25 时、空之世界 初音未来」全动态桌宠</strong></p>
  <p>
    <a href="./LICENSE"><img src="https://img.shields.io/badge/License-AGPL--3.0--or--later-blue.svg" alt="License: AGPL-3.0-or-later" /></a>
    <img src="https://img.shields.io/badge/Antigravity-Plugin-purple.svg" alt="Antigravity Plugin" />
    <img src="https://img.shields.io/badge/Windows-Native%20WebView2-0078D4.svg" alt="Windows WebView2" />
  </p>
</div>

<div align="center">
  <img src="./assets/preview.png" alt="Niigo Miku Pet Preview" width="840" />
</div>

---

## ✨ 魔改亮点

本项目基于开源项目 [`hwt675217/hachimi-pet`](https://github.com/hwt675217/hachimi-pet) 进行深度角色重制与动画细节调优，保留原插件的全部实时对话联动、上下文占用环、生成速度监控、多子代理空降与点击穿透功能：

- **全套重制「25 时初音未来（时钟礼服版）」可动形象**：淡银紫双马尾、纯白丝带蝴蝶结、白蕾丝衬衣、酒红领带、罗马数字胸带（`X XI XII ☒ I II III`）、金色双钥匙与百褶裙全套分层拆分。
- **精细化动态骨骼调优**：
  - 双马尾与刘海随走路、跳跃、拎起自然飘动，左右纯白蝴蝶结独立轻柔摆动；
  - 拎起防变形补偿，悬空拖拽时头部依然保持圆润可爱；
  - 专属收巧版坐姿白裙与双腿，坐下和打盹时重心自然贴地。
- **防翻反与眼神系统升级**：
  - **左右转身异色瞳不翻反**：无论面朝左还是面朝右，画面左侧始终为冰青蓝瞳、右侧始终为淡紫粉瞳；
  - **罗马数字胸带不倒字**：转身时胸前罗马数字自动正向呈现；
  - **对称眼神追踪**：修复左右眼转动幅度不一致问题，杜绝斗鸡眼；
  - **全套 7 组清冷气质表情**：平静、温柔弯月笑、静谧睡颜、慌张（`> <`）、宝石爱心眼、独立旋转内螺旋晕眼（睫毛眼眶保持不动）与微微惊讶；
  - 同步配备 **8 套配色方案**，方便切换发色或用于多智能体子分身区分。

---

## 🚀 快速安装

### Windows (PowerShell)

```powershell
git clone https://github.com/huangusaki/niigo-miku-pet.git "$env:TEMP\niigo-miku-pet"
Copy-Item -Recurse -Force "$env:TEMP\niigo-miku-pet\hachimi-pet" "$HOME\.gemini\config\plugins\hachimi-pet"
```

### macOS / Linux

```bash
git clone https://github.com/huangusaki/niigo-miku-pet.git /tmp/niigo-miku-pet
mkdir -p ~/.gemini/config/plugins
cp -R /tmp/niigo-miku-pet/hachimi-pet ~/.gemini/config/plugins/hachimi-pet
```

安装完成后，在 Antigravity 对话框里输入 **`/hachimi`** 即可唤醒桌宠。

- **切换配色 / 音效 / 自由漫步**：右键点击电脑右下角系统托盘图标；
- **点击穿透开关**：按快捷键 **`Alt + Shift + W`**。

---

## 📄 致谢与开源协议

- 上游基础插件框架来自 [`hwt675217/hachimi-pet`](https://github.com/hwt675217/hachimi-pet)，详见 [`hachimi-pet/THIRD_PARTY_NOTICES.md`](./hachimi-pet/THIRD_PARTY_NOTICES.md)。
- 本项目遵循 **GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)**，详见 [`LICENSE`](./LICENSE)。
