# XMACD多模式策略
[English](README.md) | [Русский](README_ru.md)

该策略基于MACD指标，提供四种入场模式：

- **Breakdown**：当MACD穿越零线时开仓。
- **MacdTwist**：当MACD方向由下转上或由上转下时触发。
- **SignalTwist**：以信号线的转折点作为信号。
- **MacdDisposition**：根据MACD与信号线的交叉进行交易。

策略订阅4小时K线，并计算经典MACD（12/26 EMA，信号线9）。出现相反信号时可反向或平仓。风险通过以入场价格百分比表示的止损和止盈进行管理。

## 详情

- **入场条件**：取决于所选模式的MACD信号。
- **多空方向**：双向。
- **退出条件**：相反信号或止损/止盈。
- **止损**：是。
- **默认值**：
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
  - `Mode` = MacdDisposition
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **过滤器**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: MACD
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 波段 (4h)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
