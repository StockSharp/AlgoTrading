# Adaptive Grid MT4（StockSharp 版本）

## 概述

该策略移植自 MetaTrader 的 "Adaptive Grid Mt4" 智能交易系统，使用 StockSharp 的高级 API 构建。策略会围绕当前
K 线收盘价布置对称的买入止损与卖出止损网格，网格间距由平均真实波幅（ATR）决定，从而自动适应市场波动。每个
挂单都有以 K 线数量表示的寿命限制，在震荡行情中可及时清理过期订单。

当挂单被触发后，策略立即按照生成网格时的 ATR 快照下达对应的止盈与止损订单。保护性订单与触发的挂单一一对应，
直到成交或手动取消。

## 参数

| 参数 | 说明 |
|------|------|
| `GridLevels` | 市场上方和下方各放置多少个止损挂单，对应原脚本的 `nGrid`。 |
| `TimerBars` | 挂单允许保留的已完成 K 线数量，超过后会被取消（原脚本 `nBars`）。 |
| `PriceOffsetMultiplier` | 初始突破偏移的 ATR 倍数（`Poffset`）。 |
| `GridStepMultiplier` | 网格层之间的 ATR 倍数（`Pstep`）。 |
| `StopLossMultiplier` | 止损价格与入场价之间的 ATR 倍数（`StopLoss`）。 |
| `TakeProfitMultiplier` | 止盈价格与入场价之间的 ATR 倍数（`TakeProfit`）。 |
| `AtrPeriod` | ATR 平滑周期，对应原代码中的 14。 |
| `OrderVolume` | 每个挂单的下单量（`Lot`）。 |
| `CandleType` | 驱动网格更新的 K 线周期（`Wtf`）。 |

## 交易流程

1. 订阅 `CandleType` 指定周期的 K 线，并计算 ATR(14)。
2. 每当一根 K 线收盘时：
   - 递增内部的 bar 计数器，并取消所有超出 `TimerBars` 限制的网格挂单。
   - 如果 ATR 尚未形成、仍有网格挂单处于激活状态，或策略已经持仓，则跳过后续步骤。
   - 根据 `ATR * 倍数` 计算突破偏移、网格步长、止损和止盈距离。
   - 围绕 K 线收盘价放置 `GridLevels` 对买入止损和卖出止损订单，价格通过 `Security.ShrinkPrice` 归一化以匹配最小跳动。
3. 挂单成交时，从网格列表中移除该挂单，并创建对应的保护性订单：
   - 多头使用 `SellStop` 作为止损、`SellLimit` 作为止盈。
   - 空头使用 `BuyStop` 作为止损、`BuyLimit` 作为止盈。
4. 通过 `OnOrderChanged` 监控保护性订单的完成状态，及时清理追踪列表。

## 说明

- 只有在没有持仓且所有网格挂单都被取消的情况下才会生成新的网格，复现 MQL 中 `What()` 的行为。
- 价格计算基于 K 线收盘价而不是即时 Bid/Ask，从而保持 K 线驱动的结构同时获得对称的网格。
- 网格生成时的 ATR 数值同时用于止盈与止损，确保与 MetaTrader 中的每笔单独挂单保持一致。
- 目前未实现 Python 版本，符合需求说明。
