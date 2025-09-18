# DeMarker Gaining Position Volume 2 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 StockSharp 高阶 API 重写 MetaTrader 5 专家顾问 **“DeMarker gaining position volume 2”**。它监控可配置的 K 线序列，计算 DeMarker 振荡指标，当数值进入极端区域时执行交易，并保留原版的资金管理特色：固定手数、信号反转开关、内置止损止盈以及可选的交易时段过滤。

## 原版专家概览

* **平台**：MetaTrader 5。
* **指标**：标准 DeMarker 振荡器，默认周期 14。
* **入场**：当 DeMarker 跌破下限时做多，当指标突破上限时做空。
* **风险控制**：以点数设置的固定止损/止盈，可选的拖尾止损步长以及交易时段限制。
* **仓位管理**：每根 K 线最多一次入场，切换方向前先平掉反向持仓。

StockSharp 版本遵循相同思路。通过 `StartProtection` 配置保护性指令，因此开仓后止损、止盈和拖尾都会自动维护。

## 交易逻辑

1. 订阅 `CandleType` 指定的 K 线（默认 5 分钟）并用 `DeMarkerPeriod` 的周期计算 DeMarker。
2. 在 K 线收盘时评估指标：
   * 当 `ReverseSignals` 为 **false**（默认）：
     * **做多**：`DeMarker <= LowerLevel`。
     * **做空**：`DeMarker >= UpperLevel`。
   * 当 `ReverseSignals` 为 **true** 时，多空条件互换。
3. 启用 `UseTimeFilter` 时，只在 `SessionStart`–`SessionEnd` 指定的时间窗口交易，支持跨越午夜的区间。
4. 每根 K 线只允许一次新的建仓，并会在进场前关闭所有反向仓位，以复刻 MT5 中的处理方式。
5. `TradeVolume` 决定下单数量；若已经持有同方向仓位则补足到目标仓位。

## 风险管理

* `StopLossPoints` 与 `TakeProfitPoints`（以价格步长计）对应原策略的止损和止盈距离。
* 打开 `EnableTrailing` 后，止损距离改用 `TrailingStopPoints`，并使用 `TrailingStepPoints` 作为拖尾调整的最小步长。
* `StartProtection` 通过 `useMarketOrders = true` 立即执行保护性指令，行为与 MT5 的市价平仓相近。

## 参数说明

| 参数 | 说明 |
|------|------|
| `DeMarkerPeriod` | DeMarker 指标的平滑周期。 |
| `UpperLevel` / `LowerLevel` | 触发做空/做多的阈值。 |
| `ReverseSignals` | 反转多空逻辑。 |
| `StopLossPoints` | 初始止损距离（价格步长数）。 |
| `TakeProfitPoints` | 止盈距离（价格步长数）。 |
| `EnableTrailing` | 是否启用拖尾止损。 |
| `TrailingStopPoints` | 拖尾止损距离。 |
| `TrailingStepPoints` | 每次上调拖尾所需的最小盈利幅度。 |
| `UseTimeFilter` | 限定交易在指定时段内进行。 |
| `SessionStart` / `SessionEnd` | 交易时段的起止时间（支持跨日）。 |
| `TradeVolume` | 每次下单的数量。 |
| `CandleType` | 参与分析的 K 线类型（默认 5 分钟）。 |

## 实现备注

* 原版包含“拖尾激活”阈值。StockSharp 的标准保护模块没有对应设置，因此在 `EnableTrailing = true` 时拖尾会立即生效。
* MT5 中的手数校验、冻结/最小止损距离以及报价刷新在 StockSharp 中由交易基础设施处理，因此未在代码中重复实现。
* 如需更多日志，可调用基类 `Strategy` 的 `LogInfo`/`LogError` 方法。
