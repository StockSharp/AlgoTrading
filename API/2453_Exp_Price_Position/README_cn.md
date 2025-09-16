# Exp Price Position
[English](README.md) | [Русский](README_ru.md)

**Exp Price Position** 策略来自原始的 MetaTrader 专家顾问，它结合价格位置与趋势过滤。
策略通过两条中值移动平均线确定最近的摆动水平，然后使用一对平滑移动平均线来识别趋势方向。
只有当价格位置与趋势方向一致并且与当前K线结构相符时才会开仓。

该策略适用于在价格回撤到动态中值水平后趋势反转的市场。通过移动止损和收益比来管理风险。

## 细节

- **入场条件**：价格在最后摆动水平之上且趋势向上时做多；在其下方且趋势向下时做空。
- **做多/做空**：双向。
- **出场条件**：反向信号或保护性止损。
- **止损**：有，采用移动止损和盈亏比。
- **默认值**：
  - `FastPeriod` = 2
  - `SlowPeriod` = 30
  - `MedianFastPeriod` = 26
  - `MedianSlowPeriod` = 20
  - `TpSlRatio` = 3m
  - `TrailingStopPips` = 10m
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：Smoothed Moving Average, Simple Moving Average
  - 止损：移动止损
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险水平：中等

