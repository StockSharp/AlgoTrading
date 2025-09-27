# Price Based Z-Trend 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用价格相对于 EMA 的 Z 分数。Z 分数穿越设定阈值时入场，可选择多头、空头或双向。

## 详情

- **入场条件**：
  - Z 分数上穿 `Threshold` 做多。
  - Z 分数下穿 `-Threshold` 做空。
- **多空方向**：通过 `TradeDirection` 配置。
- **出场条件**：相反阈值的再次穿越。
- **止损**：无。
- **默认值**：
  - `PriceDeviationLength` = 100
  - `PriceAverageLength` = 100
  - `Threshold` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 分类：Trend
  - 方向：可配置
  - 指标：EMA, StandardDeviation
  - 止损：无
  - 复杂度：基础
  - 时间框架：5分钟
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
