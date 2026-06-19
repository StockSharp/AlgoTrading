# Straddle News 策略
[English](README.md) | [Русский](README_ru.md)

该策略用于重大新闻时的高波动行情。它在当前价格上下放置对称的 Buy Stop 和 Sell Stop 挂单，以捕捉突破。当其中一单被触发后，另一单会被取消，并使用追踪止损保护头寸。

## 细节

- **入场条件**: 当点差低于 `SpreadOperation` 时，分别在 Ask + `PipsAway` 点和 Bid - `PipsAway` 点放置 Buy Stop 和 Sell Stop
- **多空方向**: 双向
- **出场条件**: 保护性止损、止盈或价格回撤 `TrailingStop` 点触发的追踪止损
- **止损**: 初始止损和止盈通过 `StartProtection` 设置；追踪止损在代码中实现
- **默认值**:
  - `StopLoss` = 100
  - `TakeProfit` = 300
  - `TrailingStop` = 50
  - `PipsAway` = 50
  - `BalanceUsed` = 0.01
  - `SpreadOperation` = 25
  - `Leverage` = 400
- **过滤器**:
  - 分类: Breakout
  - 方向: 双向
  - 指标: 无
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: Level1 / Tick
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 高

## 工作流程

1. 订阅 Level1 行情以获取最新 Bid 和 Ask。
2. 当点差满足条件时，依据账户价值、`Leverage` 和 `BalanceUsed` 计算交易量。
3. 按 `PipsAway` 的偏移放置 Buy Stop 和 Sell Stop。
4. 当持仓打开后，取消未触发的另一张挂单。
5. 根据 `StopLoss` 和 `TakeProfit` 附加止损与止盈。
6. 跟踪进场后的最高/最低价，若价格回撤超过 `TrailingStop` 点则平仓。
