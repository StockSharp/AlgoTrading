# Price Extreme 策略

## 概述

**Price Extreme Strategy** 是将 MetaTrader 专家顾问 `Price_Extreme_Strategy` 移植到 StockSharp 高级 API 的版本。策略使用最近若干根已完成 K 线的最高价和最低价构建一个动态通道。当指定的参考 K 线收盘价突破通道上轨或下轨时触发交易信号。可选的 **Reverse Signals** 参数允许将突破逻辑反向应用于反趋势入场。

策略以事件驱动方式运行，每当 K 线收盘时立即评估并下单，与原始 MQL 程序在下一根 K 线开盘瞬间处理信号的节奏一致。

## 指标逻辑

通道边界使用 StockSharp 自带的 `Highest` 与 `Lowest` 指标实时更新：

- `Highest` 追踪最近 *N* 根 K 线的最高价。
- `Lowest` 追踪最近 *N* 根 K 线的最低价。

这两个指标重现了原始专家顾问中的 `Price_Extreme_Indicator`。参数 **Level Length** 决定窗口长度。**Signal Shift** 指定用于判定突破的历史 K 线（`1` 表示刚刚收盘的 K 线，较大的值会使用更早的 K 线进行确认）。

## 交易规则

1. 每当一根 K 线收盘，重新计算通道上下边界。
2. 从内部历史缓存中取出由 **Signal Shift** 指定的参考 K 线。
3. 生成突破意图：
   - **向上突破**：参考 K 线的收盘价高于上轨。
   - **向下突破**：参考 K 线的收盘价低于下轨。
4. 如果启用了 **Reverse Signals**，则对突破方向进行反向处理（向上突破触发做空，向下突破触发做多）。
5. 检查 **Enable Long** 与 **Enable Short**，确保方向被允许。
6. 开仓前如有相反方向持仓，先平仓后再建立新仓位，保持净持仓唯一。

## 风险控制

- **Stop Loss** 与 **Take Profit** 均以价格步长 (`Security.PriceStep`) 表示，`0` 表示关闭该功能。
- 每当净持仓数量发生变化时重新计算止损和止盈价位。
- 若完成 K 线的价格区间触及保护价位（多头触及止损则最低价低于止损价等），策略会以市价单平仓并清除目标。
- 在 `OnStarted` 中调用 `StartProtection()`，启用 StockSharp 的内置保护机制。

## 参数

| 参数 | 说明 | 默认值 | 分组 |
|------|------|--------|------|
| `LevelLength` | 计算通道所使用的历史 K 线数量。 | 5 | Indicator |
| `SignalShift` | 判定突破时参考的已收盘 K 线序号（1 = 最近一根）。 | 1 | Indicator |
| `EnableLong` | 是否允许做多。 | `true` | Trading |
| `EnableShort` | 是否允许做空。 | `true` | Trading |
| `ReverseSignals` | 是否反转突破信号方向。 | `false` | Trading |
| `OrderVolume` | 每次市价单的交易量。 | 1 | Trading |
| `StopLossPoints` | 止损距离（价格步长数）。 | 0 | Risk |
| `TakeProfitPoints` | 止盈距离（价格步长数）。 | 0 | Risk |
| `CandleType` | 使用的主时间周期。 | 5 分钟 | Data |

所有参数均通过 `StrategyParam<T>` 暴露，可在 StockSharp Designer 中展示与优化。

## 使用建议

1. 选择交易品种，并将 **Candle Type** 设置为希望的周期，与原始 MQL 设置保持一致。
2. 调整 **Level Length** 控制通道宽度。数值越大通道越平滑，越小反应越灵敏。
3. 根据需求设置 **Signal Shift**，决定是否等待更多 K 线确认。
4. 通过 **Enable Long**、**Enable Short** 与 **Reverse Signals** 定义允许的交易方向及逻辑。
5. 设定 **Order Volume**、**Stop Loss** 和 **Take Profit**，注意它们的单位是价格步长。
6. 启动策略后，如存在图表区域，会自动绘制 K 线、指标通道以及实际成交。

## 备注

- 策略始终保持单一净持仓，完全复制原专家顾问在开仓前平掉反向单的行为。
- 止损与止盈在 K 线收盘时检查，实盘中可近似理解为服务器端的保护单。
- 本次仅提供 C# 版本，按要求未创建 Python 代码及其目录。
