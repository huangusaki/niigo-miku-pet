# Third-Party Notices

## Pet Physics & Sprite Rig Runtime (`coopanion/kit/` & `sound.js`)
- Derived from **Coopanion** (`public/coopanion/kit/body.js`, `public/coopanion/kit/rig.js`, `public/coopanion/sound.js`, `public/coopanion/speech/`), licensed under **GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later)**. See [`LICENSE`](./LICENSE).
- Speech syllable bank (`speech/zh.ogg`, `speech/zh.json`) uses open dictionary/phonetic data distributed under Coopanion's notices (`CMUdict` BSD, `mecab-ipadic`, `Unicode`, `pypinyin` MIT).

## Hachimi Starry Catgirl Assets (`coopanion/whale/tex`, `feat`, `schemes`, `thumbs`)
- Custom Starry Catgirl character layers & 8 palette variants created for **Hachimi Pet**. Contains no third-party vendor logos or trademarks.

## Microsoft Edge WebView2 SDK (`app/shell-win32/`)
- Bundles the official redistributable assemblies (`Microsoft.Web.WebView2.Core.dll`, `Microsoft.Web.WebView2.WinForms.dll`, `WebView2Loader.dll`) under the **Microsoft WebView2 SDK License (3-Clause BSD)**. See [`app/shell-win32/WebView2-LICENSE.txt`](./app/shell-win32/WebView2-LICENSE.txt).

## Electron Runtime (Fallback for Non-Windows / `HACHIMI_RUNTIME=electron`)
- Electron (`electron@33.3.1`, MIT License) is downloaded/cached on demand into `~/.hachimi-pet/runtime` by `scripts/launch-pet.mjs` only when the native Windows WebView2 shell is unavailable or `HACHIMI_RUNTIME=electron` is set.
