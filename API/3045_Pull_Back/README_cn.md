# Pull Back 策略

## 概述

Pull Back 策略将 MetaTrader 平台的 "PULL BACK" EA 迁移到 StockSharp 的高层 API。策略在更高周期的快速加权移动平均线上寻找回调，结合多根 K 线的动量强度，并依据长周期 MACD 的方向入场。开仓后，策略执行完整的资金管理，包括止损、止盈、保本和平滑跟踪止损。

## 数据与指标

- **交易周期：** 可配置蜡烛类型 (`CandleType`，默认 15 分钟)。
- **确认周期：** 单独订阅 (`HigherCandleType`，默认 1 小时) 用于：
  - 快速/慢速加权移动平均线 (WMA)。
  - 动量指标，计算与 100 的绝对偏离。
  - 检测上一根蜡烛是否回踩快速 WMA。
- **MACD 周期：** 单独订阅 (`MacdCandleType`，默认 30 天) 判断 MACD 线与信号线的关系。
- **使用指标：**
  - 交易周期与确认周期的 WMA。
  - 确认周期的 Momentum。
  - 长周期的 MACD。

## 交易逻辑

### 多头条件

1. 确认周期快速 WMA 高于慢速 WMA。
2. 最近完成的确认周期蜡烛开盘价位于快速 WMA 上方，最低价触及该均线。
3. 最近三次 |Momentum-100| 中至少一次超过 `MomentumBuyThreshold`。
4. MACD 主线在 MACD 周期上位于信号线之上。
5. 交易周期内快速 WMA 高于慢速 WMA。

满足以上条件时，策略按市价买入，并记录入场价以便风险控制。

### 空头条件

1. 确认周期快速 WMA 低于慢速 WMA。
2. 最近蜡烛开盘价位于快速 WMA 下方，最高价触及该均线。
3. 最近三次 |Momentum-100| 中至少一次超过 `MomentumSellThreshold`。
4. MACD 主线位于信号线下方。
5. 交易周期内快速 WMA 低于慢速 WMA。

满足条件时，策略按市价卖出开空。

## 持仓管理

- **止损：** 距离入场价 `StopLossTicks` 个最小跳动，根据 `Security.PriceStep` 转换为价格差。
- **止盈：** 距离入场价 `TakeProfitTicks` 个跳动。
- **保本：** 当浮盈达到 `BreakEvenTriggerTicks` 时，如启用 `UseBreakEven`，将止损调整到入场价加上 `BreakEvenOffsetTicks`（多头）或减去该值（空头）。
- **跟踪止损：** `UseTrailingStop` 为 true 时，止损按 `TrailingStopTicks` 的距离跟随价格。
- **退出检查：** 在每根完成的交易周期蜡烛上执行；触发止损或止盈后通过市价单平仓。

## 参数

| 参数 | 说明 |
|------|------|
| `FastMaLength` | 交易周期快速 WMA 长度（默认 6）。 |
| `SlowMaLength` | 交易周期慢速 WMA 长度（默认 85）。 |
| `BounceSlowLength` | 确认周期慢速 WMA 长度（默认 200）。 |
| `MomentumLength` | 确认周期 Momentum 周期（默认 14）。 |
| `MomentumBuyThreshold` | 多头所需的最小 |Momentum-100|（默认 0.3）。 |
| `MomentumSellThreshold` | 空头所需的最小 |Momentum-100|（默认 0.3）。 |
| `StopLossTicks` | 止损距离（以跳动计，默认 200）。 |
| `TakeProfitTicks` | 止盈距离（以跳动计，默认 500）。 |
| `UseTrailingStop` | 是否启用跟踪止损（默认 true）。 |
| `TrailingStopTicks` | 跟踪止损距离（默认 400）。 |
| `UseBreakEven` | 是否启用保本移动（默认 true）。 |
| `BreakEvenTriggerTicks` | 触发保本的跳动数（默认 300）。 |
| `BreakEvenOffsetTicks` | 保本后止损的偏移跳动数（默认 300）。 |
| `MacdFastLength` | MACD 快速 EMA 周期（默认 12）。 |
| `MacdSlowLength` | MACD 慢速 EMA 周期（默认 26）。 |
| `MacdSignalLength` | MACD 信号 EMA 周期（默认 9）。 |
| `CandleType` | 交易周期蜡烛类型。 |
| `HigherCandleType` | 确认周期蜡烛类型。 |
| `MacdCandleType` | MACD 计算所用蜡烛类型。 |

## 说明

- 需要提供 `Security.PriceStep` 才能正确换算跳动距离。
- 策略同一时间只持有一个净头寸；持仓期间不会生成反向开仓信号。
- 仅处理已经完成的蜡烛，避免使用未完结数据做出决策。
