# 线性回归通道策略（Fibo 版本）

## 概述
该策略基于 MetaTrader 专家顾问“linear regression channel”，在 StockSharp 平台上重新实现。策略利用更高周期的线性趋势、加权移动均线、动量指标以及月度 MACD 过滤器来判断方向，同时保留了原始 EA 中的资金管理逻辑，包括浮动盈利目标、盈利回撤保护、保本止损以及权益回撤止损。

## 交易逻辑
1. **主时间框架**：可配置的 K 线类型（默认 15 分钟），所有信号均在此时间框架上计算。
2. **趋势过滤**：计算典型价格的快慢线性加权移动均线（LWMA）。做多要求快线高于慢线，做空要求快线低于慢线。
3. **动量确认**：在更高周期上计算动量指标，周期映射与原始 EA 一致（M1→M15、M5→M30、M15→H1、M30→H4、H1→D1、H4→W1、D1→MN1）。取最近三根动量值，与 100 水平的距离需满足多/空阈值。
4. **月度 MACD 偏向**：使用月线计算 MACD(12,26,9)，当主线高于信号线时仅允许做多，当主线低于信号线时仅允许做空。
5. **入场条件**：当趋势、动量及 MACD 一致并允许交易时，以市价开仓；出现相反信号时平仓并可反向。

## 风险与仓位管理
- **固定止损 / 止盈**：以点数表示的距离应用到每笔交易，价格触发即平仓。
- **移动止损**：可选，当盈利达到设定点数后，按照最佳价格减去偏移值进行追踪。
- **保本逻辑**：可选，盈利超过触发值后，将止损移动到入场价加/减偏移，锁定收益。
- **货币止盈**：可选，浮动盈利（账户货币）达到阈值时立即全部平仓。
- **百分比止盈**：可选，按策略启动时的初始权益百分比计算目标盈利。
- **盈利回撤保护**：当浮动盈利达到触发值时记录峰值，若回撤超过设定值则平仓。
- **权益止损**：可选，若持仓浮亏超过权益峰值的一定百分比，则关闭仓位。

## 参数
| 参数 | 说明 |
| ---- | ---- |
| `Candle Type` | 主交易时间框架。 |
| `Fast LWMA` / `Slow LWMA` | 快慢线性加权均线周期。 |
| `Momentum Length` | 更高周期动量的回溯长度。 |
| `Momentum Buy Threshold` / `Momentum Sell Threshold` | 动量距 100 水平的最小偏移。 |
| `Take Profit (points)` / `Stop Loss (points)` | 以点数表示的固定止盈/止损。 |
| `Use Trailing`、`Trailing Activation`、`Trailing Offset` | 移动止损配置。 |
| `Use Break-even`、`Break-even Trigger`、`Break-even Offset` | 保本参数。 |
| `Max Trades` | 单次运行允许的最大入场次数。 |
| `Order Volume` | 基础下单数量。 |
| `Use Money TP`、`Money Take Profit` | 货币止盈设置。 |
| `Use Percent TP`、`Percent Take Profit` | 百分比止盈设置。 |
| `Enable Money Trailing`、`Money Trailing Trigger`、`Money Trailing Stop` | 盈利回撤保护。 |
| `Use Equity Stop`、`Equity Risk %` | 权益回撤止损。 |

## 其他说明
- 策略始终保持单一净持仓，出现相反信号时平仓并可反向。
- `GetWorkingSecurities()` 会自动订阅动量与 MACD 所需的更高周期数据。
- 代码中的注释全部使用英文，符合仓库规范。
