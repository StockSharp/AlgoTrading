# Two MA One RSI 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 5 专家顾问 “Two MA one RSI” 移植到 StockSharp。它通过快慢两条均线的交叉配合前一根 K 线的 RSI 信号生成交易，并提供多个布尔开关来快速反转比较条件。

## 细节
- **入场条件**：
  - 多头：两根之前快线低于慢线，上一根快线高于慢线，同时前一根 RSI 高于上轨。三个条件均可通过布尔参数调整方向。
  - 空头：逻辑完全相反，检查快慢均线位置与 RSI 是否跌破下轨。
  - 两条均线共享同一种类型，慢线周期恒等于 `FastMaPeriod * SlowPeriodMultiplier`。`FastMaShift`/`SlowMaShift` 参数重现 MT5 中的水平偏移效果。
- **方向**：可做多也可做空。`CloseOppositePositions` 控制在出现反向信号时是否先平掉对冲仓位。
- **出场条件**：
  - 固定止损/止盈（以点数计）。
  - 可选的跟踪止损，只有当价格至少前进 `TrailingStopPips + TrailingStepPips` 点后才会移动，并始终保持与收盘价的距离为 `TrailingStopPips`。
  - `ProfitClose` 监控浮动盈亏（使用合约的价格步长与步值）并在达到目标货币金额后平掉所有仓位。
- **止损管理**：`StopLossPips = 0` 时仅依赖跟踪止损。启用跟踪功能必须保证 `TrailingStepPips > 0`，与原版规则一致。
- **默认值**：`FastMaPeriod = 10`、`SlowPeriodMultiplier = 2`、`FastMaShift = 3`、`SlowMaShift = 0`、`RsiPeriod = 10`、`RsiUpperLevel = 70`、`RsiLowerLevel = 30`、`StopLossPips = 50`、`TakeProfitPips = 150`、`TrailingStopPips = 15`、`TrailingStepPips = 5`、`MaxPositions = 10`、`ProfitClose = 100`、`TradeVolume = 1`。
- **过滤器**：六个布尔开关（`BuyPreviousFastBelowSlow`、`BuyCurrentFastAboveSlow`、`BuyRequiresRsiAboveUpper`、`SellPreviousFastAboveSlow`、`SellCurrentFastBelowSlow`、`SellRequiresRsiBelowLower`）可即时改变各项比较关系。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 使用的 K 线类型或时间框架。 |
| `MaType` | 均线类型（SMA、EMA、Smoothed、WMA、VWMA）。 |
| `FastMaPeriod` | 快线周期。 |
| `SlowPeriodMultiplier` | 慢线周期倍率（慢线周期 = 快线周期 × 倍率）。 |
| `FastMaShift`, `SlowMaShift` | 水平偏移量（以 K 线数计算）。 |
| `RsiPeriod` | RSI 计算周期（读取上一根完成的 K 线）。 |
| `RsiUpperLevel`, `RsiLowerLevel` | RSI 上/下阈值。 |
| `BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper` | 多头信号开关。 |
| `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower` | 空头信号开关。 |
| `StopLossPips`, `TakeProfitPips` | 止损与止盈点数（基于合约价格步长估算）。 |
| `TrailingStopPips`, `TrailingStepPips` | 跟踪止损距离与最小推进。 |
| `MaxPositions` | 每个方向最多允许的持仓数（0 表示无限制）。 |
| `ProfitClose` | 浮动收益达到该货币金额时强制平仓。 |
| `CloseOppositePositions` | 是否在开新仓前先平掉反向仓位。 |
| `TradeVolume` | 基础下单手数，同时写入策略的 `Volume` 属性。 |

## 实现说明
- 所有判断都基于已完成的 K 线，完全贴合 MT5 版本只在新柱开启时触发的逻辑。
- 点值直接使用证券的价格步长。如交易品种采用 1/10 点报价，请在证券设置中调整步长以模拟原策略的 `digits_adjust` 处理。
- 跟踪止损只在价格推进 `TrailingStopPips + TrailingStepPips` 后才生效，并且只有当新的止损价优于旧值至少 `TrailingStepPips` 才会更新。
- `ProfitClose` 通过 `PriceStep` 与 `StepPrice` 计算盈亏，务必确认这些字段配置正确。
