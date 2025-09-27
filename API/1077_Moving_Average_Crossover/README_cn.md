# Moving Average Crossover 策略
[English](README.md) | [Русский](README_ru.md)

当短期SMA向上穿越长期SMA时买入，向下穿越时卖出。出现反向信号时仓位反转。

## 详情

- **入场条件**：
  - 短期SMA上穿长期SMA时做多。
  - 短期SMA下穿长期SMA时做空。
- **多空方向**：双向。
- **出场条件**：反向交叉时反转仓位。
- **止损**：无。
- **默认值**：
  - `ShortLength` = 9
  - `LongLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：Crossover
  - 方向：双向
  - 指标：SMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

