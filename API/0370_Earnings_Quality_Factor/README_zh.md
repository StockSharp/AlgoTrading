# Earnings Quality Factor
[English](README.md) | [Русский](README_ru.md)

**Earnings Quality Factor** 策略每年 7 月 1 日再平衡，依据盈利质量得分买入高质量股票并卖出低质量股票。

## 细节
- **入场条件**：每年 7 月 1 日根据质量得分再平衡。
- **方向**：双向。
- **出场条件**：下一次年度再平衡。
- **止损**：无。
- **默认值**：
  - `MinTradeUsd = 100`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **筛选**：
  - 类别：基本面
  - 方向：双向
  - 指标：质量
  - 止损：无
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：是
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
