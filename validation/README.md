# 分层结果择优适配与验证

## 本轮改动

仅修改 Framework，不修改 Core、UI、遗传编码、NFP 几何或现有局部评分。

共享比较器位于 `Framework/DeepNestLib/SvgNest.cs` 的 `SheetPlacement.IsBetterThan`，依次比较：
1. 非空结果优于空结果。
2. 已放不同零件实例 ID 的数量，多者优先。
3. 已放数量相同，非空板材项的不同板材实例 ID 数量，少者优先。
4. 前两项相同，有限 fitness 优于缺失、NaN 和无穷；有限 fitness 越小越好。
5. fitness 差的绝对值不超过 `1e-7 * max(abs(a), abs(b))` 时持平，保留当前结果。这是纯相对容差，不含绝对容差下限。

放置数和用板数优先于 fitness 的有限性，这是本轮确定的规则。ID 去重只防止重复条目虚增比较指标，不将重复放置变成合法结果。

接入三处：
- `Background.sync()`：现有贪心与前缀束搜索放置结果择优。
- `SvgNest.ResponseProcessor()`：维护最佳结果历史。
- `NestingContext.NestIterate()`：更新实际赋值、显示及导出的布局。

遗传种群仍按原 scalar fitness 选择；这不是遗传算法或放置内核替换。Background.cs 在本轮开始前已有用户差异，本轮只替换 sync 的择优条件，没有回退已有工作。

## CNS 证据和适配边界

NestFab 工作区中的补充材料：
- `analysis/native/source/CNS_ComputeCallback.c`：补识别回调，RVA 0x8e80。
- `analysis/native/source/CNS_ComputeEngine.c`：运行时确认的外层计算入口，RVA 0x8a5750。
- `analysis/native/source/CNS_InnerEngine.c`：内层组合计算入口，RVA 0x8a3b20。
- `analysis/native/source/CNS_Worker.c`：组合项任务回调，RVA 0x409f0。
- `analysis/native/source/CNS_ResultCompare.c`：比较辅助函数输出，包含分层判定和浮点相对容差。
- `analysis/native/compute_target_trace.jsonl`：实际计算目标的进入、返回记录。

原生输出中的 CNS_ComputeEngine 是分析时赋予的临时名称，不是恢复的原始函数名；多个独立输出文件可能使用相同临时名称。比较函数的请求入口 RVA 0x603b80 是跳板，工具跟踪到了主体 RVA 0x6037c0。

运行时实际读取的组合计算器包含两个项，均通过 RVA 0x8a3a00 包装器调用下层对象；这不证明两个项使用不同算法。外层计算目标本轮记录耗时约 20.9 秒，该值不是 DeepNest 性能基准。比较常量在该次运行中读取为 0.9999999。

本次借鉴的是多计算器结果的分层比较思想；本项目自行明确采用放置数、用板数和 fitness，不宣称 CNS 中尚未解释的字段就是这些指标。其候选生成、碰撞检测、搜索邻域和接受规则尚未恢复，因此不能称为完整 CNS 算法移植。

## 已执行验证

Framework Console 引用链使用 VS MSBuild 构建成功，Debug/x64，不恢复包。构建存在原有代码段的未使用变量、成员隐藏和不可达代码等警告，本次新增比较器未报告编译错误。

独立测试 `PlacementSelectionChecks.cs` 编译并执行，退出码 0，共 58 项断言通过：
- 空结果、更多零件、少用板、同来源不同实例、重复 ID、空板。
- 有限与非有限 fitness、持平、相对容差、接近零和负数评分。
- 两张 100×100 板，四件 40×30 和两件 25×20 零件，串行评估三次。
- 六件全部放入；结果不退化；实例不重复；坐标与旋转有限；旋转为90度步长；板内包含；矩形无正面积重叠。

该次矩形验证记录 fitness 10120.6896551724、三次迭代总耗时约105ms。三次迭代不是完成一代优化；上述耗时不是与旧版本的公平性能对照，不能据此宣称利用率或速度提升。

未覆盖异形、孔洞、非零间距、冷/暖缓存一致性、不同规格板材的经济性、完整 UI 或大规模对照。用板数优先也不等价于最小总板材面积/成本，尤其在混合板材规格场景中需要业务确认。

## 复现

在项目根目录的 PowerShell 中运行：

```powershell
& 'D:\vs studio\MSBuild\Current\Bin\MSBuild.exe' '.\Framework\DeepNestConsole\DeepNestConsole.csproj' /t:Build /p:Configuration=Debug /p:Platform=x64 /p:RestoreDuringBuild=false /nologo /v:minimal
& 'D:\vs studio\MSBuild\Current\Bin\Roslyn\csc.exe' /nologo /platform:x64 /out:Framework/DeepNestConsole/bin/x64/Debug/PlacementSelectionChecks.exe /reference:Framework/DeepNestConsole/bin/x64/Debug/DeepNestLib.dll validation/PlacementSelectionChecks.cs
& .\Framework\DeepNestConsole\bin\x64\Debug\PlacementSelectionChecks.exe
```

验证输出见 `placement-selection-results.txt`。未创建 Git 提交。
