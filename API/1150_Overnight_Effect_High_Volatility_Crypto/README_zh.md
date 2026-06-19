# 隔夜高波动加密策略
[English](README.md) | [Русский](README_ru.md)

该策略在高波动的夜间入场做多，并在午夜前平仓。波动率通过指定周期的对数收益标准差计算，并与历史波动率的中位数比较。

## 细节

- **入场条件**：
  - 启用 `UseVolatilityFilter` 时：`currentHour == EntryHour && highVolatility`
  - 关闭过滤器时：`currentHour == EntryHour`
- **多空方向**：多头
- **止损**：无
- **默认参数**：
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **过滤器**：
  - 类别：时间
  - 方向：多头
  - 指标：StandardDeviation，Median
  - 止损：否
  - 复杂度：初级
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：低
