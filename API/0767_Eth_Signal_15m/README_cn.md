# ETH Signal 15m 策略
[English](README.md) | [Русский](README_ru.md)

ETH Signal 15m 策略使用 Supertrend 指标检测方向变化，并用 RSI 过滤信号。当 Supertrend 方向下降且 RSI 低于超买水平时开多仓；当 Supertrend 方向上升且 RSI 高于超卖水平时开空仓。退出使用基于 ATR 的止损和止盈。

## 细节

- **入场条件**:
  - **多头**: Supertrend 方向下降且 RSI 低于 `RsiOverbought`。
  - **空头**: Supertrend 方向上升且 RSI 高于 `RsiOversold`。
- **多空**: 双向。
- **出场条件**: 基于 ATR 的止损和止盈。
- **止损/止盈**: 止损 4×ATR，多头止盈 2×ATR，空头止盈 2.237×ATR。
- **默认值**:
  - `AtrPeriod` = 12
  - `Factor` = 2.76
  - `RsiLength` = 12
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: Supertrend, RSI, ATR
  - 止损: ATR 止损和止盈
  - 复杂度: 低
  - 时间框架: 15m
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
