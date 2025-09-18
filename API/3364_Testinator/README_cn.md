# Testinator 策略

## 概述

本策略是 MetaTrader 智能交易程序 **Testinator v1.30a** 的 C# 版本。策略只做多，并把所有多单当作一个篮子来管理。只有当一组可配置的技术过滤器全部满足、且价格至少向前推进一定点数时，才会继续加仓。平仓逻辑使用另一组过滤器掩码，结构与开仓完全对称。原版 EA 使用日线 ATR 做风险控制，因此该移植版本除了主级别 K 线外还会订阅日线数据。

## 交易逻辑

### 入场过滤掩码（参数 `BuySequence`）

掩码使用低 9 个二进制位。每个被置 1 的位都必须在上一根完成的 K 线上满足对应条件。

| 位 | 条件 |
| --- | --- |
| 1 | EMA(12) 高于 SMA(14)。 |
| 2 | EMA(50) 低于最近三根 K 线的最低价。 |
| 4 | 前一根 K 线的最低价低于入场布林带下轨（20，2）。 |
| 8 | ADX(14) 高于 -DI，且 +DI 强于 -DI。 |
| 16 | 随机指标 (16, 4, 8) 的 %K 高于 %D，且 %D 大于 80。 |
| 32 | 威廉指标 %R(14) 大于 -20。 |
| 64 | MACD(12, 26, 9) 主线高于信号线。 |
| 128 | 一目均衡云的领先线 A 高于领先线 B，转折线高于基准线，并且上一根 K 线的最低价高于领先线 A。 |
| 256 | RSI（周期 `RsiEntryPeriod`）高于 `RsiEntryLevel` 且相比前一根 K 线在上升。 |

### 出场过滤掩码（参数 `CloseBuySequence`）

| 位 | 条件 |
| --- | --- |
| 1 | SMA(14) 高于 EMA(12)。 |
| 2 | EMA(50) 高于最近三根 K 线的最高价。 |
| 4 | 前一根 K 线的最高价高于出场布林带上轨（`BollingerCloseLength`, `BollingerCloseDeviation`）。 |
| 8 | -DI 高于 +DI。 |
| 16 | 随机指标的 %D 低于 80。 |
| 32 | 威廉指标 %R(14) 低于 -80。 |
| 64 | MACD 主线低于信号线。 |
| 128 | 一目均衡云的领先线 B 高于领先线 A。 |
| 256 | RSI（周期 `RsiClosePeriod`）低于 `RsiCloseLevel`。 |

只有当所有启用的入场位都返回真、当前买单数量少于 `MaxBuys` 并且最新成交价距离上一次加仓至少 `StepPips` 个点时，策略才会继续加仓。一旦出场掩码满足或保护性价位被触发，就会整体平仓。

### 交易时间与风险控制

* 策略只在 `TradeStartHour` 到 `TradeStartHour + TradeDurationHours - 1`（东欧时间）的时段内交易。若超出交易窗口且篮子处于盈利状态，会立即平掉所有多单。
* 止盈和止损距离以点为单位。设置为 `-1` 表示关闭；设置为 `0` 时使用 ATR 乘数（`StopRatio`、`TakeRatio`）。
* 跟踪止损同样支持 ATR 逻辑，通过 `StartTrailPips`、`TrailStepPips`、`StartTrailRatio`、`TrailStepRatio` 来配置。
* 策略在日线数据上计算 ATR(15)，以保持与原始 EA 一致的行为。

## 参数

* `TradeVolume` – 每次市价买单的交易量。
* `BuySequence` / `CloseBuySequence` – 控制各个指标过滤条件的位掩码。
* `MaxBuys` – 同时持有的最大买单数量。
* `StepPips` – 再次加仓所需的最小价格推进（点）。
* `TradeStartHour`, `TradeDurationHours` – 日内交易窗口。
* `TakeProfitPips`, `StopLossPips` – 固定止盈止损（负值关闭，0 表示使用 ATR 比例）。
* `StartTrailPips`, `TrailStepPips` – 跟踪止损的起始距离和步长（负值关闭，0 表示使用 ATR 比例）。
* `TakeRatio`, `StopRatio`, `StartTrailRatio`, `TrailStepRatio` – 当固定值为零时所使用的 ATR 乘数。
* `RsiEntryLevel`, `RsiEntryPeriod` – 入场 RSI 的阈值与周期。
* `RsiCloseLevel`, `RsiClosePeriod` – 出场 RSI 的阈值与周期。
* `BollingerCloseLength`, `BollingerCloseDeviation` – 出场布林带参数。
* `CandleType` – 主时间框架（策略会自动订阅日线以计算 ATR）。

## 说明

* 移植版本保持了原 EA 的篮子管理模型：只做多，并仅使用市价单。
* 策略显式保存各指标的历史值，以模拟 MetaTrader 中对 `bar[1]` 的引用。
* 原 EA 中未使用的输入（例如 `TakeAsBasket`, `StopAsBasket` 等）在移植时被忽略，因为它们不会影响逻辑。
