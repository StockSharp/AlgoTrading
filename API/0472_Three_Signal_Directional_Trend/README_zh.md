# Three Signal Directional Trend 策略
[English](README.md) | [Русский](README_ru.md)

**Three Signal Directional Trend** 策略结合 MACD、随机指标和移动平均变化率 (ROC) 三个信号来判断趋势。当至少两个信号方向一致时开仓。多重确认有助于过滤噪音并跟随主要趋势。

## 细节

- **入场条件：**
  - 三个信号中至少两个一致。
  - **做多**：MACD 信号上升、随机指标低于超卖、MA ROC 为正。
  - **做空**：MACD 信号下降、随机指标高于超买、MA ROC 为负。
- **多空方向**：双向。
- **出场条件：**
  - 反向信号。
- **止损**：无。
- **默认值：**
  - `AvgLength` = 50
  - `RocLength` = 1
  - `AvgRocLength` = 10
  - `StochLength` = 14
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdAvgLength` = 9
- **过滤器：**
  - 分类：趋势跟随
  - 方向：双向
  - 指标：MACD、Stochastic、SMA、ROC
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
