# Butterfly Pattern 策略

## 概览

**Butterfly Pattern Strategy** 将 MetaTrader「Cypher EA」谐波交易思想迁移到 StockSharp 高阶 API。策略在指定周期的 K 线中寻找多头/空头蝴蝶形态，验证关键斐波那契比例，并以三段式止盈开仓。与原版 EA 一样，还提供了移动至保本和移动止损的仓位管理功能。

## 工作流程

1. 策略按照 `PivotLeft` / `PivotRight` 设置缓存 K 线，确认局部高低点（Pivot）。
2. 当最新的五个 Pivot 满足蝴蝶形态的排列时，检查所有必要的比例约束。
3. 通过 `MinPatternQuality` 计算的质量分数过滤劣质形态；若启用 `RevalidatePattern`，还会在下单前再次验证价格。
4. 在信号确认的收盘时：
   - 按照固定手数或风险百分比下达市价单。
   - 将持仓按照 `TP1/TP2/TP3` 参数划分为三个止盈目标。
   - 根据形态几何关系计算保护性止损。
5. 在持仓期间，策略跟踪价格是否到达分段止盈，必要时把止损推至保本并按设定的步长进行跟踪。

> **提示：** 原始 EA 同时处理多个周期。若要在 StockSharp 中实现同样的效果，可启动多个策略实例，并为它们指定不同的 `CandleType`。

## 关键参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 用于检测形态的 K 线类型（时间周期）。 |
| `PivotLeft` / `PivotRight` | 确认 Pivot 所需的左/右侧 K 线数量。 |
| `Tolerance` | 允许的斐波那契比例误差。 |
| `AllowTrading` | 是否在检测到形态后发出交易。 |
| `UseFixedVolume` / `FixedVolume` | 是否使用固定手数；关闭时将根据 `RiskPercent` 动态计算。 |
| `RiskPercent` | 单笔交易愿意承受的账户风险百分比（仅在动态手数模式下有效）。 |
| `AdjustLotsForTakeProfits` | 重新归一化分段手数，使其总和等于下单量。 |
| `Tp1Percent` / `Tp2Percent` / `Tp3Percent` | 三个止盈目标对应的手数分配比例。 |
| `MinPatternQuality` | 接受形态所需的最小质量评分（0–1）。 |
| `UseSessionFilter`, `SessionStartHour`, `SessionEndHour` | 限定策略只在指定交易时段工作。 |
| `RevalidatePattern` | 在下单前再次确认形态没有被价格破坏。 |
| `UseBreakEven`, `BreakEvenAfterTp`, `BreakEvenTrigger`, `BreakEvenProfit` | 控制在达到特定止盈后将止损移动到保本附近的逻辑。 |
| `UseTrailingStop`, `TrailAfterTp`, `TrailStart`, `TrailStep` | 达到指定止盈且满足最小盈利后启动跟踪止损。 |

## 风险控制

- 策略内部管理止损、保本和跟踪逻辑，不会额外挂出保护单；出场使用市价指令以贴合原始 EA 行为。
- 当关闭 `UseFixedVolume` 时，持仓手数依据止损距离、合约价格步长以及 `RiskPercent` 自动计算。

## 使用建议

- 请确认标的证券支持所选的 `CandleType` 以及足够的报价精度，否则最小距离校验可能导致信号被拒绝。
- `BreakEvenAfterTp` 与 `TrailAfterTp` 需在对应分段止盈成交后才会生效。
- 如需同时监控多个周期，可运行多个策略实例并分别设置不同的 `CandleType`。
