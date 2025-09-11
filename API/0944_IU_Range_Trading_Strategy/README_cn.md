# IU 区间交易策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格在特定周期内的高低波动小于 ATR 乘数时认定为区间整理。当价格突破区间上限或下限时开仓，并使用基于 ATR 的移动止损跟随趋势。

## 详情

- **入场条件**：突破 ATR 定义的窄幅区间。
- **多空方向**：双向。
- **出场条件**：基于 ATR 的移动止损。
- **止损**：有。
- **默认参数**：
  - `RangeLength` = 10
  - `AtrLength` = 14
  - `AtrTargetFactor` = 2.0m
  - `AtrRangeFactor` = 1.75m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：突破
  - 方向：双向
  - 指标：ATR, Highest, Lowest
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
