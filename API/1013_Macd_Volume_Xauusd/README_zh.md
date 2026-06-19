# MACD Volume XAUUSD 策略
[English](README.md) | [Русский](README_ru.md)

该策略在15分钟XAUUSD上运行，结合MACD零轴交叉与成交量振荡器过滤，并使用固定风险参数。

## 详情

- **入场条件**：MACD穿越零轴，成交量振荡器为正，且比较当前与前一根K线的成交量。
- **多空方向**：双向。
- **出场条件**：止损或止盈水平。
- **止损**：固定止损与止盈倍数。
- **默认参数**：
  - `ShortLength` = 5
  - `LongLength` = 8
  - `FastLength` = 16
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Leverage` = 1.0
  - `StopLoss` = 10100
  - `TakeProfitMultiplier` = 1.1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**：
  - 类别：Trend Following
  - 方向：双向
  - 指标：MACD, EMA, Volume
  - 止损：是
  - 复杂度：基础
  - 周期：日内 (15m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
