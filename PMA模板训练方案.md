# PMA 模板训练 · 实施方案

> 项目：`D:\visionPor\20260914-资料\04-WinForms\VisionSort`（net48 / x64 / WinForms / VisionPro 9.0 CR2 59.2.0.0）
> 页面：`Views/VisionConfigView.cs` → `grpPma`「PMA模板训练」分组
> 依据文档：`20260914-笔记/联合编程01.md` §八、`README_预览说明.txt` §三.3、`20260909-note/visionPro09.md`
> 版本：v1.5（**已落盘并实测通过**；定稿：保留训练后自检 A 方案 + 采集不作废旧模板 + 预览特征标注修复，见 §13.5/§13.6）
> 状态：源码已实现；实测覆盖 T1/T2/T4/T5/T7/T8/T13 + 预览标注逐像素验证

**变更记录**

| 版本 | 变更 |
|---|---|
| v1.3 | **已落盘 + 实测**：加载器扩展支持 `CogJobManager`/`CogToolGroup`；`Verify()` 自检前钉死输入图；实测记录与遗留问题见 §十三 |
| v1.2 | 决策定稿：**D1**「金字塔层数」改为「**粒度**」（直接写 `GrainLimit*`，不再做层数换算）；**D2** 匹配阈值＝可选值；**D3** 多把 PMA → **下拉可选**；**D4** 工具查找**递归一层**，找不到就提示；D5–D7、D9、D10 按默认。新增 §4.7 取像来源与优先级 |
| v1.1 | 训练区域定型为 **A（沿用方案里已画好的区域）+ B（页面 `CogRecordDisplay` 交互式拖拽微调）**；修正 v1.0 “训练时无条件重建区域会覆盖方案区域”的缺陷；新增 §4.6 / §6.3 |
| v1.0 | 初稿 |

---

## 一、目标与范围

| 项 | 内容 |
|---|---|
| 目标 | 把 `grpPma` 分组做成可用的 PMA 模板训练功能：采集训练图 → 训练模板 → 显示训练区域/验证分数 → 保存模板 |
| 在范围内 | 训练图采集（相机回调 / 方案内取像工具 / 本地图片兜底）、**训练区域：沿用方案已有区域（A）+ 页面交互式拖拽微调（B）**、**多把 PMA 下拉选择**、**粒度（原“金字塔层数”）**、原点设置、运行参数写入、训练与自检、模板存盘、预览显示 |
| 不在范围内 | 相机连接与实时预览（`btnCamConn/btnLive/btnSnap` 属相机分组）、VPP 的加载/保存（已有）、生产运行与结果输出、机器人联动 |
| 关键约束 | ① 训练只作用于**方案里已有的 PMA 工具**（下拉选中的那把），没有就提示，**不新建工具**；② **不覆盖方案里已有的训练区域**；③ 不改动现有已实现功能；④ 训练结果需经『保存方案』写回 VPP 才在生产生效 |

---

## 二、现状基线（已核对）

### 2.1 相关文件

| 文件 | 现状 |
|---|---|
| `Views/VisionConfigView.cs` | 140 行；`_toolBlock` / `_job` / `_vppPath` 三个字段；`btnLoadVpp_Click` 已实现（加载 → 挂 `CogToolBlockEditV2` 到 `pnlVppHost`）；`btnSaveVpp_Click` 已实现；`btnSaveTpl_Click` 是**空方法**（135–138 行）；文件头注释写“阈值只读显示配方值”（本方案按 D2 修正） |
| `Views/VisionConfigView.Designer.cs` | `grpPma` 内控件齐备：`picTemplate`(PictureBox/Zoom) + `flpPma`(btnGrabTpl / btnTrain / btnSaveTpl) + `cmbThreshold` / `cmbAngle` / `cmbPyramid`(标签「金字塔层数」) / `lblTplScore`；事件绑定只有 `btnSaveTpl.Click`，另两个按钮**未绑定** |
| `VisionSort.csproj` | SDK 风格，Cognex 程序集（含 `Cognex.VisionPro.PMAlign`、`.ImageFile`、`.ImageProcessing`、`.Controls`、`.Display.Controls`）已引用 → **新增 .cs 无需改 csproj** |
| 相机层 | 尚未实现（`btnCamConn/btnLive/btnSnap` 无事件）→ 采集训练图必须带兜底路径 |

### 2.2 已核对的 VisionPro API（来源：`D:\cognex\VisionPro\ReferencedAssemblies\*.xml`）

| 用途 | 成员 | 出处 |
|---|---|---|
| 模式训练 | `CogPMAlignPattern.TrainImage / TrainRegion / TrainRegionMode / Origin / TrainAlgorithm / TrainMode / Train() / Trained / GetInfoStrings()` | `Cognex.VisionPro.PMAlign.xml` |
| **粒度（D1）** | `GrainLimitAutoSelect / GrainLimitCoarse / GrainLimitFine`（默认 4.0 / 1.0；范围 1.0–25.5） | 同上 |
| 运行参数 | `CogPMAlignRunParams.RunAlgorithm / AcceptThreshold / ApproximateNumberToFind / ZoneAngle.{Configuration,Low,High}` | 同上 |
| 结果 | `CogPMAlignResults.Count / 索引器`、`CogPMAlignResult.Score / Accepted / GetPose()` | 同上、`visionPro07.md` |
| 区域 | `CogRectangleAffine.FitToImage(ICogImage,double,double)`、`CogCircularAnnulusSection.FitToImage / AngleStart / AngleSpan / CenterX / CenterY / Radius`、`*DOFConstants.All`、`Interactive` | `Cognex.VisionPro.Core.xml` |
| 交互式区域（B） | `CogDisplay.InteractiveGraphics.Add(ICogGraphicInteractive, string, bool)`、`CogDisplay.MouseMode`、`CogDisplayMouseModeConstants.Pointer`、`CogRectangleAffine.DraggingStopped` 事件 | `Cognex.VisionPro.Display.Controls.xml`、`Cognex.VisionPro.Core.xml` |
| 灰度转换 | `CogImageConvertTool` + `CogImageConvertRunModeConstants.Intensity` | `Cognex.VisionPro.ImageProcessing.xml` |
| 读图片 | `CogImageFileTool.Operator.Open(path, CogImageFileModeConstants.Read)` → `Run()` → `OutputImage` | `Cognex.VisionPro.ImageFile.xml`、`visionPro09.md` |
| 取像工具 | `CogAcqFifoTool.Operator / Run() / OutputImage` | `Cognex.VisionPro.Core.xml` |
| **工具容器（D3/D4）** | `CogToolBlock.Tools`、`CogToolGroup.Tools`（同为 `CogToolCollection`，可嵌套子容器）、`CogToolCollection.Add(ICogTool)`、`foreach (ICogTool t in block.Tools)` | `ToolGroup.xml`、`visionPro06.md` |
| 记录/显示 | `ICogTool.CreateCurrentRecord()`、`CreateGraphicsFine(CogColorConstants)`、`CogStaticGraphicsContainer.AddList(...)`、`CogDisplay.Fit(bool)` | `Cognex.VisionPro.Core.xml`、`.Display.Controls.xml` |
| 显示控件 | `CogRecordDisplay`（位于 `Cognex.VisionPro.Controls.dll`，已引用） | 二进制核对 + `联合编程01.md` §四 |

---

## 三、总体设计

### 3.1 文件分工（一个 partial 拆三份，沿用现项目风格）

| 文件 | 类型 | 职责 |
|---|---|---|
| `Views/VisionConfigView.cs` | 现有 | VPP 加载/保存（仅删掉空 `btnSaveTpl_Click`；修正阈值注释） |
| `Views/VisionConfigView.Designer.cs` | 现有 | 控件布局（补 2 行事件绑定；`picTemplate` 换 `CogRecordDisplay`；「金字塔层数」改「粒度」；新增 PMA 工具下拉） |
| **`Views/VisionConfigView.Pma.cs`** | **新增** | PMA 分组的界面调度：取图 → 区域（沿用/拖拽） → 训练 → 预览 → 存盘 → 提示 |
| **`Services/PmaTemplateTrainer.cs`** | **新增** | 纯 VisionPro 逻辑，无 UI 依赖，可被主运行页复用 |

### 3.2 类型结构（新增）

```
VisionSort.Services
├── enum PmaTrainRegionShape { RectangleAffine, CircularAnnulusSection }
├── enum PmaGrainPreset { Auto, Coarse, Standard, Fine }     // D1：界面就叫「粒度」，不再做层数换算
├── sealed class PmaTrainSettings          // 界面参数 DTO
│     RegionShape=RectangleAffine, RegionScale=0.8,
│     AngleLowDeg=-180, AngleHighDeg=180,
│     AcceptThreshold=0.70,                  // D2：可选值（下拉选中即用）
│     ApproximateNumberToFind=1,
│     GrainAutoSelect=true, GrainPreset=PmaGrainPreset.Standard
├── sealed class PmaTrainResult            // 训练/自检结果
│     ToolName, Trained, MatchCount, AcceptedCount, BestScore/BestX/BestY/BestAngle,
│     Diagnostics, Summary, ScoreText
└── sealed class PmaTemplateTrainer        // 训练器（作用于下拉选中的那把工具）
      ctor(CogPMAlignTool)
      FindTool(IEnumerable, preferredName, nestDepth=1)   // 静态：递归一层找 PMA（D4）
      ListPmaTools(IEnumerable, nestDepth=1)              // 静态：列出所有 PMA（D3，含一层子容器）
      SetTrainImage(ICogImage)              // 转灰度 + 写 TrainImage/InputImage
      CurrentTrainRegion                    // 本页设过的 → 否则方案自带（A 的基础）
      BuildTrainRegion(PmaTrainSettings)    // 居中矩形/圆环（仅方案没有区域时用）
      ResetTrainRegion(PmaTrainSettings)    // 显式重设（可选，见 D9）
      ApplyTrainRegion(ICogRegion, bool)    // 写区域 + 可选设原点
      ApplyOriginFromRegion(ICogRegion)     // 原点 = 区域中心
      ApplyRunParams(PmaTrainSettings)      // 阈值/角度/个数/粒度
      Train(PmaTrainSettings)               // 训练 + 自检（区域：沿用 → 无则生成）
      Verify()                              // Run() 一次，汇总最高分
      SavePattern(string)                   // .pat 存档
      static EnsureGreyImage(ICogImage)
      static LoadImageFromFile(string)
```

### 3.3 界面类新增成员（`VisionConfigView` 的 partial）

| 成员 | 说明 |
|---|---|
| `Func<ICogImage> TrainingImageProvider` | 相机层注入的取像回调（见 §4.7） |
| `CogRecordDisplay TemplateDisplay` | **B 方案的前提**：`picTemplate` 换成 `CogRecordDisplay` 后挂上；未挂时退化为“只能看不能拖” |
| `void AttachInteractiveTrainRegion()` | 把 `CurrentTrainRegion` 挂到 `InteractiveGraphics`，鼠标拖拽微调（见 §6.3） |
| `void RefreshPmaToolList()` | D3：扫描方案（递归一层）把 PMA 名字填进 `cmbPmaTool`，尽量保持原选中项 |
| `string SelectedPmaToolName` | D3：`cmbPmaTool` 当前选中名，作为 `FindTool` 的 `preferredName` |
| `void SyncPmaFromScheme()` | 方案加载完成后调用（可选一行）：刷新工具下拉 + 同步显示方案里的模板 |
| `_pmaSettings / _pmaTrainer / _trainImage / _pmaBusy` | 参数、训练器、当前训练图、防重入 |

---

## 四、关键流程

### 4.1 工具解析（三个按钮共用，D3 + D4）

```
ResolveTrainer(showTip)
  ├─ 容器来源：_toolBlock（ToolBlock）或 _job.VisionTool as CogToolGroup
  ├─ preferredName = cmbPmaTool 选中项（默认 CogPMAlignTool1；下拉为空时先 RefreshPmaToolList()）
  ├─ FindTool(容器.Tools, preferredName, nestDepth: 1)
  │    ├─ 先扫本层：名字匹配优先，否则记第一个 PMA
  │    └─ 本层没有 → 对子容器（CogToolGroup / CogToolBlock）的 Tools 再扫一层，然后停止（D4：最多一层）
  ├─ 命中 → 复用/新建 PmaTemplateTrainer(那把工具) → 返回
  └─ 未命中 → showTip=true 时提示：
         · 未加载方案        → “请先『加载VPP方案』”
         · 方案里没有 PMA    → “请在 QuickBuild 里把 PMA 加进方案后重新加载”
         · 下拉选的名字失效  → “选中的 PMA 已不在当前方案，已刷新下拉，请重新选择”
```

**决策（已确认）**：不新建 `CogPMAlignTool`，不写 `Tools.Add`。

### 4.2 『采集训练图』（btnGrabTpl）

```
ResolveTrainer(true) → null 就退出
取图（优先级，详见 §4.7）：
  1) TrainingImageProvider?.Invoke()   // 相机层注入（相机在 WinForms 侧时用）
  2) 方案里的 CogAcqFifoTool → Run() → OutputImage   // = QuickBuild 的 Image Source
  3) OpenFileDialog 选图片 → CogImageFileTool 读取   // 离线兜底
SetTrainImage(image)             // 非 8 位灰度 → CogImageConvertTool(Intensity) 转灰度
ApplySettingsFromUi()            // 读 cmbAngle / cmbThreshold(D2:可选值) / cmbGranularity(D1:粒度)
EnsureTrainRegion(settings)      // A：方案已有区域 → 沿用；没有 → 才生成居中默认区域
AttachInteractiveTrainRegion()   // B：区域挂 InteractiveGraphics，可鼠标拖拽
RenderPreview()                  // 图像 + 精细特征（结果图形）+ 可拖拽区域
lblTplScore = "验证分数：--（已采集训练图，点『训练模板』开始训练）"
```

### 4.3 『训练模板』（btnTrain）

```
ResolveTrainer(true)
若 _trainImage 为空 → 取 Pattern.TrainImage（方案自带模板可直接重训）
若仍无训练图 → 提示"先采集训练图"并退出

ApplyRunParams()                                                        // 阈值(D2) / 角度区间 / 个数 / 粒度(D1)
ICogRegion region = CurrentTrainRegion ?? BuildTrainRegion(settings);   // A：优先沿用方案里的区域
ApplyTrainRegion(region, resetOrigin: CurrentTrainRegion == null);      // 只有"自动生成"时才重设原点
Pattern.TrainAlgorithm = PatMaxAndPatQuick
Pattern.TrainMode      = Image
Pattern.Train()                       // 失败抛异常 → 友好提示
Run() → 汇总 Results → PmaTrainResult
AttachInteractiveTrainRegion() ; RenderPreview()
lblTplScore = result.ScoreText ; MessageBox(result.Summary)
```

### 4.4 『保存模板』（btnSaveTpl）（D5 按默认）

```
ResolveTrainer(true) → 未训练则提示
SaveFileDialog（默认目录 <StartupPath>\templates，默认名 PMA模板_<工具名>_<时间戳>.pat）
CogSerializer.SaveObjectToFile(Pattern, 路径)
提示：要让生产方案用上它，请再点『保存方案』把 VPP 写回
```

### 4.5 方案加载后同步（可选增强）

```
btnLoadVpp_Click 末尾追加 SyncPmaFromScheme();
  → 静默 ResolveTrainer(false)
  → RefreshPmaToolList()                        // D3：重建工具下拉，优先保持原选中项
  → 有 PMA 且模板带 TrainImage：显示到预览框 + 挂可拖拽区域；否则清空预览
  → 不弹窗、不改方案里的任何参数
```

### 4.6 训练区域的获取规则（A+B，已定稿）

| 顺序 | 规则 | 说明 |
|---|---|---|
| **A1** | 方案里已有 `Pattern.TrainRegion` → **原样沿用** | 区域随 VPP 一起序列化；在 QuickBuild 里画一次，长期复用，WinForms 只换图重训 |
| **A2** | 方案里没有区域 → 自动生成居中默认区域（`FitToImage(0.8)` 矩形/圆环）并设原点 | 首件 / 新工件用，再用 B 拖到想要的位置 |
| **B** | 区域挂到 `CogRecordDisplay.InteractiveGraphics` → 鼠标拖拽把手（位置/尺寸/旋转；圆环为内外径与角度） | 拖完点『训练模板』即时重训；`picTemplate` 仍是 `PictureBox` 时只能看不能拖 |
| **禁止** | 训练时**无条件** `ApplyTrainRegion(BuildTrainRegion(...), true)` | 会覆盖 QuickBuild 里画好的区域（v1.0 的缺陷，v1.1 已修正） |

判定代码（`PmaTemplateTrainer.Train` 内）：

```csharp
ICogRegion region = CurrentTrainRegion ?? BuildTrainRegion(settings);
ApplyTrainRegion(region, resetOrigin: CurrentTrainRegion == null);
```

> 判定依据是“区域存不存在”，不是“图换没换”。换图后区域沿用属预期行为（相机固定、工件同规格）；
> 若新工件位置/尺寸变化，用户用 B 拖一下即可。

### 4.7 取像来源与优先级（谁持有相机）

| 场景 | 相机谁持有 | 走哪条 | 说明 |
|---|---|---|---|
| A. 相机配在 vpp 的 Image Source 里 | `CogAcqFifoTool.Operator` | 第 2 条 | QuickBuild 的「图像源」在 .NET 侧就是方案里的 `CogAcqFifoTool`：`acq.Run()` → `acq.OutputImage` |
| B. 相机在我们自己的相机层（主运行页 live/snap） | 我们的 `ICogAcqFifo` | 第 1 条 | `TrainingImageProvider` 返回最近一帧；**训练页借图，不抢相机**（GigE 同一时刻基本只允许一个 FIFO 占用） |
| C. vpp 有相机但 `Operator` 被外部赋值（联合编程01 §三 例3） | 我们的 FIFO | 第 1 条 | 同上；不要重复 `Run()`，避免和主运行页抢帧 |
| D. 没相机 / 离线调试 | — | 第 3 条 | 选本地图片（**本轮验收就靠它**，相机层尚未实现） |

**D10（按默认：简单版）**：半自动（Semi）触发时 `Func<ICogImage>` 表达不了“等触发”——provider 返回 `null`，页面提示“请在相机分组里触发一次拍照后再点采集”。事件式（`RequestFrame()` + `FrameArrived`，按钮显示“等待触发…”）留作后续扩展。

---

## 五、界面参数 → API 映射表

| 控件 | 取值 | 写入 | 状态 |
|---|---|---|---|
| `cmbAngle` | ±180°/±90°/±30°/±10° | `RunParams.ZoneAngle.{Configuration=LowHigh, Low, High}` | 定稿 |
| `cmbThreshold` | 0.50~0.80 | `RunParams.AcceptThreshold` | **D2 定稿：可选值**，下拉选中即用（不选则默认 0.70） |
| `cmbGranularity`（原 `cmbPyramid`） | 自动/粗/标准/细 | `Pattern.GrainLimitAutoSelect` + `GrainLimitCoarse/Fine` | **D1 定稿：改为粒度**，见 5.1 |
| `cmbPmaTool`（新增） | 方案里所有 PMA 名字 | `FindTool(..., preferredName)` 的选中项 | **D3 定稿：下拉可选** |
| （未放置控件） | — | `RunParams.ApproximateNumberToFind = 1` | 定稿（单件流） |
| （未放置控件） | — | `RunParams.ZoneScale` **不动** | 定稿（相机固定、工件同规格） |
| （方案自带 / 拖拽） | — | `Pattern.TrainRegion`（**沿用，不覆盖**） | 定稿（A 主路径） |
| （未放置控件） | — | `PmaTrainSettings.RegionShape/RegionScale` | 定稿（仅“方案无区域自动生成”时用；默认矩形 / 0.8） |
| `lblTplScore` | 只显示 | `PmaTrainResult.ScoreText` | 定稿（D6 自检口径） |
| `picTemplate` / `TemplateDisplay` | 显示 + 拖拽 | `ToBitmap()`（+GDI 描区域，只看）/ `CogRecordDisplay` + `InteractiveGraphics`（可拖） | 见第六节 |

### 5.1 粒度（D1 定稿）

下拉语义就是 VisionPro 的**粒度限制**，不再做“金字塔层数 → 粒度”的换算；数值全部来自官方默认值与官方示例：

| 下拉项 | `GrainLimitAutoSelect` | `GrainLimitCoarse` | `GrainLimitFine` | 说明 |
|---|---|---|---|---|
| **自动**（默认） | `true` | — | — | 训练时由 VisionPro 自选粒度 |
| 粗 | `false` | 6.1 | 1.5 | 只用较大特征：最快、抗噪，适合特征大而明显的工件（官方示例值） |
| 标准 | `false` | 4.0 | 1.0 | VisionPro 默认量级（XML 默认值） |
| 细 | `false` | 2.0 | 1.0 | 允许更小特征参与：最稳、最慢 |

> 写入顺序固定为**先粗后细**（XML：`GrainLimitCoarse` 必须 ≥ `GrainLimitFine`）。
> 后续如需数值细调，再加两个 `NumericUpDown` 直接绑 `GrainLimitCoarse/Fine`（见 §11.2）。

---

## 六、预览与显示

### 6.1 降级路径：`picTemplate` 仍是 `PictureBox`

```
image.ToBitmap() → 24bpp 画布 → DrawImage
                 → GDI 描训练区域（CogRectangleAffine 四角 / CogCircularAnnulusSection 外圆）
                 → picTemplate.Image = bmp（旧的 Bitmap 要 Dispose）
```

**只能看、不能拖**——仅作为 `CogRecordDisplay` 未接入时的过渡显示。

### 6.2 主路径（README §三.3）：换成 `CogRecordDisplay`

```
TemplateDisplay.Image = image;  TemplateDisplay.Fit();
TemplateDisplay.StaticGraphics.Clear();      // 精细特征、自检结果（只显示）
已训练 → StaticGraphics.AddList(Pattern.CreateGraphicsFine(Purple), "精细模板")
TemplateDisplay.Record = Tool.CreateCurrentRecord();
AttachInteractiveTrainRegion();              // 训练区域（可拖拽，见 6.3）
```

两条路径由 `TemplateDisplay != null` 自动切换；`picTemplate` 换成 `CogRecordDisplay` 后，6.1 整段可删。

### 6.3 交互式训练区域（B 的实现）

```csharp
using Cognex.VisionPro.Display;

/// <summary>B：把训练区域挂到 CogRecordDisplay 上，供鼠标拖拽微调（所见即所得）。</summary>
private void AttachInteractiveTrainRegion()
{
    if (TemplateDisplay == null) return;   // 还是 PictureBox：只能看，不能拖（降级路径）

    // 不设 Pointer 的话鼠标模式是平移，区域把手拖不动
    TemplateDisplay.MouseMode = CogDisplayMouseModeConstants.Pointer;
    TemplateDisplay.InteractiveGraphics.Clear();   // 同一图形重复加入容器行为未定义，必须先清

    ICogGraphicInteractive graphic = _pmaTrainer?.CurrentTrainRegion as ICogGraphicInteractive;
    if (graphic == null) return;

    // Add(图形, 分组名, 是否查重)：查重 false 性能更好，但同一图形只能加一次
    TemplateDisplay.InteractiveGraphics.Add(graphic, TrainRegionGroup, false);

    // 可选（D9 按默认：不做自动重训）
    // if (graphic is CogRectangleAffine rect)
    //     rect.DraggingStopped += (s, e) => TrainAndRefresh();
}
```

区域能被拖的前提是图形自身已打开自由度——`BuildTrainRegion` 里已设：

```csharp
rect.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;   // 位置/尺寸/旋转/斜切把手
rect.Interactive      = true;
```

要点：

1. 挂进去的图形**必须就是 `Pattern.TrainRegion` 那个实例**（同一个对象），否则拖了不生效；
2. 拖完点『训练模板』即重训（复用现有按钮，不引入新事件）；
3. 精细特征、自检结果仍走 `StaticGraphics`（只显示），两者别混用。

---

## 七、对现有文件的改动清单

| # | 文件 | 改动 | 必需性 |
|---|---|---|---|
| 1 | `Views/VisionConfigView.cs` | 删除 135–138 行空方法 `btnSaveTpl_Click`（新文件已实现，否则 CS0111 重名） | **必需** |
| 2 | `Views/VisionConfigView.Designer.cs` | `btnGrabTpl` 段内追加 `this.btnGrabTpl.Click += new System.EventHandler(this.btnGrabTpl_Click);` | **必需** |
| 3 | `Views/VisionConfigView.Designer.cs` | `btnTrain` 段内追加 `this.btnTrain.Click += new System.EventHandler(this.btnTrain_Click);` | **必需** |
| 4 | `Views/VisionConfigView.Designer.cs` | `picTemplate` 由 `PictureBox` 换 `CogRecordDisplay`（`grpPma` 左半区、`Dock=Left`、`SizeMode` 相关属性删除），并把控件赋给 `TemplateDisplay` | **必需（B 的前提）** |
| 5 | `Views/VisionConfigView.Designer.cs` | **D1**：`lblPyr.Text` 改「粒度」；`cmbPyramid` 改名 `cmbGranularity`，`Items` 改为 `自动 / 粗 / 标准 / 细`，`SelectedIndex = 0` | **必需** |
| 6 | `Views/VisionConfigView.Designer.cs` | **D3**：新增 `pnlPmaTool` + `lblPmaTool`（「模板工具」）+ `cmbPmaTool`（`DropDownList`），插在 `flpPma` 之后、`pnlThr` 之前（高 36，`pnlPmaRight` 总高 88+36×4+28 = 260 ≤ 278） | **必需** |
| 7 | `Views/VisionConfigView.cs` | 文件头注释里“阈值只读显示配方值（见技术文档§8）”改为“阈值可下拉选择，选中值写入 PMA 运行参数（技术文档§8 的配方值仅作默认显示）” | 可选（对齐 D2） |
| 8 | `Views/VisionConfigView.cs` | `btnLoadVpp_Click` 末尾（`lblVppTip.Visible = false;` 之后）追加 `SyncPmaFromScheme();` | 可选 |
| 9 | `Views/VisionConfigView.Designer.cs` + `.Pma.cs` | 可选：加 `btnResetRegion`「重设区域」按钮（D9 按默认＝不加） | 可选 |
| 10 | `VisionSort.csproj` | 无需改动（SDK 风格自动包含新文件） | — |

---

## 八、决策记录（全部关闭）

| 编号 | 问题 | 结论 | 落地位置 |
|---|---|---|---|
| D1 | 「金字塔层数」与 API 对不上（无公开 Pyramid 成员） | **改为「粒度」**：下拉＝自动/粗/标准/细，直接写 `GrainLimitAutoSelect/Coarse/Fine`；默认「自动」 | §5.1、改控件标签与选项（§7-5） |
| D2 | `cmbThreshold` 是可选值还是配方只读值 | **可选值**：下拉选中即写入 `AcceptThreshold`；未选/解析失败用默认 0.70 | §五、§7-7 注释修正 |
| D3 | 多把 PMA 时如何选工具 | **下拉可选**：新增 `cmbPmaTool`，列出方案里所有 PMA（含一层子容器，子容器内同名工具显示为 `容器名/工具名`），默认选 `CogPMAlignTool1`（不存在则第一项） | §3.3、§4.1、§4.5、§7-6 |
| D4 | PMA 藏在嵌套容器里 | **递归一层**：先顶层，再扫每个子容器（`CogToolGroup` / `CogToolBlock`）的 `Tools`，到此为止；仍找不到 → 提示 | §4.1、§9-T17 |
| D5 | 『保存模板』语义 | 按默认：存模式对象 `.pat`，并提示再『保存方案』写回 VPP | §4.4 |
| D6 | “验证分数”口径 | 按默认：训练后用同一张训练图自检（≈1.0，只证明模板可用） | §4.3 |
| D7 | 训练后是否自动写回 VPP | 按默认：不自动，提示手动点『保存方案』 | §4.3/§4.4 |
| ~~D8~~ | 训练区域轮廓怎么获取 | **已定稿（v1.1）：A+B** —— 方案已有区域→沿用；页面 `CogRecordDisplay` 拖拽微调；方案无区域才自动生成 | §4.6、§6.3 |
| D9 | 要不要「重设区域」按钮 / 拖拽后自动重训 | 按默认：不加按钮、不自动重训；方案无区域时自动生成，调整靠拖拽（拖完点『训练模板』） | §4.6、§6.3 |
| D10 | 半自动触发下『采集训练图』怎么等图 | 按默认（简单版）：provider 返回 `null` → 提示“请先触发一次拍照”；事件式留作后续扩展 | §4.7 |

---

## 九、验收 / 自测清单

| # | 场景 | 期望 |
|---|---|---|
| T1 | 编译（x64 / net48） | 0 error；新文件被 SDK 自动纳入 |
| T2 | 未加载 VPP 点『采集训练图』 | 提示“还没有加载 VPP 方案…”，不做任何事，无异常 |
| T3 | 加载不含 PMA 的 VPP 后点三个按钮 | 提示“当前方案里没有 PMA 模板工具…”，`Tools` 数量不变（不新建） |
| T4 | 加载含 PMA 的 VPP，用本地图片采集 | 预览框出现图 + 训练区域轮廓；`lblTplScore` 提示待训练 |
| T5 | 点『训练模板』 | 弹摘要（分数/位置/角度）；`lblTplScore` 显示 ≥1.000 量级；`Pattern.Trained == true` |
| T6 | 训练区域故意选在空白区 | 捕获 `CanNotTrain` 类异常，提示“区域里没有足够特征…” |
| T7 | 点『保存模板』 | 在 `templates\` 生成 `.pat`；随后用 `CogSerializer.LoadObjectFromFile` 能读回 `CogPMAlignPattern` |
| T8 | 训练后『保存方案』→ 重新加载 VPP | PMA 工具为已训练状态，模板与区域随方案恢复 |
| T9 | **D1**：粒度下拉切「粗 / 标准 / 细」→ 重训 | `GrainLimitAutoSelect=false`，且 `GrainLimitCoarse/Fine` 分别等于 6.1/1.5、4.0/1.0、2.0/1.0；切回「自动」→ `AutoSelect=true` |
| T10 | 连续快速点击按钮 | `_pmaBusy` 生效，不重入、不叠加等待光标 |
| T11 | 载入方案后调用 `SyncPmaFromScheme()` | 预览显示方案模板训练图，且**不修改**方案里的区域/参数 |
| T12 | 主窗体 5 个 Tab 切换、设计器预览 | 无异常；未改动的页面行为不变 |
| T13 | 方案里已画好区域 → 采集另一张图 → 训练 | `Pattern.TrainRegion` 与方案里的一致（**未被覆盖**），原点未被重设 |
| T14 | 拖动区域把手 → 点『训练模板』 | 区域几何随拖动改变；自检分数/位置随之变化；`Pattern.TrainRegion` 仍是同一实例 |
| T15 | 未接 `CogRecordDisplay`（仍是 PictureBox） | 区域能显示但不能拖；不报错、不抛异常（降级路径可用） |
| **T16** | **D3**：方案里有 2 把以上 PMA | `cmbPmaTool` 列出全部名字；切换后『采集/训练/保存』作用于**选中那把**；未选中的工具 `Trained`/参数不变 |
| **T17** | **D4**：PMA 只在子容器（一层）内 | 能被找到并可训练；若埋在两层的容器深处 → 提示找不到（`Tools` 数量不变） |
| **T18** | **D3**：换一个 VPP 后下拉里旧名字失效 | `RefreshPmaToolList()` 重建列表并尽量保持选中项；动作前解析失败 → 提示“选中的 PMA 已不在当前方案” |
| **T19** | **D2**：阈值下拉选 0.85 | `RunParams.AcceptThreshold == 0.85`；清空选择 → 回到 0.70 |

---

## 十、风险与规避

| 风险 | 影响 | 规避 |
|---|---|---|
| 训练时无条件重建区域 → 覆盖方案区域（v1.0 缺陷） | QuickBuild 里画好的区域丢失 | §4.6 规则 `CurrentTrainRegion ?? BuildTrainRegion(...)`；用 T13 兜住 |
| `Pattern.TrainImage/TrainRegion/Origin/GrainLimit` 赋值即作废旧训练 | 顺序错则训练失效 | 训练器内固定顺序：设图 → 参数/区域 → `Train()`；粒度写入固定“先粗后细” |
| `MouseMode` 不是 `Pointer` | 区域把手拖不动 | `AttachInteractiveTrainRegion()` 内强制设 `CogDisplayMouseModeConstants.Pointer` |
| 同一区域图形重复加入 `InteractiveGraphics` | XML 明确警告“行为未定义” | 每次 `Clear()` 后再 `Add`；挂的实例与 `Pattern.TrainRegion` 相同 |
| 下拉选中项与当前方案不同步（换 VPP / 方案被改） | 训练到错误的工具或找不到 | 每次动作前按选中名重新解析；失败即 `RefreshPmaToolList()` + 提示（T18） |
| 控件改名（`cmbPyramid` → `cmbGranularity`）漏改引用 | 编译错误 | 方案里列出改名点；`InitializeComponent` 与 `.Pma.cs` 两处同步改，编译即暴露 |
| 一层递归仍可能找到“非目标工位”的 PMA | 训练错工具 | 下拉里能看到并切换；子容器内工具带 `容器名/` 前缀区分（T16/T17） |
| 彩色图直接训练会失败 | T4 失败 | `EnsureGreyImage()` 统一走 `CogImageConvertTool(Intensity)` |
| `CogRecordDisplay.Fit()` 重载差异 | 编译风险 | 若报错改 `Fit(true)`（`CogDisplay.Fit(bool)` 已在 XML 核对） |
| 取像工具 `OutputImage` 为空 / `Operator` 未配置 | 采集无图、`Run()` 抛异常 | 取图后判空并提示，回退本地图片；提示去相机分组配相机 |
| 相机层尚未实现 | 真机路径暂时不可用 | `TrainingImageProvider` 预留注入点，本轮用本地图片验收 |
| 操作大图（如 500 万像素）时 UI 卡顿 | 体验 | 训练/采集期间 `Cursor.WaitCursor` + `_pmaBusy`；后续可加进度提示 |

---

## 十一、实施步骤与后续扩展

### 11.1 实施顺序

1. 新增 `Services/PmaTemplateTrainer.cs`（含 §4.6 区域规则、D1 粒度、D3/D4 工具查找，纯逻辑先编译过）
2. `Views/VisionConfigView.Designer.cs`：D1 改标签/选项（`cmbGranularity`）+ D3 新增 `cmbPmaTool` 行 + 2 行事件绑定 + `picTemplate` 换 `CogRecordDisplay`
3. 新增 `Views/VisionConfigView.Pma.cs`（三个按钮 + `RefreshPmaToolList` + `EnsureTrainRegion` + `AttachInteractiveTrainRegion`）
4. `Views/VisionConfigView.cs`：删空方法、修正阈值注释、可选加 `SyncPmaFromScheme()`
5. 编译 → 按第九节跑 T1–T19（本轮以本地图片路径为主）
6. 可选：`btnResetRegion`（D9）、事件式取像（D10）

### 11.2 后续可扩展

- 主运行页（`MainRunView`）复用 `PmaTemplateTrainer.FindTool + Verify`，或直接调用方案 `Run()` 得到分数
- 配方化：`PmaTrainSettings` + 区域几何（中心/宽高/角度）+ 粒度预设序列化为 JSON，随配方下拉一起切换
- 多套区域模板：按工件型号保存/加载区域，切换配方时自动套用
- 粒度数值细调：加两个 `NumericUpDown` 直接绑 `GrainLimitCoarse/Fine`
- 复杂调参兜底：加「高级…」按钮弹 `CogPMAlignEditV2`（原生编辑器，区域/掩膜/极性全在）
- 事件式取像：`RequestFrame()` + `FrameArrived`，支撑半自动（Semi）触发

---

## 十二、落盘清单

决策已全部关闭（§八），按下表落盘：

| 动作 | 目标文件 |
|---|---|
| 新增 | `VisionSort/Services/PmaTemplateTrainer.cs` |
| 新增 | `VisionSort/Views/VisionConfigView.Pma.cs` |
| 改 | `VisionSort/Views/VisionConfigView.cs`：删 4 行空方法；修正阈值注释；（可选）加 1 行 `SyncPmaFromScheme()` |
| 改 | `VisionSort/Views/VisionConfigView.Designer.cs`：补 2 行事件绑定；`picTemplate` 换 `CogRecordDisplay`；「金字塔层数」→「粒度」（`cmbGranularity`）；新增 `cmbPmaTool` 一行 |

> 源码落盘前我会先把两个新文件贴给你审阅（沿用你“先看代码、再改文件”的要求）。

---

## 十三、实测记录（2026-09-15，v1.3 追加）

### 13.1 落盘结果

| 文件 | 动作 | 说明 |
|---|---|---|
| `Services/PmaTemplateTrainer.cs` | 新增 | 训练逻辑（粒度 D1、工具查找 D3/D4、区域 A+B、自检、存盘） |
| `Views/VisionConfigView.Pma.cs` | 新增 | 界面调度（三个按钮、工具下拉、拖拽区域、预览） |
| `Views/VisionConfigView.cs` | 改 | 删 3 个空存根；修正阈值注释；`btnLoadVpp_Click` 末尾 `SyncPmaFromScheme()`；**加载器扩展**（见 13.2） |
| `Views/VisionConfigView.Designer.cs` | 改 | 「粒度」下拉；「模板工具」下拉；`picTemplate` → `CogRecordDisplay`（含 VS 生成的 `OcxState`） |

编译：`dotnet build` 会因 `VisionConfigView.resx` 里的 `picTemplate.OcxState`（非字符串资源）报 MSB3823；
**请用 VS 或 VS 的 MSBuild 编译**（同一工具链实测通过，0 warning / 0 error）。

### 13.2 与方案的实现偏差（3 处，均已落地）

| # | 偏差 | 原因 | 处理 |
|---|---|---|---|
| 1 | 加载器新增支持 **`CogJobManager`** 与 **`CogToolGroup`** | 用户提供的 `QuickBuild1.vpp` 根节点是 `CogJobManager`（QuickBuild 工程文件），`VisionTool` 是 `CogToolGroup`；原加载器只认 `CogToolBlock`/`CogJob` → 报“加载失败” | `btnLoadVpp_Click` 支持三种形态；`pnlVppHost` 按类型挂 `CogToolBlockEditV2` / `CogToolGroupEditV2`；保存时按加载形态写回 |
| 2 | `Verify()` 里自检前**重设 `Tool.InputImage = 训练图`** | 方案的 PMA 在 `CogToolBlock` 里被数据绑定（Image Source → Convert → PMA），块内跑过之后会覆盖 `InputImage`；实测**第二次训练**曾报“自检找不到匹配” | `Verify()` 开头钉死输入图；复测连续两次训练结果一致（1 个匹配 / 0.895） |
| 3 | 去掉 `TemplateDisplay` 双路径属性 | 本轮直接把 `picTemplate` 换成 `CogRecordDisplay`，无需 GDI 降级分支（§6.1 已说明可删） | T15 不再适用 |

补充：`CogRecordDisplay` 的真实命名空间是 **`Cognex.VisionPro`**（`Cognex.VisionPro.Controls.dll`），`Fit` 只有 `Fit(Boolean)` 重载。

### 13.3 实测结果（vpp：`bin\x64\Debug\net48\vpps\QuickBuild1.vpp`，训练图：`20260908-素材\齿轮缺齿检测\2.bmp`）

| 项 | 结果 |
|---|---|
| T1 编译 | ✅ 0 error / 0 warning（VS MSBuild amd64） |
| T2 未加载方案点『采集训练图』 | ✅ 提示“还没有加载 VPP 方案…” |
| 加载含 PMA 的 vpp | ✅ 路径正常显示；`模板工具 = CogToolBlock1/CogPMAlignTool1`（**D4 递归一层正好命中**） |
| T4 采集训练图 | ✅ 方案 Image Source 是 `CogInputImageTool`（无相机）→ 自动回退本地图片；区域按 **A 规则沿用**方案里的 `CogRectangleAffine` |
| T5 训练模板 | ✅ 摘要：**找到 1 个匹配（Accepted 1），最高分 0.895，角度 0.00°**；标签 `验证分数：0.895（自检，角度 0.00°）`；训练诊断信息也带出 |
| 连续第二次训练 | ✅ 结果与首次完全一致（偏差 2 的复测） |
| T7 保存模板 | ✅ 生成 `_tmp\PMA模板_测试.pat`（1.31 MB），提示“请再点『保存方案』把 VPP 写回” |
| T8 保存方案 → 重新加载 | ✅ `vpps\QuickBuild1_训练后.vpp`（1.0 MB → 2.3 MB）；重载后标签显示“方案里的模板已训练” |
| T13 区域不被覆盖 | ✅ 探针核验：`TrainRegion` 仍是方案原 `CogRectangleAffine` |
| 探针对比（独立核验） | 原始：`Trained=False / TrainImage=无 / AcceptThreshold=0.5`；训练后：`Trained=True / TrainImage=有 / AcceptThreshold=0.7` |

### 13.4 已知限制 / 后续建议

1. **`CogInputImageTool` 不自动取像**：这份方案的“Image Source”是输入图像工具（生产时由外部喂图），训练图只能走本地图片或相机回调；如需“从方案 Image Source 取当前图”，要按 `ToolBlock.Inputs["InputImage"]` 的数据绑定去喂（联合编程01 §三 例1 的写法），可作为后续增强。
2. **生产验证另用一张图**：0.895 是**训练图自检**分（D6 口径），不代表生产分数；上线前请用另一张工件图复核。
3. **VS 设计器会重排/重写 Designer**：实测 VS 保存窗体时把 `flpPma` 的按钮顺序改成了“采集/保存/训练”，并丢掉了 `cmbGranularity.SelectedIndex = 0`（已修复回“采集/训练/保存”+默认选中“自动”）。若之后再进设计器保存，请复查这两处与 `picTemplate.OcxState`。
4. **异步取像（D10）** 仍是简单版：半自动触发时 provider 返回 null 会提示先触发。
5. 测试产生的文件（可删）：`VisionSort\bin\x64\Debug\net48\vpps\QuickBuild1_训练后.vpp`、`_tmp\PMA模板_测试.pat`、`_tmp\vpp_probe*.exe`。

### 13.5 定稿：保留训练后自检（A 方案）+ 采集与训练图解耦（v1.4）

**决策**：训练完**保留一次自检**（`Train()` → `Verify()`），操作员立刻能看到量化分数；同时把“喂图”和“设为训练图”拆开。

**代码变更**

| 变更 | 说明 |
|---|---|
| 新增 `SetInputImage(image)` | 只喂图（转灰度 + 写 `Tool.InputImage`），**不写 `Pattern.TrainImage`** → 换图不作废旧模板 |
| `SetTrainImage(image)` | = `SetInputImage` + 写 `Pattern.TrainImage`（会作废旧模板，仅需要立刻重训时用） |
| 『采集训练图』改用 `SetInputImage` | 标签分状态：未训练→“验证分数：--（已采集训练图…）”；已训练→“已换新图（旧模板仍有效，点『训练模板』用它重训）” |
| `Train()` | 仍保持“训练 + 自检”（A 方案） |
| `Verify()` 内钉死输入图 | 解决块内数据绑定覆盖 `Tool.InputImage` 导致自检假失败 |
| 修一个由此暴露的 bug | `RefreshPmaToolList()` 原来会把 `_pmaTrainer` 置空 → 重新绑定出的新训练器 `HasTrainImage=false`，『训练模板』误报“还没有训练图”；现在保留实例（工具实例真变了才由 `ReferenceEquals` 重建），并在 `btnTrain_Click` 加一道兜底喂图 |

**复测结果（重新编译后实跑）**

| 步骤 | 结果 |
|---|---|
| 加载 `QuickBuild1.vpp` → 采集 `2.bmp` → 训练 | ✅ 摘要“找到 1 个匹配，最高分 0.895，角度 0.00°”+ 训练诊断；标签 `验证分数：0.895（自检，角度 0.00°）` |
| 采集另一张 `3.bmp`（**不**训练） | ✅ 标签“已换新图（旧模板仍有效，点『训练模板』用它重训）”——旧模板**未被作废** |
| 保存方案 → 探针核验 | ✅ `QuickBuild1_A方案.vpp`：`Trained=True / TrainImage=有 / TrainRegion=CogRectangleAffine / AcceptThreshold=0.7`（修复前这一步会是 `Trained=False`） |

> 结论：既保留了“训练完立刻给分数”的可用性，又消除了“采集一张新图就把方案里已有模板弄失效”的风险。

### 13.6 预览：训练特征标注的显示修复（v1.5）

**问题**：训练完之后预览框里只有原图，看不到特征标注。

**根因（探针实测，非推测）**：`CogRecordDisplay` 在**赋值 `Record` 时会清空 `StaticGraphics`**：

```
before Record: Static=1
after  Record: Static=0      ← 赋值 Record 把已加的标注清空了
```

旧代码的顺序是 `Image → Fit → 加精细特征 → Record = CreateCurrentRecord()`，所以标注刚加上就被 Record 清掉。

**修复**：确定性的“先底图、后标注、最后 Fit”顺序，并且不再使用 `CreateCurrentRecord()`（标注全部自己叠加）：

```csharp
picTemplate.StaticGraphics.Clear();
picTemplate.InteractiveGraphics.Clear();
picTemplate.Image = image;                       // ① 底图
AddPatternGraphics(pattern);                     // ② 精细特征标注
AttachInteractiveTrainRegion();                  // ③ 可拖拽的训练区域
picTemplate.Fit(true);                           // ④ 最后适配（连同标注一起进可视范围）
```

**逐像素验证**（探针把显示控件内容截成位图，再与“只看原图”相减）：

| 场景 | 差异像素 |
|---|---|
| 按修复后的顺序（Image → 标注 → Fit） | 59 像素，颜色 `(128,0,255)`（精细特征轮廓）✔ |
| 只看原图（对照） | 0 |

**顺带查清的两件事**

1. `CreateGraphicsFine` / `CreateGraphicsCoarse` 返回的都是 `CogGeneralContour`，两者轮廓基本重合；且**颜色参数对轮廓不生效**（传 `Purple` 实际渲染成默认色，`Color` 读回是 `Cyan`）。因此只叠**一层**精细特征，避免两层互相压色 —— 见 `AddPatternGraphics`。
2. **这份方案里的训练区域是占位值**：`TrainRegion = CogRectangleAffine, center=(0,0), side=100×50`，即图像左上角一小块。这解释了两件事：
   - 特征标注在预览里只占角落很小一块（不是代码问题）；
   - 自检分数只有 **0.895** 且诊断提示“训练图像显示模糊 / 信息可能不足以测量 Y 轴比例”。
   **建议**：在预览里把区域拖到工件特征上（B 功能）后重新『训练模板』，或回 QuickBuild 重画区域并保存 VPP，分数与标注覆盖范围都会明显改善。

