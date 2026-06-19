# 按时间和星期的 Sigma Spike 策略
[English](README.md) | [Русский](README_ru.md)

使用收益的 z 分数来识别各小时的大幅波动，可按星期过滤。
当出现尖峰时买入，波动恢复正常时卖出。

## 详情

- **入场条件**：绝对 z 分数 >= `Threshold`
- **多空**：仅做多
- **出场条件**：z 分数跌破 `Threshold`
- **止损**：无
- **默认值**：
  - `Threshold` = 2.5
  - `AllDays` = false
  - `DayOfWeekFilter` = Monday
  - `StdevLength` = 20
- **过滤器**：
  - 分类：波动率
  - 方向：多头
  - 指标：StandardDeviation
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：是
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
