# New Intraday High With Weak Bar 策略
[English](README.md) | [Русский](README_ru.md)

当出现新的 `HighestLength` 根K线的最高价且蜡烛收盘接近最低价时做多；当价格收于前一根K线高点之上时平仓。

## 详情

- **入场条件**：
  - 无持仓，最高价等于过去 `HighestLength` 根K线的最高价且 `(close - low)/(high - low) < WeakRatio`。
- **多空方向**：仅做多。
- **出场条件**：收盘价高于前一根K线的高点。
- **止损**：无。
- **默认值**：
  - `HighestLength` = 10
  - `WeakRatio` = 0.15
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**：
  - 分类：突破
  - 方向：做多
  - 指标：Highest
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
