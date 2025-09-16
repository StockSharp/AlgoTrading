# Exp TimeZone Pivots Open System Tm Plus 策略
[English](README.md) | [Русский](README_ru.md)

本策略是将 **Exp_TimeZonePivotsOpenSystem_Tm_Plus** MQL5 专家顾问完整移植到 StockSharp 高级 API 的版本。代码中重建了 *TimeZonePivotsOpenSystem* 指标：在每日会话开盘价附近绘制上下两个突破带，并在价格突破后回撤时寻找进场机会。原脚本中的信号延迟、持仓时间限制、方向性平仓和多种资金管理模式全部保留，并以参数形式暴露，方便与原版保持一致。

## 交易逻辑

1. 当新的时间段到达参数 `StartHour` 指定的小时后，记录会话开盘价，并在其上下 `OffsetPoints`（以点数表示）处生成动态通道。
2. 如果收盘价 **突破上轨**：
   - 在下一根 K 线（考虑 `SignalBar` 延迟）尝试开多，但仅当当前 K 线已经回到通道内。
   - 若启用 `SellPosClose`，立即平掉空头仓位。
3. 如果收盘价 **跌破下轨**：
   - 在下一根 K 线尝试开空，但仅当当前 K 线已经回到通道内。
   - 若启用 `BuyPosClose`，立即平掉多头仓位。
4. 通过 `TryExecutePendingEntries` 在新 K 线的首个更新中提交挂起订单，从而与原专家在新柱开始时入场的行为保持一致。

`SignalBar` 用于控制信号引用的历史柱数：`0` 代表最新一根收盘柱，`1`（默认值）表示再额外等待一根柱，以获得额外确认。

## 仓位管理

* **止损/止盈**：`StopLossPoints` 与 `TakeProfitPoints` 以点数表示，根据品种的 `PriceStep` 转换为价格距离，并利用蜡烛的最高/最低价进行监控，确保盘中触发也能及时离场。
* **持仓计时**：当 `TimeTrade` 为真时，持仓时间超过 `HoldingMinutes` 分钟后会被强制平仓，对应原始脚本中的 `nTime` 逻辑。
* **反向平仓**：若新的突破信号与当前仓位方向相反，并且对应的 `BuyPosClose` 或 `SellPosClose` 允许，则立即平仓。

## 资金管理

`MoneyMode` 参数对应原始枚举 `MarginMode`：

- `Lot`：固定手数，取值为 `MoneyManagement`。
- `Balance`、`FreeMargin`：按账户权益或可用保证金的比例下单（`MoneyManagement * Equity / Price`）。
- `LossBalance`、`LossFreeMargin`：按照止损距离来计算风险敞口，等价于 `MoneyManagement * Equity / StopDistance`。

若 `StopLossPoints` 设为 0，风险模式会自动退化为按价格比例下单，避免除以零。

## 参数一览

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `MoneyManagement` | 资金管理基数，根据 `MoneyMode` 计算下单量。 | `0.1` |
| `MoneyMode` | 资金管理方式（`Lot`、`Balance`、`FreeMargin`、`LossBalance`、`LossFreeMargin`）。 | `Lot` |
| `StopLossPoints` | 止损距离（点）。 | `1000` |
| `TakeProfitPoints` | 止盈距离（点）。 | `2000` |
| `DeviationPoints` | 来自原脚本的滑点设置，当前仅作展示用。 | `10` |
| `BuyPosOpen` / `SellPosOpen` | 是否允许开多 / 开空。 | `true` |
| `BuyPosClose` / `SellPosClose` | 是否允许由反向信号强制平仓。 | `true` |
| `TimeTrade` | 是否启用最大持仓时间限制。 | `true` |
| `HoldingMinutes` | 最大持仓时间（分钟）。 | `720` |
| `OffsetPoints` | 上下突破带距离会话开盘价的偏移（点）。 | `200` |
| `SignalBar` | 信号延迟的柱数（0 表示上一根已收盘的 K 线）。 | `1` |
| `CandleType` | 指标计算所用的主时间框架。 | `TimeSpan.FromHours(1).TimeFrame()` |
| `StartHour` | 会话开盘价所对应的小时（0–23）。 | `0` |

## 使用建议

- 需要品种提供有效的 `PriceStep`，否则系统会使用备用值 `0.0001`。
- 由于下单在新柱首个更新时触发，真实成交价将跟随市场，即使与理论开盘价存在滑点也与原专家一致。
- 建议测试时使用 H1 或更低时间框架，以匹配指标的设计假设。
- 调整 `SignalBar` 可以在敏捷度与稳健性之间取舍：`0` 更快，`1` 更稳健。

