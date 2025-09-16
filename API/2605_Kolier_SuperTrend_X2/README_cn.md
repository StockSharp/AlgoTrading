# Kolier SuperTrend X2 策略

## 概述
该策略复刻原始的 MetaTrader 专家顾问，通过两个不同周期的 SuperTrend 滤波器协同工作。高周期 SuperTrend 用于识别市场主趋势，低周期 SuperTrend 负责在趋势方向出现同步反转时触发交易信号。移植到 StockSharp 时使用了高级 API 绑定，因此指标直接接收K线并维护内部状态。

## 交易逻辑
- **趋势过滤：** 高周期 SuperTrend 需要确认多头或空头走势。`TrendSignalShift` 控制确认的延迟，`TrendMode` 决定是只要一根K线（`NewWay`）还是需要两根连续K线（其余模式）。
- **进场信号：** 低周期 SuperTrend 等待与当前趋势一致的方向切换。`EntrySignalShift` 用于延迟到完全收盘的K线，`EntryMode` 控制策略在切换后立即响应（`NewWay`）还是等待确认（其他模式）。
- **做多：** 当 `EnableBuyEntries` 为 `true`、趋势过滤显示多头且低周期 SuperTrend 按选定模式翻转向上时，平掉可能存在的空头头寸，再以 `Volume + |Position|` 的量开多。
- **做空：** 当 `EnableSellEntries` 为 `true`、趋势过滤显示空头且低周期 SuperTrend 翻转向下时，先平掉多头再开空。
- **离场：**
  - 高周期趋势反向时，可根据 `CloseBuyOnTrendFlip` 或 `CloseSellOnTrendFlip` 平掉相应方向仓位。
  - 低周期方向切换，在启用 `CloseBuyOnEntryFlip`/`CloseSellOnEntryFlip` 时也会触发平仓。
  - 可选的固定止损/止盈（`StopLossPoints`、`TakeProfitPoints`）按 `Security.PriceStep` 的倍数计算。

## 指标
- 两个 StockSharp `SuperTrend` 实例（分别用于趋势过滤与入场信号）。

## 参数
- `TrendCandleType` – 趋势过滤所用K线周期。
- `EntryCandleType` – 入场信号所用K线周期。
- `TrendAtrPeriod`、`TrendAtrMultiplier` – 趋势 SuperTrend 的 ATR 设置。
- `EntryAtrPeriod`、`EntryAtrMultiplier` – 入场 SuperTrend 的 ATR 设置。
- `TrendMode`、`EntryMode` – 确认模式：`NewWay` 一根K线即可，其余模式需要两根连续K线（在本移植中 Visual 与 ExpertSignal 与经典模式一致）。
- `TrendSignalShift`、`EntrySignalShift` – 使用指标值前需要等待的已收盘K线数量。
- `EnableBuyEntries`、`EnableSellEntries` – 是否允许做多/做空。
- `CloseBuyOnTrendFlip`、`CloseSellOnTrendFlip` – 趋势过滤反向时的离场选项。
- `CloseBuyOnEntryFlip`、`CloseSellOnEntryFlip` – 入场周期方向反向时的离场选项。
- `StopLossPoints`、`TakeProfitPoints` – 以价格跳动（PriceStep）为单位的止损/止盈距离（0 表示禁用）。
- `Volume` – 新开仓的基础数量。
- `Slippage` – 为兼容原始专家保留的占位参数。

## 说明
- 该移植遵循 StockSharp 的高级流程：通过 `SubscribeCandles` 订阅K线、使用 `BindEx` 绑定指标，只保存必要的少量状态（趋势方向和当前止损/止盈）。
- 调用了 `StartProtection()` 来启用 StockSharp 内置的头寸保护助手。
