# MACD 背离

MACD 背离策略寻找价格走势与 MACD 指标之间的不一致。当价格创出更高高点而 MACD 高点却更低时，表明动能减弱（看跌背离）；相反，价格创出更低低点而 MACD 低点抬高，则可能预示着看涨反转。

发现背离后，系统等待 MACD 上穿其信号线再入场。若 MACD 再次反向穿越或触发止损则平仓。

## 细节

- **入场条件**：出现看涨或看跌背离并且 MACD 穿越信号线。
- **多/空**：双向。
- **退出条件**：MACD 反向穿越或触发止损。
- **止损**：有。
- **默认值**：
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `DivergencePeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 2.0m
- **过滤条件**：
  - 类别: 背离
  - 方向: 双向
  - 指标: MACD
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 是
  - 风险级别: 中等
