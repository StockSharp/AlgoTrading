# Prop Firm Helper 策略

## 概述
Prop Firm Helper 策略源自 MetaTrader 的 "Prop Firm Helper" 智能交易系统，使用唐奇安通道突破逻辑。策略在价格突破近期高点时挂买入止损单，在跌破近期低点时挂卖出止损单，并在达到挑战目标或触及每日亏损限制后自动停止交易。

## 交易逻辑
- 根据 `Candle Type` 订阅指定周期的 K 线。
- 计算两个唐奇安通道：
  - `Entry Period`/`Entry Shift` 用于识别突破水平。
  - `Exit Period`/`Exit Shift` 用于跟踪持仓并移动止损。
- 当账户为空仓或持有空头时，在移位后的上轨上方一格挂买入止损单。
- 当账户为空仓或持有多头时，在移位后的下轨下方一格挂卖出止损单。
- 使用平均真实波幅 (`ATR Period`) 平滑 trailing 止损的移动频率。
- 如果蜡烛收盘价低于跟踪的下轨则平多仓；若收盘价高于跟踪的上轨则平空仓。

## 风险管理
- `Risk Per Trade %` 按当前资产净值、最小价格变动和每档价格计算下单数量，并按照交易所的最小手数与增量进行取整，限制在最小/最大允许数量内。
- 防护性止损使用跟踪通道并叠加 ATR 缓冲，避免频繁修改订单。

## Prop Firm 挑战规则
- 勾选 `Use Challenge Rules` 后启用挑战检查。
- 当权益达到 `Pass Criteria` 时停止交易，取消所有挂单并平掉持仓。
- 当当日亏损超过 `Daily Loss Limit` 时立即清空持仓，取消挂单，并在该交易日剩余时间内禁止新订单。每日开始时重新记录基准权益。

## 参数说明
| 名称 | 描述 |
| --- | --- |
| `Entry Period` | 突破唐奇安通道的回溯周期。 |
| `Entry Shift` | 计算突破时忽略的已完成 K 线数量。 |
| `Exit Period` | 跟踪唐奇安通道的回溯周期。 |
| `Exit Shift` | 计算 trailing 止损时忽略的已完成 K 线数量。 |
| `Risk Per Trade %` | 每次入场风险占账户权益的百分比。 |
| `ATR Period` | 平滑 trailing 止损的 ATR 周期。 |
| `Use Challenge Rules` | 是否启用 prop firm 挑战限制。 |
| `Pass Criteria` | 达到该权益后停止交易。 |
| `Daily Loss Limit` | 当日允许的最大亏损额。 |
| `Candle Type` | 用于计算的 K 线类型。 |

## 注意事项
- 策略需要可用的投资组合数据来计算头寸规模和挑战指标。
- 每根完成的 K 线都会重新计算挂单价格并取消旧订单。
- 默认参数复现了原始 MetaTrader 策略的行为。
