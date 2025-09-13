# Aver4 Stoch Post ZigZag
[English](README.md) | [Русский](README_ru.md)

该策略结合了四个不同周期的 Stochastic 振荡器和一个简单的 ZigZag 枢轴检测器。平均 Stochastic 用于判断超买和超卖水平，而 ZigZag 用于确认新的高点和低点。当平均 Stochastic 低于超卖阈值并出现新的 ZigZag 低点时买入；当平均 Stochastic 高于超买阈值并出现新的 ZigZag 高点时卖出。反向信号出现时会平掉相反的持仓。

## 细节
- **入场条件**：平均 Stochastic 穿越超买/超卖水平并出现匹配的 ZigZag 枢轴。
- **多空方向**：双向。
- **出场条件**：相反信号。
- **止损**：StartProtection 2%/2%（默认）。
- **默认值**：
  - `ShortLength` = 26
  - `MidLength1` = 72
  - `MidLength2` = 144
  - `LongLength` = 288
  - `ZigZagDepth` = 14
  - `Oversold` = 5
  - `Overbought` = 95
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**：
  - 分类：Oscillator
  - 方向：双向
  - 指标：Stochastic, ZigZag
  - 止损：是
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
