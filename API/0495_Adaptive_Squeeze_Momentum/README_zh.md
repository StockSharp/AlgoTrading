# Adaptive Squeeze Momentum 策略
[English](README.md) | [Русский](README_ru.md)

Adaptive Squeeze Momentum 策略在布林带进入肯特纳通道时识别波动性收缩，并等待伴随强动量的突破。动量强度通过标准差阈值衡量。可选的 RSI 和 EMA 趋势过滤器帮助优化进场。ATR 可用于设置动态止损和止盈，持仓在预定的时间周期后自动关闭。

## 细节

- **入场条件**：
  - 挤压结束（布林带在肯特纳通道外）。
  - **多头**：动量 > 动态阈值，RSI 上穿超卖位，EMA 上升（可选）。
  - **空头**：动量 < -动态阈值，RSI 下穿超买位，EMA 下降（可选）。
- **方向**：双向。
- **出场条件**：
  - 反向信号、ATR 止损/止盈或时间退出。
- **止损**：可选的 ATR 止损和止盈。
- **默认参数**：
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0
  - `KeltnerPeriod` = 20
  - `KeltnerMultiplier` = 1.5
  - `MomentumLength` = 12
  - `TrendMaLength` = 50
  - `UseAtrStops` = True
  - `AtrMultiplierSl` = 1.5
  - `AtrMultiplierTp` = 2.5
  - `AtrLength` = 14
  - `MinVolatility` = 0.5
  - `HoldingPeriodMultiplier` = 1.5
  - `UseTrendFilter` = True
  - `UseRsiFilter` = True
  - `RsiLength` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `MomentumMultiplier` = 1.5
  - `AllowLong` = True
  - `AllowShort` = True
- **过滤器**：
  - 类型：波动性突破
  - 方向：双向
  - 指标：布林带、肯特纳通道、动量、RSI、EMA、ATR
  - 止损：可选
  - 复杂度：高
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
