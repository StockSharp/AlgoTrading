# RRS 随机策略

## 概述

**RRS Randomness Strategy** 是 MetaTrader 4 上 “RRS Randomness in Nature EA” 的 StockSharp 移植版。  
策略会随机打开多头或空头市价单，设置止损与止盈，可选地启动跟踪止损，并在浮动亏损超过设定阈值时触发风险控制强制平仓。

由于 StockSharp 对同一品种采用净持仓模式，不能同时持有多头和空头仓位。因此 “DoubleSide” 模式会交替开仓，而不是像 MetaTrader 那样同时维持两笔对冲交易。

## 交易逻辑

1. 每根 K 线收盘后，策略会根据最新成交价或 Level1 报价计算当前市场价格。  
2. 如果存在持仓，则依次检查止损、止盈与跟踪止损条件，并根据浮动盈亏评估风险。  
3. 在空仓状态下，先检查点差与可用成交量再决定是否进场：
   - **DoubleSide** 模式按顺序交替开多或开空。  
   - **OneSide** 模式沿用原版规则：从 `[0,5]` 之间生成的随机整数，当结果为 `1` 或 `4` 时开多，为 `0` 或 `3` 时开空。
4. 下单手数在最小与最大值之间均匀随机，并会按照交易品种的最小变动手数进行对齐。

## 参数

| 组别 | 名称 | 说明 |
|------|------|------|
| General | `Mode` | 选择方向逻辑：交替进场 (`DoubleSide`) 或随机过滤进场 (`OneSide`)。 |
| Lot Settings | `MinVolume` / `MaxVolume` | 随机生成手数的范围。 |
| Protection | `TakeProfitPoints` | 止盈距离（价格步长）。 |
| Protection | `StopLossPoints` | 止损距离（价格步长）。 |
| Protection | `TrailingStartPoints` | 激活跟踪止损所需的盈利距离。 |
| Protection | `TrailingGapPoints` | 跟踪止损相对当前价格的距离。 |
| Filters | `MaxSpreadPoints` | 允许进场的最大点差（以价格步长计）。 |
| Filters | `SlippagePoints` | 预期滑点，仅作参考。 |
| Risk Management | `MoneyRiskMode` | 风险模式：固定金额或按账户价值百分比。 |
| Risk Management | `RiskValue` | 风险阈值（货币金额或百分比，取决于模式）。 |
| General | `TradeComment` | 附加在订单上的说明文字。 |
| General | `CandleType` | 用于驱动策略的 K 线类型。 |

## 备注

- 策略需要订阅 K 线、Level1 报价以及成交数据，请确保所选品种提供这些数据。  
- 跟踪止损逻辑与 MQL 版本一致：当盈利超过 `TrailingStartPoints + TrailingGapPoints` 个价格步长时启动，并始终保持 `TrailingGapPoints` 的距离。  
- 风险控制会比较浮动盈亏与设定阈值，一旦亏损超限立即平仓。

