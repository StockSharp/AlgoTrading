# Hpcs Inter7 策略

## 概述
Hpcs Inter7 策略是从 MetaTrader 4 专家顾问 `_HPCS_Inter7_MT4_EA_V01_We.mq4` 转换而来的布林带突破系统。算法在所选 K 线序列上计算标准布林带，当收盘价向带外突破时，会顺势开仓。每次开仓后都会立即设置固定距离的止损与止盈，以复现原始 EA 的风险控制方式。

## 交易逻辑
- **做空入场**：若前一根 K 线收盘价位于下轨之上，而最新完成的 K 线收盘价跌破下轨，则以市价卖出，对应原始条件 `Close[0] < LowerBand[0] && Close[1] > LowerBand[1]`。
- **做多入场**：若前一根 K 线收盘价位于上轨之下，而最新完成的 K 线收盘价突破上轨，则以市价买入，复现条件 `Close[0] > UpperBand[0] && Close[1] < UpperBand[1]`。
- **每根 K 线仅一笔交易**：策略记录触发信号的 K 线开盘时间，如果同一根 K 线再次出现信号将被忽略，对应 MQL4 中的 `gdt_Candle` 变量。
- **保护性订单**：在开仓后立即调用 `SetStopLoss` 与 `SetTakeProfit`，按照配置的距离在入场价上下对称放置止损止盈，确保持仓始终具有预定义的风险与收益。

## 参数
| 名称 | 说明 | 默认值 | 可优化 |
| --- | --- | --- | --- |
| `BollingerLength` | 参与布林带计算的 K 线数量。 | 20 | 是 |
| `BollingerDeviation` | 布林带宽度所使用的标准差倍数。 | 2 | 是 |
| `CandleType` | 用于计算的 K 线类型（默认 1 分钟）。 | 1 分钟 K 线 | 否 |
| `ProtectionDistancePoints` | 以价格步长为单位的止损与止盈距离。 | 10 | 是 |

## 其他说明
- 策略使用 StockSharp 高阶 API（`SubscribeCandles().Bind(...)`），无需维护自定义历史数组。
- 启动时调用 `StartProtection()`，平台会自动管理由 `SetStopLoss` 与 `SetTakeProfit` 创建的保护单。
- 下单量由基类 `Strategy.Volume` 控制，与原始 EA 固定 1 手的做法一致。
- 该策略源自外汇市场，但只要标的提供有效的布林带信号并具有合理的 `PriceStep`，同样可以应用于其他品种。
