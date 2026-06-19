# Get Rich or Die Trying GBP 策略

## 概述
**Get Rich or Die Trying GBP 策略** 是将 MetaTrader 4 专家顾问 "Get Rich or Die Trying GBP" 移植到 StockSharp 高级 API 的结果。策略通过持续统计最近一段时间的分钟 K 线涨跌次数，并在每天两个设定的时间窗口内寻找反向反弹机会，以捕捉伦敦与纽约交易时段重叠时常见的短期修复波动。

## 交易逻辑
1. 默认订阅 1 分钟 K 线（可通过参数调整 K 线类型）。
2. 维护最近 *Lookback* 根已收盘 K 线的滚动窗口：
   - 收盘价低于开盘价的阴线记为 `+1`。
   - 收盘价高于开盘价的阳线记为 `-1`。
   - 十字线或持平的 K 线记为 `0`。
3. 计算上述数值的累计和，用于判断方向：
   - 累计值大于 0 代表近期以阴线为主，策略准备做多。
   - 累计值小于 0 代表近期以阳线为主，策略准备做空。
4. 只有在服务器时间落在两个目标小时之一，并且仍处于该小时前 *EntryWindowMinutes* 分钟时，才允许开仓：
   - `FirstEntryHour + HourShift`（默认对应 GMT+2 校正后的伦敦午夜）。
   - `SecondEntryHour + HourShift`（默认对应服务器时间 21:00，纽约收盘阶段）。
5. 当没有持仓且所有条件满足时，策略根据固定手数或启用的资金管理模块计算的手数市价开仓。
6. 持仓期间同时监控以下退出规则：
   - **部分止盈**：价格按照 *PartialTakeProfitPoints* 个最小变动单位运行后立即平仓。
   - **硬性止损**：价格朝不利方向波动 *StopLossPoints* 个最小变动单位时平仓。
   - **跟踪止损**：价格沿有利方向超过 *TrailingStopPoints* 个最小变动单位后，利用持仓以来的最高价（多头）或最低价（空头）动态收紧止损。
7. 额外监控 *TakeProfitPoints* 对应的最终止盈距离，以提供安全兜底。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TakeProfitPoints` | 100 | 最终止盈距离（以最小变动单位计）。 |
| `PartialTakeProfitPoints` | 40 | 主要止盈距离，对应原 EA 的快速平仓逻辑。 |
| `StopLossPoints` | 100 | 止损距离（以最小变动单位计）。 |
| `TrailingStopPoints` | 30 | 跟踪止损距离（以最小变动单位计）。 |
| `FixedVolume` | 1 | 未启用资金管理时的基础下单手数。 |
| `UseMoneyManagement` | false | 启用后根据账户权益和风险百分比动态计算手数。 |
| `RiskPercent` | 10 | 启用资金管理时，每笔交易希望承担的账户资金百分比。 |
| `Lookback` | 18 | 用于统计多空蜡烛数量的已完成 K 线数量。 |
| `FirstEntryHour` | 22 | 第一个交易小时（校正前）。 |
| `SecondEntryHour` | 19 | 第二个交易小时（校正前）。 |
| `HourShift` | 2 | 应用于两个交易小时的时间偏移。 |
| `EntryWindowMinutes` | 5 | 每个合格小时内允许开仓的时间窗口宽度（分钟）。 |
| `CandleType` | 1 分钟 | 订阅的 K 线类型，可替换为其他周期。 |

## 资金管理
当 `UseMoneyManagement` 为 `true` 时，策略按照 `RiskPercent` 与 `StopLossPoints` 计算风险资金，结合合约的 `StepPrice`、`LotStep` 与 `MinVolume` 推导出合规的下单手数。

## 使用提示
- 交易时间判断基于服务器/交易所时间，请根据实际时区调整 `HourShift`，确保修正后的目标小时匹配想要的交易时段。
- 建议保持 `Lookback` 大于 1，以减少噪声。增大该值可使方向判断更平滑，但也会降低反应速度。
- 跟踪止损与止盈逻辑均基于已收盘的 K 线。如需更精细的触发，请缩短 K 线周期。
- 原 EA 支持多笔持仓；此移植版本为了易于控制风险，仅允许同时持有一笔仓位。

## 限制
- 跟踪止损为虚拟执行，只有在下一根完成的 K 线上满足条件时才会发送平仓指令。
- 资金管理假设 `Security.StepPrice` 能正确反映一个最小变动单位的货币价值，请在实盘前确认。

## 环境需求
- 已安装的 StockSharp 高级 API（AlgoTrading 解决方案）。
- 目标 GBP 品种的分钟级历史与实时数据。

## 参考
- 原始 MetaTrader 4 EA：`MQL/7690/Get_rich_or_die_trying_any_gbp.mq4`。
