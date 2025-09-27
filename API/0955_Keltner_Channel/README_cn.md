# Keltner通道策略
[English](README.md) | [Русский](README_ru.md)

该策略交易Keltner通道突破以及EMA趋势交叉。

## 细节

- **入场条件**：
  - 多头：价格跌破下轨后向上穿越，或EMA9上穿EMA21且价格高于EMA50。
  - 空头：价格突破上轨后向下穿越，或EMA9下穿EMA21且价格低于EMA50。
- **多空方向**：双向。
- **出场条件**：
  - 价格向相反方向穿越中轨或EMA反向交叉。
  - 止损为1.5倍ATR。
  - 止盈为3倍ATR。
- **止损**：有。
- **默认值**：
  - `Length` = 20
  - `Multiplier` = 1.5
  - `AtrMultiplier` = 1.5
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `TrendEmaPeriod` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类别：通道
  - 方向：双向
  - 指标：EMA, ATR, Keltner
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
