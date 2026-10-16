# Framework 第三求解器

## 交付范围

`Framework/DeepNestLib/Background.cs` 的 `placeParts2()` 已替换旧两件前瞻实现，提供独立的多板求解闭环。`sync()` 在原来的 `placeParts()`、`placeParts1()` 后运行新路径，再用共享结果比较器择优。前两种方法体与本轮修改前基线逐文本相同，核验脚本为 `verify_original_placers.py`。

这是借鉴研究机制、复用 DeepNest NFP 几何的完整启发式求解器，不是 NestFab/CNS 内核的完整等价复制。没有同一输入与预算的 NestFab 最终排版对照，不能宣称复现其全部效果。

## 算法

- 首次起点保留输入顺序；后续起点对面积初始化的优先权重乘以[0.8,1.2)随机因子，未放件增益1.5，并归一化。固定种子控制随机序列。这些数值和权重到顺序的映射是本项目设计，不冒充CNS原始参数。
- 逐件生成现有配置允许的离散旋转；不支持任意连续旋转或新镜像约束。
- 每张板执行滚动有界前瞻束搜索：扩展前若干剩余件、角度和位置候选，同时保留跳过首件分支。按已放数量、已放面积、包围盒紧凑度排序，签名含实例、旋转、坐标及剩余集合。
- 候选来自板内NFP减去已放零件禁止区域的布尔差集。外NFP平移前递归复制，避免新路径污染共享返回值。可行域顶点约按256个采样尺度取样，再截断候选数；不是穷举所有合法位置。
- 每个提交步骤减少Pending，已放件从多板剩余集合删除；未放件继续下一张板。空板跳过，不阻止后续更大板。
- 搜索预算耗尽后退化为单分支、单位置候选的贪心补完，继续尝试剩余件及旋转。单次NFP不可中断，因此预算不是总运行时间硬上限。
- 多起点分别生成整解，按现有放置数量、板数、fitness规则保留最好结果；未放件有惩罚。空零件和无板输入均返回可解释结果。

## 配置

`SvgNestConfig.cs`：
- EnablePlaceParts2=true
- PlaceParts2Starts=3
- PlaceParts2BeamWidth=4
- PlaceParts2Lookahead=3
- PlaceParts2Candidates=3
- PlaceParts2Seed=1
- PlaceParts2TimeLimitMs=200

起点、束宽、候选数最大32，前瞻最大8。设置EnablePlaceParts2=false可恢复仅前两条路径参与的组合。时间预算为0时不进行额外起点，首解按贪心补完。固定seed不保证在时间截止触发时跨机器得到完全相同结果。

每个GA个体评估都会运行这条路径，因此增加预算会放大整轮遗传计算成本。GA本身仍使用原有fitness选择，没有改交叉或变异算法。

## 几何与缓存调用约束

直接调用placeParts2时必须提供已预处理几何；仅设置config.spacing不会自行偏移输入。正常NestingContext流水线已做零件/板边间距预处理。

source必须稳定表示同一几何，不同几何不能共享source；板和零件来源应分离。输入几何、孔洞或间距改变时，通过StartNest重置缓存，或在独立调用前清空两层缓存。新路径不修复全局缓存键缺少几何版本的继承问题。

孔洞继续依赖现有NFP实现。本次验证了普通一级板孔洞，不保证任意嵌套轮廓、所有退化输入或外部Minkowski DLL路径。原两条路径本身仍有共享缓存/几何风险；它们的方法体按要求没有改动。库内没有新增最终合法性兜底过滤，故验证通过不能外推到所有输入。

## 验证

VS MSBuild构建Framework Console引用链，Debug/x64、RestoreDuringBuild=false，退出0。现有未使用变量、成员隐藏和不可达代码等警告仍存在。

`SolverChecks.cs`：617项断言通过，输出 `solver-results.txt`。包括：
- 空件、无板、单件、重复同源实例、跳过空小板、非原点凹形板、板孔洞、必须旋转、凹形零件、无法放件、多板。
- 固定seed下冷/暖缓存的布局一致性、输入不变、有限fitness、实例不重复及合法旋转。
- 用独立重建的Clipper实心多边形差集/交集检查板内包含和无正面积重叠，不用候选NFP自证。
- 零搜索预算贪心补完。
- 正常流水线与直接预处理输入的新路径间距样例。流水线样例断言4单位零件间距、3单位板边间距；直接新路径样例检查预处理下两件可放及原始实心区域合法性。
- 关闭新路径后正常流水线仍可返回结果。

`PlacementSelectionChecks.exe`既有58项断言再次通过。
`verify_original_placers.py`确认placeParts、placeParts1方法文本相同；最初简单括号计数hash误把注释内括号计入范围，原original-placer-hashes.json不应作为正确方法边界依据，应使用核验脚本。
`git diff --check`通过，仅提示工作树换行符转换。

固定8件矩形样例（一次运行，非性能基准）：
- placeParts：8件、1板，fitness约10132.35，低于1ms显示为0ms。
- placeParts1：8件、2板，fitness约23126.98，约7ms。
- placeParts2：8件、1板，fitness约10132.35，约14ms。

这仅说明该样例与原贪心路径持平，并不能宣称总体提升、CNS等价或全局最优。

## 已知能力边界

- 顺序多板分配，没有跨板回迁、移除重插或完整全局局部优化。
- 固定宽度/深度、候选采样可能错过更好的布局；没有最优性证明。
- 混合板材的经济性仍依赖现有用板数优先与fitness，不保证最低面积或材料成本。
- 没有实现NestFab数据库策略、矩形/平铺专用内核、扫描评分场的完整生产路径、镜像/共边加工等全部功能。
- 没有NestFab同数据最终布局对照或工业规模性能验证。

## 复现

在项目根目录运行：
```powershell
& 'D:\vs studio\MSBuild\Current\Bin\MSBuild.exe' Framework/DeepNestConsole/DeepNestConsole.csproj /p:Configuration=Debug /p:Platform=x64 /p:RestoreDuringBuild=false /v:minimal /nologo
& 'D:\vs studio\MSBuild\Current\Bin\Roslyn\csc.exe' /nologo /platform:x64 /out:Framework/DeepNestConsole/bin/x64/Debug/SolverChecks.exe /reference:Framework/DeepNestConsole/bin/x64/Debug/DeepNestLib.dll /reference:Framework/DeepNestConsole/bin/x64/Debug/clipper_library.dll validation/SolverChecks.cs
& .\Framework\DeepNestConsole\bin\x64\Debug\SolverChecks.exe
```

本轮没有提交或推送Git，没有修改商业DLL或授权材料。pre-solver-changes.patch为编辑前已有差异备份，不是新求解器补丁。
