# Application icon

The original transparent winged-key image is in `src/ACZeroPCremap.App/Assets/winged-key-source.png`.
`winged-key.png` is the 256-pixel preview; `ACZeroPCremap.ico` is the multi-size Windows icon embedded in the EXE and WPF windows.
Run `powershell -File scripts/Build-Icon.ps1` to reproduce the ICO from the source PNG. The script packages the artwork at 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels with alpha transparency. No image service or Python dependency is needed to rebuild it.

The source artwork was generated with the built-in image generation tool. Prompt:

> Use case: logo-brand. Asset type: Windows desktop application icon for ACZeroPCremap, a flight game's keyboard and mouse control editor. Create one finished icon, no text, no letters. Subject: a bold burgundy keyboard keycap with a thick warm gold outline, flanked by short symmetrical angular aviation wings. The key is central, square with gently clipped corners, with a simple inset keycap contour; wings are a single clean geometric gold shape per side, swept upward, with at most two large feather-like steps. Refined flat emblem, crisp clean edges, extremely readable small silhouette, no fine detail. Deep burgundy #47160F, warm gold #F5CC38 and dark burgundy shadows. Front view, exact bilateral symmetry, nearly square overall silhouette (wings approximately twice keycap width, height about 75% of width). Generous transparent margin, centered. Transparent background outside the emblem, true alpha. No outer badge or surrounding square. No photorealistic keyboard, no illustration background, no typography, no metallic texture, no glow, no gradient backdrop, no game logos. Deliver a single large square transparent icon asset.
