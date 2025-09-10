# 图表振荡器
[English](README.md) | [Русский](README_ru.md)

该策略使用可选择的振荡器进行交易，可在随机指标、RSI 或 MFI 之间选择。当振荡器显示超卖时买入，超买时卖出。对于随机指标，信号基于 %K 和 %D 的交叉。

测试表明它在加密货币等波动性市场表现良好。

当出现相反条件或触发止损时，仓位将反转。

## 细节

- **入场条件**：振荡器超卖/超买以及 %K/%D 交叉。
- **多空方向**：双向。
- **出场条件**：相反信号或止损。
- **止损**：有。
- **默认值**：
  - `Choice` = OscillatorChoice.Stochastic
  - `Length` = 14
  - `KPeriod` = 14
  - `DPeriod` = 3
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **过滤器**：
  - 类别: Oscillator
  - 方向: 双向
  - 指标: Stochastic/RSI/MFI
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

