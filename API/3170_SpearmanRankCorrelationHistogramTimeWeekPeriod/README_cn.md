# Spearman Rank Correlation Histogram 周期交易窗口策略

## 概览
本策略在 StockSharp 高级 API 上复现 MetaTrader 专家顾问 **Exp_SpearmanRankCorrelation_Histogram_TimeWeekPeriod**。策略订阅单一周期的 K 线（默认 4 小时）并重算原始指标中的 Spearman 排名相关系数直方图。直方图的颜色代表短期趋势的多空倾向（大于零为多头，小于零为空头），同时借助一个可配置的周内时间窗口还原 MQL 中的 `TimeTrade` 逻辑。

## 交易规则
1. **指标计算**
   - 每根收盘 K 线都会记录收盘价并使用 `RangeLength` 个收盘价计算 Spearman 排名相关系数。
   - 颜色映射与指标完全一致：相关值大于 `HighLevel` 标记为 `4`，介于 `0` 与 `HighLevel` 之间为 `3`，介于 `LowLevel` 与 `0` 之间为 `1`，小于 `LowLevel` 为 `0`，恰好为零时记为 `2`。
   - 信号来自编号为 `SignalBar` 的已完成 K 线（默认上一根），并结合更早一根的颜色判断趋势切换。

2. **交易模式 (`TradeMode`)**
   - **Mode1**：颜色上穿 `2`（且前值 < `3`）开多；颜色下穿 `2`（且前值 > `1`）开空。多头颜色同时触发空单平仓，空头颜色触发多单平仓。
   - **Mode2**：颜色等于 `4`（且前值 < `4`）开多；颜色等于 `0`（且前值 > `0`）开空。颜色 > `2` 平空，颜色 < `2` 平多。
   - **Mode3**：颜色 `4` 同时平空并开多；颜色 `0` 同时平多并开空。
   - 成功建仓后会设置与 K 线长度相同的冷却时间，下一笔同向交易要等到下一根 K 线在 MetaTrader 中闭合。

3. **资金管理与下单量**
   - `MoneyManagement` 配合 `MarginMode` 将账户资金或风险比例转换为下单量。参数为正时复现原脚本的资金管理；为零时回退到策略的 `Volume`；为负值时视为固定手数。
   - 风险模式（`LossFreeMargin`, `LossBalance`）必须提供正的 `StopLossPoints`。当止损为零时，逻辑回退到 `Volume`，与原 EA 拒绝下单的行为一致。

4. **风控管理**
   - `StopLossPoints` 与 `TakeProfitPoints` 通过 `Security.PriceStep` 转换成价格。每根收盘 K 线都会使用最高价/最低价检查是否触发止损或止盈，触发后立即平掉全部仓位。
   - `DeviationPoints` 仅用于界面显示，StockSharp 的市价单不会使用该值。

5. **周内交易窗口**
   - `TimeTrade` 为真时，系统时间必须位于 (`StartDay`, `StartHour`, `StartMinute`, `StartSecond`) 与 (`EndDay`, `EndHour`, `EndMinute`, `EndSecond`) 之间；否则会立即强制平仓，完全对应原策略的保护逻辑。
   - 默认假设 `StartDay` 不晚于 `EndDay`。若需要跨周的时间段，请手动调整参数。

6. **其他说明**
   - 至少需要 `RangeLength + SignalBar + 1` 根完成的 K 线，策略才能生成交易信号。
   - `Direction` 为原指标保留的参数，在本移植中不参与计算。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `MoneyManagement` | 资金管理系数或固定手数。 | `0.1` |
| `MarginMode` | 资金管理模式（`FreeMargin`, `Balance`, `LossFreeMargin`, `LossBalance`, `Lot`）。 | `Lot` |
| `StopLossPoints` | 止损距离（价格点）。 | `1000` |
| `TakeProfitPoints` | 止盈距离（价格点）。 | `2000` |
| `DeviationPoints` | 允许的滑点（仅展示）。 | `10` |
| `BuyOpen` / `SellOpen` | 是否允许开多/开空。 | `true` |
| `BuyClose` / `SellClose` | 是否允许按信号平多/平空。 | `true` |
| `TradeMode` | 颜色信号模式（`Mode1`, `Mode2`, `Mode3`）。 | `Mode1` |
| `TimeTrade` | 是否启用周内交易窗口。 | `true` |
| `StartDay`, `StartHour`, `StartMinute`, `StartSecond` | 交易窗口开始的星期与时间。 | `星期二`, `8`, `0`, `0` |
| `EndDay`, `EndHour`, `EndMinute`, `EndSecond` | 交易窗口结束的星期与时间。 | `星期五`, `20`, `59`, `40` |
| `CandleType` | 处理的 K 线周期。 | `H4` |
| `RangeLength` | 参与 Spearman 计算的收盘价数量。 | `14` |
| `MaxRange` | `RangeLength` 的上限，用于安全限制。 | `30` |
| `Direction` | 指标保留参数，对策略无影响。 | `true` |
| `HighLevel`, `LowLevel` | 直方图上/下阈值。 | `0.5`, `-0.5` |
| `SignalBar` | 读取信号的已收盘 K 线编号。 | `1` |

其余设置（组合、标的、基础 `Volume`、风险参数等）按照标准的 StockSharp 工作流程配置即可。
