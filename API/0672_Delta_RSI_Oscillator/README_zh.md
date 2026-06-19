# Delta-RSI Oscillator
[Русский](README_ru.md) | [English](README.md)

该策略使用Delta-RSI振荡器，它是RSI的变化并用EMA平滑。当Delta-RSI穿越零轴、穿越信号线或改变方向时产生信号，退出条件与所选条件相同。

## 细节

- **入场条件**：根据`BuyCondition`（零轴穿越、信号线穿越或方向改变）。
- **多/空**：两者，受`UseLong`和`UseShort`控制。
- **出场条件**：基于`ExitCondition`。
- **止损**：无。
- **默认值**：
  - `RsiLength` = 21
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤**：
  - 分类：Momentum
  - 方向：双向
  - 指标：RSI, EMA
  - 止损：无
  - 复杂度：中
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险水平：中
