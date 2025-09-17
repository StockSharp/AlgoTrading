# FT Bill Williams Trader 策略

## 概述

**FT Bill Williams Trader Strategy** 是 MetaTrader 专家顾问 “FT_BillWillams_Trader” 的高层 StockSharp 移植版本。策略把比尔·威廉姆斯的分形指标和 Alligator 鳄鱼指标结合起来，捕捉趋势方向的突破机会。在入场之前，算法会验证 Alligator 三条线的排列，并根据需要附加距离、趋势一致性和反向信号等过滤条件。

## 交易逻辑

1. **分形识别**：策略维护最近 `FractalPeriod` 根 K 线的高点和低点。当中间那根 K 线创出窗口内的最高（或最低）点时，记录新的突破价位，并根据 `IndentPoints` 在该价位上方/下方加入偏移，以避免过早入场。
2. **突破确认**：
   - `PriceBreakout` 模式在当前 K 线的最高价/最低价穿越突破价位时触发。
   - `CloseBreakout` 模式则要求上一根 K 线的收盘价已经站在突破价位之外。
3. **距离过滤**：若突破价位与上一根 K 线的鳄鱼“嘴唇”之间的距离超过 `MaxDistancePoints`（点），信号会被拒绝。将该参数设为 0 可关闭此检查。
4. **齿线过滤**：当 `UseTeethFilter` 打开时，上一根 K 线的收盘价必须位于鳄鱼“牙齿”之上（做多）或之下（做空）。
5. **趋势一致性**：`UseTrendAlignment = true` 时，鳄鱼的“嘴唇”“牙齿”“下颚”之间的间距需要分别超过 `TeethLipsDistancePoints` 与 `JawTeethDistancePoints`，以确认多头或空头趋势正在展开。
6. **反向信号出场**：若 `ReverseExit = OppositeFractal`，新的反向分形出现后立刻平仓。若为 `OppositePosition`，策略会先关闭当前持仓，再根据新的方向开仓。
7. **下颚出场**：`JawExit` 决定价格重新触及鳄鱼下颚时是否平仓，以及使用盘中高低价还是收盘价作为判定依据。
8. **移动止损**：`EnableTrailing` 启用时，且持仓已有浮盈，策略会比较鳄鱼“嘴唇”的斜率与 `SlopeSmaPeriod` 的 SMA：
   - 嘴唇斜率大于 SMA 斜率时，将止损上调/下调到嘴唇的位置；
   - 否则退守到牙齿位置。
   初始止损和止盈距离由 `StopLossPoints`、`TakeProfitPoints` 控制，值为 0 表示不设置。

## 参数

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 下单使用的交易量。 | `0.1` |
| `FractalPeriod` | 分形窗口长度（建议为奇数）。 | `5` |
| `IndentPoints` | 在分形价位上增加的偏移点数。 | `1` |
| `EntryConfirmation` | 突破确认方式（`PriceBreakout`、`CloseBreakout`）。 | `CloseBreakout` |
| `UseTeethFilter` | 是否要求上一根收盘价站在鳄鱼牙齿的同侧。 | `true` |
| `MaxDistancePoints` | 突破价位与嘴唇之间的最大允许距离（点）。 | `1000` |
| `UseTrendAlignment` | 是否启用鳄鱼线排列过滤。 | `false` |
| `JawTeethDistancePoints` | 下颚与牙齿之间的最小距离。 | `10` |
| `TeethLipsDistancePoints` | 牙齿与嘴唇之间的最小距离。 | `10` |
| `JawExit` | 触及下颚时的平仓模式（`Disabled`、`PriceCross`、`CloseCross`）。 | `CloseCross` |
| `ReverseExit` | 反向信号的处理方式（`Disabled`、`OppositeFractal`、`OppositePosition`）。 | `OppositePosition` |
| `EnableTrailing` | 是否启用鳄鱼线移动止损。 | `true` |
| `SlopeSmaPeriod` | 与嘴唇斜率对比的 SMA 周期。 | `5` |
| `StopLossPoints` | 止损距离（点，0 表示关闭）。 | `50` |
| `TakeProfitPoints` | 止盈距离（点，0 表示关闭）。 | `50` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | 鳄鱼三条线的周期。 | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | 每条线向前平移的柱数。 | `8`, `5`, `3` |
| `MaMethod` | 使用的移动平均类型（`Simple`、`Exponential`、`Smoothed`、`Weighted`）。 | `Simple` |
| `AppliedPrice` | 提供给 Alligator 的价格类型。 | `CandlePrice.Median` |
| `CandleType` | 订阅的 K 线类型。 | `15 分钟 K 线` |

## 其他说明

- 策略会在默认图表区域绘制 Alligator 三条线及成交记录。
- 为保证分形识别正确，`FractalPeriod` 建议保持为奇数；默认值与原版 EA 一致。
- 所有距离类参数（`IndentPoints`、`MaxDistancePoints`、`JawTeethDistancePoints`、`TeethLipsDistancePoints`、`StopLossPoints`、`TakeProfitPoints`）均以品种的最小报价步长 (`Security.PriceStep`) 为单位。
- 移动止损和下颚出场基于已完成的 K 线，与原始 MQL4 实现一致。
