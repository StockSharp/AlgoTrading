# Sidus v1 策略

## 概述
Sidus v1 是一套趋势型策略，结合了两组指数移动平均线（EMA）与 RSI 滤波器。原始的 MT4 智能交易系统在快 EMA 明显偏离慢 EMA 且 RSI 显示超卖或超买时入场。本移植版本保留了核心逻辑，仅允许在低成交量的蜡烛上交易，并针对多头与空头分别挂出不同的止盈止损订单。

## 使用的指标
- **快 EMA（做多）**：衡量短期动量。
- **慢 EMA（做多）**：过滤长期趋势方向。
- **快 EMA（做空）**：衡量做空前的短期动量。
- **慢 EMA（做空）**：过滤做空前的趋势方向。
- **RSI（做多）**：确认超卖状态。
- **RSI（做空）**：确认超买状态。

## 交易逻辑
1. 订阅所选周期的蜡烛数据（默认 15 分钟）。
2. 在每根收盘蜡烛上更新全部 EMA 与 RSI。
3. 当蜡烛成交量超过阈值（默认 10）时跳过信号。
4. **做多条件**：
   - 快 EMA 与慢 EMA 的差值低于做多阈值。
   - RSI 低于做多阈值。
   - 当前净头寸不为正（允许平空并转多）。
5. **做空条件**：
   - 做空 EMA 组合的差值高于做空阈值。
   - 做空 RSI 高于做空阈值。
   - 当前净头寸不为负（允许平多并转空）。
6. 触发信号后取消挂单，按需平仓并建立新的市场单，然后立即挂出相应方向的止盈与止损单。

## 风险控制
- 多头止盈价：`entry + BuyTakeProfitPips * priceStep`；止损价：`entry - BuyStopLossPips * priceStep`。
- 空头止盈价：`entry - SellTakeProfitPips * priceStep`；止损价：`entry + SellStopLossPips * priceStep`。
- 参数以“点”为单位，通过合约的最小价格变动（priceStep）换算成价格。若标的的 tick 大小不同，请调整相关参数。

## 参数列表
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `FastEmaLength` | 做多信号的快 EMA 长度 | 23 |
| `SlowEmaLength` | 做多信号的慢 EMA 长度 | 62 |
| `FastEma2Length` | 做空信号的快 EMA 长度 | 18 |
| `SlowEma2Length` | 做空信号的慢 EMA 长度 | 54 |
| `RsiPeriod` | 做多 RSI 周期 | 67 |
| `RsiPeriod2` | 做空 RSI 周期 | 97 |
| `BuyDifferenceThreshold` | 做多时允许的最大 EMA 差值 | 63 |
| `BuyRsiThreshold` | 做多时允许的最大 RSI 值 | 59 |
| `SellDifferenceThreshold` | 做空时需要的最小 EMA 差值 | -57 |
| `SellRsiThreshold` | 做空时需要的最小 RSI 值 | 60 |
| `BuyTakeProfitPips` | 多头止盈点数 | 95 |
| `BuyStopLossPips` | 多头止损点数 | 100 |
| `SellTakeProfitPips` | 空头止盈点数 | 17 |
| `SellStopLossPips` | 空头止损点数 | 69 |
| `OrderVolume` | 下单数量 | 0.5 |
| `MaxCandleVolume` | 允许的最大蜡烛成交量 | 10 |
| `CandleType` | 指标计算所用的蜡烛类型 | 15 分钟蜡烛 |

## 使用建议
- 确认交易通道支持同时挂出市价单、止损单和限价单，以便策略布设保护订单。
- 如标的价格最小变动与 MT4 的 `Point` 不同，请调整止盈止损点数。
- 策略基于净头寸管理，改变方向时会先平掉已有仓位，再建立新的方向。
