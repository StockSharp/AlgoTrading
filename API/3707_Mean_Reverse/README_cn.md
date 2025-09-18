# Mean Reverse 策略

## 概述
Mean Reverse 策略复刻了 "MeanReversionTrendEA" 智能交易系统，通过简单均线交叉的趋势模块与基于 ATR 波动区间的均值回归模块相结合来产生交易信号。当价格确认趋势反转或相对慢速均线出现过度偏离时，系统会开仓。

## 交易逻辑
- **趋势模块**：当快速简单移动平均线 (SMA) 上穿慢速 SMA 时产生买入信号；当快速 SMA 下穿慢速 SMA 时产生卖出信号。
- **均值回归模块**：当收盘价低于慢速 SMA 且偏离幅度超过 `ATR × Multiplier` 时触发买入；当收盘价高于慢速 SMA 且偏离幅度超过该阈值时触发卖出。
- **信号合成**：只要任一模块给出信号且当前没有持仓，就会按照设定的交易量开多或开空。

## 仓位管理
- **止损**：进场后立即根据 `入场价 − StopLossPoints × Step`（多头）或 `入场价 + StopLossPoints × Step`（空头）计算止损价位，一旦蜡烛的最高价或最低价触及该水平即平仓。
- **止盈**：同时设置 `入场价 + TakeProfitPoints × Step`（多头）或 `入场价 − TakeProfitPoints × Step`（空头）的目标价位，价格触及即止盈离场。
- **单仓约束**：策略同一时间只保持一笔持仓，持仓未平之前忽略新的入场信号。
- **安全模块**：`StartProtection()` 调用复现了原始 EA 的安全检查逻辑，用于防止异常的持仓状态。

## 指标
- `FastMaPeriod` 周期的简单移动平均线。
- `SlowMaPeriod` 周期的简单移动平均线。
- `AtrPeriod` 周期的平均真实波幅 (ATR)。

所有指标都使用 `CandleType` 指定的同一份 K 线订阅数据。

## 参数
| 名称 | 说明 | 默认值 |
|------|------|--------|
| `FastMaPeriod` | 快速 SMA 的回溯长度，同时用于均值回归带。 | 20 |
| `SlowMaPeriod` | 慢速 SMA 的回溯长度，代表均值。 | 50 |
| `AtrPeriod` | ATR 波动度计算所需的蜡烛数量。 | 14 |
| `AtrMultiplier` | ATR 偏离宽度的乘数。 | 2.0 |
| `StopLossPoints` | 以品种最小步长 `Security.Step` 表示的止损距离。 | 500 |
| `TakeProfitPoints` | 以 `Security.Step` 表示的止盈距离。 | 1000 |
| `TradeVolume` | 每次下单使用的交易量。 | 1 |
| `CandleType` | 为指标提供数据的 K 线类型。 | 1 小时周期 |

## 备注
- 默认的 1 小时 K 线对应 MetaTrader 中的“当前周期”概念，可根据需要调整。
- ATR 偏离带以收盘价为基准，与原策略取 Bid/Ask 均值的做法一致。
- 参数均已启用优化标记，方便在不同市场上进行回测和调参。
