# 夜班吊装

竖屏 Unity 叠楼小游戏原型。吊机在夜间工地上运送预制楼层，玩家找准时机放下楼板，塔顶和镜头一起升高。

## 本地试玩

1. 用 Unity Hub 打开这个目录，使用 Unity 2022.3.62f2。
2. 打开 `Assets/NightShift/Scenes/NightShift.unity`，从 `Tools > Night Shift > Portrait Preview (1080 x 1920)` 切到固定竖屏预览，再按 Play。也可以在 Game 视图手动添加 1080×1920 的 Fixed Resolution。检查像素时可最大化 Game 视图并使用 1× Scale。Scene 视图的 2D 开关只切换编辑视角。
3. 选择休闲或标准模式。点击、轻触画面或按空格放下楼板。青色投影线表示当前可以落稳，橙红色表示失稳。完美对齐会点亮窗灯。

休闲模式只检查新楼板和塔顶之间的支撑。标准模式还会检查下方每个接缝的累计重心。右侧小图显示已落稳楼层的整体重心在地基上的投影。接缝接近失稳时，底部会另外显示该接缝的安全范围和重心。下方接缝失稳时，镜头会对准该处，上方楼段从接缝处倾倒。两种模式分别保存最高分。

楼层落稳后，窗内会出现带家具和道具的生活场景，包括阅读、做饭、使用电脑、看电视、浇花、锻炼、整理衣物和交谈。每局重新洗牌选择立面布局与活动，人物各自按不同节奏活动和停顿。环境随高度从雨后街区进入云层，偶尔有鸟群或客机飞过。再往上会经过由天阶相连的三层云上宫殿，下层是山门，中层是双阁，上层是主殿和两座塔阁。随后进入带环行星和外星飞碟出没的近地太空。抖音 SDK 接入是后续开发内容。

楼体、吊机、街区和音效由程序生成。街道和工地共用 [湿沥青](Assets/NightShift/Art/WetAsphalt.png)，楼板与横梁使用 [雨后混凝土](Assets/NightShift/Art/RainConcrete.png)，窗格使用 [带暖工灯的窗内贴图](Assets/NightShift/Art/WindowRoom.png)。贴图由内置图像生成工具制作。混凝土使用无方向光的正俯视灰色材料，窗内图使用正面观察的未完工房间。湿沥青在 Unity 中保留 Read/Write 和 Repeat，供工地材质运行时采样。

界面在 uGUI 的 Screen Space Overlay 画布上用 TextMeshPro 渲染。正文和比分使用 [Noto Sans CJK](https://github.com/notofonts/noto-cjk/tree/Sans2.004)，标题使用 [得意黑](https://github.com/atelier-anchor/smiley-sans/releases/tag/v2.0.1)。两套静态 SDF 图集及原字体的 OFL 许可证在 `Assets/NightShift/Fonts/`。

## 开发调试

在 Unity Play Mode 或 Development Build 按 **F1**。开发构建也可点右上角 **DEV**。面板提供城市、云层和云上预设，支持跳到 0–80 层、暂停摆动、完美落板、复现标准模式的地基失稳和重开本局。可跳到 24 层看云中飞行物，在 43、53 和 67 层分别看天宫的山门、双阁和主殿，在 77 层看近地太空。

跳层、冻结或执行调试落板会把当前对局标为调试局，正常最高分保持独立。选择重开本局可恢复正常计分。调试面板与调试指令只编译进编辑器和开发构建。

[游戏设计](docs/night-shift-design.md) · [高度互动示意](docs/tower-altitude.html)
