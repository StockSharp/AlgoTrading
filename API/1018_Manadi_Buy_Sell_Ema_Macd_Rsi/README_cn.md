# Manadi 买卖 EMA MACD RSI 策略
[English](README.md) | [Русский](README_ru.md)

EMA 金叉死叉结合 MACD 和 RSI 过滤的策略。按市场价入场，使用百分比止损和止盈。

## 详情

- **入场条件**：EMA 交叉且 MACD 同向，RSI 处于限制范围。
- **多空方向**：双向。
- **出场条件**：按百分比的止损或止盈。
- **止损**：百分比。
- **默认值**：
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：Trend Following
  - 方向：双向
  - 指标：EMA, MACD, RSI
  - 止损：是
  - 复杂度：初级
  - 时间框架：日内 (1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
