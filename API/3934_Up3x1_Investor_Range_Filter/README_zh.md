# Up3x1 Investor 策略

## 概览
本策略为 MetaTrader 4 专家顾问 **up3x1_Investor** 的移植版本。策略在可配置的周期（默认 1 小时）上处理已收盘的 K 线，并使用 StockSharp 的高级 API 重现原始规则，同时提供明确的风险控制参数。

## 交易逻辑
- 检查最近一根已完成的 K 线：
  - K 线振幅（最高价 − 最低价）大于 `0.0060`；
  - K 线实体（|开盘价 − 收盘价|）大于 `0.0050`。
- 如果满足条件且 K 线收阳，则开多单；
- 如果满足条件且 K 线收阴，则开空单；
- 每逢周一完全停止交易，等同于原始 MQL 代码中的 `DayOfWeek()==1` 判断。

## 仓位管理
- 入场时根据参数计算内部目标：
  - `TakeProfitPoints`：获利目标距离；
  - `StopLossPoints`：止损距离；
  - `TrailingStopPoints`：用于移动止损的距离。
- 在每根收盘 K 线上执行以下检查：
  - 价格触及目标价则平仓；
  - 价格触及止损价则平仓；
  - 当价格向有利方向移动超过追踪距离时，将止损向价格方向收紧。
- 另外，当 24 周期与 60 周期简单移动平均值在同一根 K 线上相等（允许一个最小跳动的误差）时，立即平仓，复刻原始 EA 中的关闭条件。

## 资金管理
- `BaseVolume`：无法从资金计算时的基础手数。
- `MaximumRisk`：复制 `AccountFreeMargin()*MaximumRisk/1000` 公式，若可获取组合价值，则手数 = `价值 * MaximumRisk / 1000`，并保留一位小数。
- `DecreaseFactor`：连续亏损超过一次时，按 `亏损次数 / DecreaseFactor` 比例降低手数。
- `MinimumVolume`：手数下限，保持与原始脚本一致的 0.1 手。

## 参数说明
| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `BaseVolume` | `0.1` | 基础持仓手数。 |
| `MaximumRisk` | `0.2` | 根据账户资金调整手数的风险系数。 |
| `DecreaseFactor` | `3` | 连续亏损后的手数削减因子。 |
| `MinimumVolume` | `0.1` | 最小手数限制。 |
| `TakeProfitPoints` | `20` | 止盈距离（以最小跳动为单位）。 |
| `StopLossPoints` | `50` | 止损距离（以最小跳动为单位）。 |
| `TrailingStopPoints` | `10` | 追踪止损距离。 |
| `SkipMondays` | `true` | 是否跳过周一交易。 |
| `CandleType` | `1 小时` | 订阅的 K 线周期。 |

## 其他说明
- 策略同一时间只持有一个方向的仓位，对应原始 EA 的 `CalculateCurrentOrders` 逻辑。
- 连续亏损的统计在策略内部完成，因为 StockSharp 不提供 MetaTrader 的历史订单数据。
- 所有交易均以市价单执行，调用 `BuyMarket` 或 `SellMarket`。
