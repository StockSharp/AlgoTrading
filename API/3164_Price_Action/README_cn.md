# 价格行为策略

该策略是 MetaTrader "PRICE_ACTION" 专家的 C# 版本。它在所选周期上结合威廉姆斯分形、加权移动平均线、动量和 MACD 滤波器，寻找由价格行为确认的突破机会。

## 思路

1. 仅分析已完成的 K 线，所有决策都在设定周期的收盘价上完成。
2. 使用 5 根 K 线窗口捕获新的多头或空头分形。新的下分形表示潜在支撑，新的上分形表示潜在阻力。
3. 通过两条线性加权移动平均线确认趋势方向。做多需要快线高于慢线，做空则相反。
4. 检查动量指标与 100 的偏离度，以确认更高周期上的动量强度。
5. 使用 MACD（默认 12,26,9）过滤信号：多头要求 MACD 主线位于信号线上方，空头要求主线位于信号线下方。
6. 当所有条件一致时，按突破方向进场，并通过固定止损、跟踪止损以及可选的保本平移来管理仓位。

## 入场规则

- **多头**
  - 当前 K 线上形成新的下分形（五根 K 线模式）。
  - 快速 LWMA &gt; 慢速 LWMA。
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`。
  - MACD 主线 &gt; MACD 信号线。
  - 下单数量基于策略的 `Volume`，同时受 `MaxPositionUnits` 限制。

- **空头**
  - 当前 K 线上形成新的上分形。
  - 快速 LWMA &lt; 慢速 LWMA。
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`。
  - MACD 主线 &lt; MACD 信号线。

## 离场规则

- 使用 `StopLossPoints` 和 `TakeProfitPoints`（以价格步长表示）的固定止损/止盈。
- 可选的 `TrailingStopPoints` 跟踪止损，在价格至少运行一个跟踪距离后锁定利润。
- 可选的保本机制：当收益达到 `BreakEvenTriggerPoints` 时，将止损移动至 `EntryPrice ± BreakEvenOffsetPoints`。
- 所有离场通过市价单完成，利用 K 线高低点判断触发条件。

## 仓位管理

- 策略每个品种仅维护一个净仓位。
- `Volume` 定义基础下单数量。反向操作时会先平掉反向持仓，再按需求开新仓。
- `MaxPositionUnits` 用于限制仓位绝对值，防止过度放大。

## 参数

- `CandleType` – 指标与交易使用的周期（对应 MQL 中的 `T`）。
- `FastMaPeriod` / `SlowMaPeriod` – 加权移动平均线长度（`FastMA`, `SlowMA`）。
- `MomentumPeriod` – 动量回看长度（原脚本固定为 14）。
- `MomentumThreshold` – 动量相对 100 的最小偏离（对应 `Mom_Buy`/`Mom_Sell`）。
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD 参数。
- `StopLossPoints`, `TakeProfitPoints` – 固定止损/止盈距离（`Stop_Loss`, `Take_Profit`）。
- `TrailingStopPoints` – 跟踪止损距离（`TrailingStop`）。
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – 保本触发值与偏移（`WHENTOMOVETOBE`, `PIPSTOMOVESL`）。
- `FractalLifetime` – 分形在多少根 K 线内有效（`CandlesToRetrace`）。
- `MaxPositionUnits` – 仓位上限（对应 `Max_Trades` 的手数限制）。
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – 分别控制跟踪/保本/止损/止盈逻辑。

## 与原始 EA 的差异

- 未实现按金额止盈、权益止损及邮件/通知等投资组合级功能。
- MetaTrader 中的手数优化被简化，策略改用 StockSharp 的成交量规范化。
- 保护性操作使用市价平仓，而不是修改挂单，因为 StockSharp 的风险控制方式不同。

## 文件

- `CS/PriceActionStrategy.cs` – C# 实现。
- 暂无 Python 版本。
