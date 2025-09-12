# IMACD 狙击手
[English](README.md) | [Русский](README_ru.md)

IMACD Sniper 策略结合 MACD 金叉/死叉、EMA 趋势过滤、成交量确认和强势 K 线。基于近期平均波动的动态止盈与止损。

## 细节
- **数据**：价格 K 线。
- **入场条件**：
  - **多头**：MACD 线向上穿越信号线，价格高于 EMA，MACD 差值超过最小值，两条 MACD 线远离零轴，成交量高于均值，出现强势多头 K 线。
  - **空头**：MACD 线向下穿越信号线，价格低于 EMA，MACD 差值超过最小值，两条 MACD 线远离零轴，成交量高于均值，出现强势空头 K 线。
- **出场条件**：反向 MACD 交叉或达到止盈/止损。
- **止损**：基于平均波动的动态止盈与止损。
- **默认参数**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdDeltaMin` = 0.03
  - `MacdZeroLimit` = 0.05
  - `RangeLength` = 14
  - `RangeMultiplierTp` = 4.0
  - `RangeMultiplierSl` = 1.5
  - `EmaLength` = 20
  - `CandleType` = tf(1m)
- **过滤器**：
  - 类别：趋势
  - 方向：多 & 空
  - 指标：MACD、EMA、成交量
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
