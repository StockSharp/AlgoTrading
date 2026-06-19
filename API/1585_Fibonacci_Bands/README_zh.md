# Fibonacci Bands 策略
[English](README.md) | [Русский](README_ru.md)

利用斐波那契比率扩展肯特纳通道，当价格突破外带并结合 RSI 确认时交易。

## 细节

- **入场条件**：价格突破 `fbUpper3` 且 RSI > 60 做多；突破 `fbLower3` 且 RSI < 40 做空。
- **多空**：双向。
- **出场条件**：价格回到移动平均线。
- **止损**：否。
- **默认值**：
  - `MaType` = WMA
  - `MaLength` = 233
  - `Fib1` = 1.618
  - `Fib2` = 2.618
  - `Fib3` = 4.236
  - `KcMultiplier` = 2
  - `KcLength` = 89
  - `RsiLength` = 14
  - `CandleType` = 5 分钟
- **过滤器**：
  - 类别: Volatility
  - 方向: 双向
  - 指标: MA, ATR, RSI
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
