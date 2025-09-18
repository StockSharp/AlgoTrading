# SMC Trader Camel CCI MACD 策略

## 概述

本策略为 MetaTrader 4 指标 **“Steve Cartwright Trader Camel CCI MACD”** 的 StockSharp 版本。
策略复刻了原始 EA 的核心逻辑：使用基于最高价/最低价的 EMA 通道、MACD 趋势过滤以及 CCI 阈值判断。
所有交易决策都在完成的 K 线之后执行，以保持与 MQL4 中逐 K 检查的行为一致。

## 交易逻辑

1. **指标组件**
   - 两条相同周期的指数移动平均线 (EMA) 分别作用于 K 线最高价和最低价，形成 Camel 通道。
     前一根 K 线的收盘价突破该通道的上沿或下沿意味着动能增强。
   - 标准 MACD (快线 EMA、慢线 EMA、信号线) 用于确认趋势方向。
   - CCI 指标利用 ±100 的阈值检验动能强度。
2. **做多条件**
   - 前一根 K 线收盘价高于 Camel 通道上轨。
   - 前一根 K 线的 MACD 主线大于 0 且高于信号线。
   - 前一根 K 线的 CCI 高于正向阈值。
   - 当前无持仓，且上一次离场到现在至少过去一个 K 线周期。
3. **做空条件**
   - 前一根 K 线收盘价低于 Camel 通道下轨。
   - 前一根 K 线的 MACD 主线小于 0 且低于信号线。
   - 前一根 K 线的 CCI 低于负向阈值。
   - 同样要求空仓并满足冷却时间。
4. **离场规则**
   - 多单：当前一根 K 线的 MACD 主线跌破信号线，或 CCI 回落至阈值以下时平仓。
   - 空单：当前一根 K 线的 MACD 主线上穿信号线时平仓。
   - 每次平仓后都会记录离场时间，在一个 K 线周期内禁止再次开仓。

由于全部判断都基于前一根 K 线的数据，策略每根 K 线最多只会触发一笔交易。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 用于计算指标的蜡烛类型与周期。 | 1 小时 K 线 |
| `CamelLength` | Camel 通道的 EMA 周期。 | 34 |
| `CciPeriod` | CCI 指标周期。 | 20 |
| `MacdFastPeriod` | MACD 快速 EMA 周期。 | 12 |
| `MacdSlowPeriod` | MACD 慢速 EMA 周期。 | 26 |
| `MacdSignalPeriod` | MACD 信号线平滑周期。 | 9 |
| `CciThreshold` | CCI 入场阈值，正负方向对称使用。 | 100 |

所有参数均已配置 `SetOptimize`，可直接在 StockSharp 优化器中进行网格搜索。

## 风险管理

- 下单通过 `BuyMarket` 与 `SellMarket` 方法执行，默认使用策略的 `Volume` 设置。
- `StartProtection()` 被调用以启用 StockSharp 的通用保护机制。
- 原版 EA 未使用固定止损/止盈，本策略同样仅依赖指标信号离场。

## 图表展示

策略会在图表上绘制 Camel EMA 通道、MACD、CCI 以及自己的成交记录，帮助还原原始 EA 的视觉提示。

## 其他说明

- 冷却时间基于 `CandleType.Arg` 中的 `TimeSpan` 计算；切换周期时请确认该参数有效。
- 使用前一根 K 线数据的方式，与 MQL4 中调用 `iMACD`、`iCCI`、`iMA` 并传入 `shift = 1` 的效果保持一致。
