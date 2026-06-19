# RSI Automated Strategy
[English](README.md) | [Русский](README_ru.md)

利用相对强弱指数（RSI）在极度超卖和超买状态下进行交易的动量策略。
当RSI跌破超卖水平时开多仓，当RSI升破超买水平时开空仓。
当RSI回到中间阈值或触发止损、止盈或追踪止损时平仓。

## 细节

- **入场条件**：RSI跌破 `Oversold` 做多或升破 `Overbought` 做空。
- **多空方向**：双向。
- **出场条件**：RSI触及 `ExitLevel`、止损、止盈或追踪止损。
- **止损**：是，固定止损、止盈以及可选的追踪止损。
- **默认值**：
  - `RsiPeriod` = 14
  - `Overbought` = 75
  - `Oversold` = 25
  - `ExitLevel` = 50
  - `StopLossPoints` = 50
  - `TakeProfitPoints` = 150
  - `TrailingStopPoints` = 25
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：振荡指标
  - 方向：双向
  - 指标：RSI
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
