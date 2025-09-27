# Pavan CPR 策略
[English](README.md) | [Русский](README_ru.md)

当价格在前一收盘位于顶部 CPR 之下后向上突破该水平时做多。止损放在枢轴点，固定距离设置止盈。

## 详情

- **入场条件**：前一收盘低于顶部 CPR，当前收盘突破该水平。
- **多空方向**：仅多头。
- **出场条件**：达到止盈或触及枢轴止损。
- **止损**：是。
- **默认值**：
  - `TakeProfitTarget` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：Breakout
  - 方向：Long
  - 指标：Pivot
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
