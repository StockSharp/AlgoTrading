# DreamBot 策略

## 概述
DreamBot 是 MetaTrader 4 顾问 "DreamBot" 的 StockSharp 版本。策略在 1 小时 K 线上计算 Force Index 振荡指标，并等待动量突破多头或空头阈值。当 Force Index 在上一根 K 线上自下而上穿越多头阈值时开多；当上一根 K 线的数值自上而下跌破空头阈值时开空。与原始 EA 一样，只有在仓位为空时才允许入场。

## 交易逻辑
- 订阅 H1 K 线并计算平滑的 Force Index（默认长度 13）。
- 保存最近两根已完成 K 线的 Force Index 数值，完全复刻 MT4 中带有 shift=1、2 的 `iForce` 调用方式。
- 当上一根 K 线的 Force Index 高于 `BullsThreshold`，而再往前一根 K 线低于该阈值、且当前没有仓位时，执行买入。
- 当上一根 K 线的 Force Index 低于 `BearsThreshold`，而再往前一根 K 线高于该阈值、且当前没有仓位时，执行卖出。
- 可选的拖尾止损与原版一致：利润超过 `TrailingStepPoints` 后，将止损拉至距离价格 `TrailingStartPoints` 的位置，并随行情移动。

## 风险管理
- 通过 `StartProtection` 把 MetaTrader “点”单位的止盈止损转换为价格步长，并自动附加到仓位。
- 拖尾保护采用市价平仓：当价格触及计算出的拖尾水平时，立即发送市价单关闭仓位。
- 通过成交回报计算加权平均入场价，确保拖尾逻辑能够处理部分成交和反向操作。

## 参数
| 参数 | 说明 |
|------|------|
| `ForcePeriod` | Force Index 平滑周期（默认 13）。 |
| `TakeProfitPoints` | 以 MetaTrader 点为单位的止盈距离。 |
| `StopLossPoints` | 以 MetaTrader 点为单位的止损距离。 |
| `BullsThreshold` | 触发做多的 Force Index 阈值。 |
| `BearsThreshold` | 触发做空的 Force Index 阈值。 |
| `EnableTrailing` | 是否启用拖尾止损。 |
| `TrailingStartPoints` | 拖尾激活后保持的点数距离。 |
| `TrailingStepPoints` | 激活拖尾所需的盈利点数。 |
| `CandleType` | 用于指标计算的 K 线类型（默认 H1）。 |

## 说明
- 参数校验会阻止 `TrailingStepPoints` 大于 `TrailingStartPoints`，以符合原始 EA 的安全检查。
- 原策略依赖经纪商的 `MODE_STOPLEVEL` 限制，这里通过价格步长近似处理，必要时可补充更严格的校验。
- 根据项目要求，代码注释和日志均保持为英文。
