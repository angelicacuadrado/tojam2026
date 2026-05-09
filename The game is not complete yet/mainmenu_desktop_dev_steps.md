# MainMenu 桌面 / 剧情门控系统 — 后续开发流程

> 配合 `unity_desktop_window_fps_scene_dev_steps.md` 一起看：那份描述的是"游戏窗口 + RenderTexture + FPS 注入"的机制；本文聚焦在桌面 / 任务栏 / 章节门控剧情之上**还差什么**、按什么顺序做。

---

## 0. 当前已完成

### 脚本
```
Assets/Scripts/MainMenu/
├── WindowControls.cs              ✓ events, isSingleton, preserveOnClose, headerHeight
├── DragWindow.cs                  ✓ (未动)
├── Core/
│   ├── WindowManager.cs           ✓ 单例 + preservedWindows 复活机制
│   ├── DesktopIcon.cs             ✓ 双击 / 单击切换
│   └── WindowFocus.cs             ✓ 点击置顶
├── Taskbar/
│   ├── TaskbarController.cs       ✓
│   └── TaskbarButton.cs           ✓ 最小化指示 + 切换逻辑
├── StartMenu/
│   ├── StartMenuController.cs     ✓
│   └── StartMenuBackdrop.cs       ✓ 点空白处关菜单
├── Settings/
│   ├── SettingsManager.cs         ✓ static + PlayerPrefs
│   └── SettingsWindow.cs          ✓
├── Messages/
│   ├── MessageConversation.cs     ✓ MessageEntry + MessageChapter + MessageStory
│   ├── MessageScheduler.cs        ✓ 分章 + AdvanceToNextChapter()
│   ├── MessageWindow.cs           ✓ 历史重建
│   ├── MessageBubble.cs           ✓ 可选 CTA 按钮
│   └── ChapterProgressManager.cs  ✓ 监听 Exit.AnyLevelCompleted
└── Credits/
    └── CreditsWindow.cs           ✓
```

### 框架修改
- `GameManager` 去掉 `DontDestroyOnLoad`，每个 level scene 自带
- `Exit` 加了 `static event AnyLevelCompleted`

### Prefab / 资源
- `Window.prefab` ✓
- `WindowLevel1.prefab` ✓
- `Render Texture Level1.renderTexture` ✓
- 桌面背景图 / Logo 图素材 ✓

---

## 1. 开发阶段总览

| 阶段 | 内容 | 阻塞下一步 |
|---|---|---|
| A | 桌面基础接线（场景结构 / 三个内容窗口 prefab） | 是 |
| B | 游戏窗口 + FPS 注入（GameSceneSessionManager 等） | 是 |
| C | PlayerController 接 IGameInputReceiver + Sensitivity | 是（B 之后才能测） |
| D | 剧情内容（MessageStory + Chapter 资产） | 否 |
| E | Level 2 / Level 3 复制 | 否 |
| F | 打磨 / 视觉 / 测试 | 否 |

A → B → C 是关键路径，必须按序完成才能跑通"消息 → 点按钮 → 进 FPS → 通关 → 下一章"。
D / E / F 可以并行或最后做。

---

## 2. Phase A — 桌面基础接线

### A.1 MainMenu 场景层级

```
MainMenuScene
├── Main Camera
├── EventSystem
├── Canvas (Screen Space - Overlay)
│   ├── Desktop                     ← 背景图 (menackni-xp-low.jpg)
│   │   ├── Icon_Messages           +DesktopIcon → MessageWindow.prefab
│   │   └── Icon_Credits            +DesktopIcon → CreditsWindow.prefab
│   ├── Windows                     ← 空 RectTransform，铺满 Canvas，所有窗口 spawn 这里
│   ├── Taskbar
│   │   ├── StartButton             +Button
│   │   ├── ButtonContainer         (Horizontal Layout Group)
│   │   ├── StartMenuBackdrop       +Image (alpha=0, Raycast Target ✓) +StartMenuBackdrop
│   │   └── StartMenuPanel          (默认 inactive)
│   │       ├── Button_Settings
│   │       └── Button_Shutdown
└── Managers
    ├── WindowManager               windowRoot → Canvas/Windows
    ├── StartMenuController         + 拖各按钮 / SettingsWindow.prefab
    ├── TaskbarController           buttonPrefab → TaskbarButton.prefab, container → ButtonContainer
    ├── MessageScheduler            story → Story_Main.asset
    └── ChapterProgressManager
```

⚠️ Sibling 顺序：`StartMenuBackdrop` 必须在 `StartButton` 之后、`StartMenuPanel` 之前。

### A.2 三个内容窗口 prefab（Window.prefab 的变体）

| Prefab | windowId | windowTitle | isSingleton | preserveOnClose |
|---|---|---|---|---|
| `MessageWindow.prefab` | `messages` | `Messages` | ✓ | ✓ |
| `CreditsWindow.prefab` | `credits` | `Credits.txt` | ✓ | ✓ |
| `SettingsWindow.prefab` | `settings` | `Settings` | ✓ | ✓ |

每个变体在 Content 节点下加自己的 UI，绑对应脚本：
- MessageWindow：ScrollView + bubblePrefab + messageContainer + scrollRect
- CreditsWindow：TMP_Text + 一个 `Credits.txt` TextAsset
- SettingsWindow：两个 Slider + 数值 TMP_Text

### A.3 TaskbarButton.prefab + MessageBubble.prefab

- `TaskbarButton`：Button + Image (icon) + TMP_Text + 可选 MinimizedIndicator
- `MessageBubble`：Vertical Layout，包含 senderLabel + bodyLabel + **CTAContainer (子对象，默认 inactive)**，CTAContainer 里有 Button + ctaLabel

### A.4 验收
- 启动场景，桌面图标双击 → 窗口打开 + 任务栏出现按钮
- 任务栏按钮点击：在最小化 / 还原 / 置顶之间切换
- Min/Max/Close 按钮全部可用（OnClick 绑 `ToggleMinimize` / `ToggleMaximize` / `Close`）
- 开始菜单：StartButton 切换菜单显隐；点空白处关闭；Settings 打开设置窗口；Shutdown 退出
- 设置滑条改值后 PlayerPrefs 持久化（重启后保留）

---

## 3. Phase B — 游戏窗口 / FPS 注入

参考 `unity_desktop_window_fps_scene_dev_steps.md` 的章节 8 - 13 实现，但需要落地到当前架构里。

### B.1 新增脚本

```
Assets/Scripts/MainMenu/GameWindow/
├── IGameInputReceiver.cs        ← 接口：SetInputEnabled(bool)
├── GamePauseController.cs       ← static IsPaused，敌人 / 计时器读它
├── GameSceneSessionManager.cs   ← 一个 GameWindow 一个，管 additive load + RT + focus + esc + close 时 unload
└── GameWindowFocusArea.cs       ← 挂 RawImage，点击重新 focus
```

参考 dev_steps.md 第 10 / 11 / 12 节代码即可，但接入点改一下：
- `GameSceneSessionManager` 不再是场景级单例，而是挂在 **每个 LevelN 窗口 prefab 上**（与 WindowControls 同级）
- `OpenWindow()` 由 `WindowControls` 实例化时 `Awake` / `OnEnable` 自动调用，不再需要桌面图标按钮触发（因为现在窗口是从消息 CTA 打开的）
- `CloseWindow()` 监听 `WindowControls.Closing` 事件

```csharp
// GameSceneSessionManager.cs (大致样貌)
private void Awake()
{
    var controls = GetComponent<WindowControls>();
    controls.Closing += _ => CloseWindow();
}
private void OnEnable() => OpenWindow();
```

### B.2 用户决策对应实现
- **关卡关闭后 unload + 重置** → `CloseWindow()` 调 `SceneManager.UnloadSceneAsync`
- **一次只开一个关卡窗口** → 在 WindowManager 里加一组"互斥"语义，或在 `GameSceneSessionManager.OpenWindow` 里检查全局已开关卡列表。最简实现：在 `GameSceneSessionManager` 里维护 `static GameSceneSessionManager Active`，新打开时若已有 Active，先 `Active.CloseWindow()`。

### B.3 三个 LevelN 窗口 prefab

基于现有 `WindowLevel1.prefab` 创建变体：
- `WindowLevel1.prefab`：sceneName=`Level1`, RT=`Render Texture Level1`, windowId=`level1`, isSingleton=✓, preserveOnClose=✗
- `WindowLevel2.prefab`：sceneName=`Level2`, RT=`Render Texture Level2`（要新建）
- `WindowLevel3.prefab`：sceneName=`Level3`, RT=`Render Texture Level3`（要新建）

每个 prefab 内还要加：
- RawImage（绑对应 RT）+ GameWindowFocusArea
- PauseOverlay（Resume 按钮）
- GameSceneSessionManager 组件

### B.4 Build Settings
```
0  MainMenu
1  Level1
2  Level2
3  Level3
```
启动 scene = MainMenu。

### B.5 验收
- 从消息 CTA 打开 LevelN.exe 按钮 → 关卡 scene additive 加载 → RawImage 显示游戏画面
- 鼠标自动锁定到游戏；Esc 暂停，鼠标释放，PauseOverlay 显示
- 点 X 关闭窗口 → 关卡 scene unload；再次打开是初始状态

---

## 4. Phase C — PlayerController 接入

需要改你现有的 `Assets/Scripts/Player/PlayerController.cs`：

1. **实现 `IGameInputReceiver`**：所有移动 / 视角逻辑被 `inputEnabled` flag 门控
2. **鼠标灵敏度从 `SettingsManager.MouseSensitivity` 读**，并订阅 `SettingsManager.Changed` 实时刷新
3. **音量** 已经通过 `SettingsManager.ApplyVolume` 自动作用于 AudioListener，PlayerController 不用动

```csharp
public class PlayerController : MonoBehaviour, IGameInputReceiver
{
    private bool inputEnabled;
    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

    void Update()
    {
        if (!inputEnabled || GamePauseController.IsPaused) return;
        // movement / mouse look using SettingsManager.MouseSensitivity
    }
}
```

敌人 / 计时器同样：
```csharp
if (GamePauseController.IsPaused) return;
```

---

## 5. Phase D — 剧情内容（资产）

```
Assets/_Project/Story/
├── Story_Main.asset                 (MessageStory)
├── Chapter_01_Intro.asset           ←最后一条带 Level1.exe 按钮
├── Chapter_02_AfterLevel1.asset     ←最后一条带 Level2.exe 按钮
├── Chapter_03_AfterLevel2.asset     ←最后一条带 Level3.exe 按钮
└── Chapter_04_Outro.asset           ←可选 ending 文本
```

Story_Main.chapters 里按顺序拖入这 4 个 Chapter。MessageScheduler.story 拖 Story_Main。

每个 Chapter 用 inspector 编辑 `messages` 列表：sender / body / delayAfterPrevious。最后一条额外填 `buttonLabel = "Level1.exe"` + `windowPrefabToOpen = WindowLevel1.prefab`。

---

## 6. Phase E — Level 2 / Level 3

每个新关卡：
1. Duplicate `Level1.unity` → 改名 `Level2.unity`
2. 修改 GameManager 的 `winCondition`（不同 keys / enemies 数量）
3. 改场景内容（不同布局 / 敌人数 / 风险）
4. 加入 Build Settings
5. 创建对应的 `Render Texture Level2.renderTexture`
6. Duplicate `WindowLevel1.prefab` → `WindowLevel2.prefab`，把 RT / sceneName 字段改掉

Level3 同理。

---

## 7. Phase F — 打磨与测试

### 视觉
- XP 风格的 StartButton / Taskbar / Window TitleBar 配色（在 prefab Inspector 里调，**不要写死在脚本里**）
- 桌面图标加 hover / selected 视觉反馈
- 消息气泡的 sender / body / CTA 按钮样式
- PauseOverlay 的暗化背景 + 中心提示文字

### 全流程冒烟测试 (golden path)
1. 启动游戏 → 桌面 + 任务栏可见
2. 等 ~2 秒 → Chapter 1 第一条消息推送（如果消息窗口默认关，看不到 — 用 desktop notification badge 补强？或者第一次自动打开消息窗）
3. 点 Messages 图标 → 历史消息渲染，含 Level1.exe 按钮
4. 点 Level1.exe → 游戏窗口打开 + FPS 视角 + 鼠标锁
5. 通关（开门 + 走过出口）→ Chapter 2 推送
6. 关闭 Level1 窗口 → scene unload
7. Level2.exe 按钮在新消息里出现 → 重复步骤 4-7
8. Level3 完成 → Chapter 4 outro

### 边界
- 中途强制关闭关卡窗口（X 键）：剧情不推进；下次还能从同一个 Level 按钮再开
- 设置改音量 / 灵敏度后立即生效；游戏中 / 桌面均生效
- 关闭再开消息窗口：历史完整保留
- 多窗口同时存在（Messages + Settings + Level1）；点哪个置顶哪个；任务栏同步

### 已知坑 / 注意点
- `GameManager.Instance` 现在 per-scene，关卡 unload 后 Instance 短暂为 null。Exit 在 Start 里 `GameManager.Instance.OpenExit.AddListener(Open)`，关卡内顺序没问题；其他脚本若在桌面侧引用 `GameManager.Instance`，要做 null 检查。
- 现有 Exit 是 singleton 模式，多关同时 load 不行（已通过"一次只开一个关卡窗口"约束规避）
- `MessageScheduler.startupDelay` 之前送达的 `AdvanceToNextChapter` 调用已通过 `WaitThenAdvance` 处理，不会丢
- `WindowControls.Initialize()` 里强制 stretch anchor 已修复，但只在被外部调用时执行 — 如果 prefab 已经设好就不需要调
- 任务栏按钮当前对"是否在最顶层"的判断用 sibling index — 桌面 Windows 节点下不要放别的非窗口对象，否则判断不准

---

## 8. 接下来动手第一件事

**最小可见进展路径**（建议）：

```
A.1 → A.2 (Messages + Credits + Settings) → A.3 → A.4 验收 ✓
                ↓
D 写 1 章占位文案（Chapter_01 + 一条带 Level1.exe 的假按钮）
                ↓
B.1 / B.2 / B.3 game window
                ↓
C PlayerController
                ↓
全流程跑一遍 Level1
                ↓
D 补完所有章节 + E 复制 Level2 / Level3
                ↓
F 打磨
```

每跑通一节都先在 Editor 里玩一遍再进下一节，不要憋大招。
