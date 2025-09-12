# SMA RSI Volume ATR 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合简单移动平均线（SMA）、相对强弱指数（RSI）、成交量确认以及基于 ATR 的波动性过滤。
当价格高于 SMA、RSI 低于超卖水平、成交量高于其均值乘以阈值且波动性上升时买入；相反条件下卖出。

止损与止盈使用固定百分比。

## 详情

- **入场条件**：
  - **做多**：`Close > SMA` && `RSI < RsiOversold` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
  - **做空**：`Close < SMA` && `RSI > RsiOverbought` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
- **多空方向**：双向
- **出场条件**：止损或止盈
- **止损**：是，百分比
- **默认值**：
  - `SmaLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `VolumeThreshold` = 1.5
  - `AtrLength` = 14
  - `TakeProfitPerc` = 1.5
  - `StopLossPerc` = 0.5
- **筛选器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA、RSI、成交量、ATR
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
