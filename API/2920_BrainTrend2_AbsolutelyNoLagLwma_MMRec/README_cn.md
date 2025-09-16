# BrainTrend2 + AbsolutelyNoLagLWMA MMRec 策略

## 概述
该策略重现 MetaTrader 智能交易系统 `Exp_BrainTrend2_AbsolutelyNoLagLwma_MMRec`，将 BrainTrend2 趋势模块与 AbsolutelyNoLagLWMA 自适应滤波器结合在一起。两个模块都可以根据各自的权限独立开仓或平仓，从而模拟原始 MMRec 模板中的风险控制。下单通过 StockSharp 的高级 API 以市价单执行，并可设置默认交易数量。

## 交易逻辑
### BrainTrend2 模块
* 使用加权真实波幅构建动态跟踪带。
* 当 K 线穿透跟踪带超过 `0.7 * ATR` 时，趋势方向（river）发生切换。
* 上涨河道中的多头 K 线触发买入（若允许）并关闭空头仓位。
* 下跌河道中的空头 K 线触发卖出（若允许）并关闭多头仓位。
* `Brain Signal Shift` 参数可将信号向历史回溯若干根 K 线。

### AbsolutelyNoLagLWMA 模块
* 对选定价格源执行两次线性加权滑动平均。
* 当双层 LWMA 上升时颜色为 **2**，下降时为 **0**，保持不变时为 **1**。
* 颜色由其他值切换为 2 时开多并可选择平空；颜色由其他值切换为 0 时开空并可选择平多。
* `Abs Signal Shift` 参数用于延后信号评估。

### 仓位管理
* 策略只维护一个净头寸。当两个模块同时发出指令时，所有平仓动作先于新的开仓执行。
* 如果某个模块想要开仓，但当前持有相反仓位且禁止平仓，则该信号被忽略（与单净头寸账户无法对冲的情况一致）。

## 参数
| 分组 | 名称 | 说明 |
| --- | --- | --- |
| BrainTrend2 | Brain Candle | 计算 BrainTrend2 指标所用的蜡烛类型。 |
| BrainTrend2 | Brain ATR | BrainTrend2 内部使用的 ATR 周期。 |
| BrainTrend2 | Brain Signal Shift | BrainTrend2 信号回溯的 K 线数量。 |
| BrainTrend2 | Brain Buy / Sell | 允许 BrainTrend2 模块开多/开空。 |
| BrainTrend2 | Brain Close Buys / Close Sells | 允许 BrainTrend2 模块平多/平空。 |
| AbsolutelyNoLag | Abs Candle | AbsolutelyNoLagLWMA 使用的蜡烛类型。 |
| AbsolutelyNoLag | Abs Length | LWMA 周期。 |
| AbsolutelyNoLag | Abs Price | LWMA 采用的价格源，对应 MQL 中的 `Applied_price_` 枚举。 |
| AbsolutelyNoLag | Abs Signal Shift | LWMA 信号回溯的 K 线数量。 |
| AbsolutelyNoLag | Abs Buy / Sell | 允许 LWMA 模块开多/开空。 |
| AbsolutelyNoLag | Abs Close Buys / Close Sells | 允许 LWMA 模块平多/平空。 |
| AbsolutelyNoLag | Abs Shift | 给 LWMA 输出增加的常数偏移。 |
| General | Order Volume | 市价单的默认交易数量。 |

## 说明
* ATR 与 LWMA 的计算方法完全移植自 MQL 版本，包括三角加权和扩展的价格源列表。
* StockSharp 的蜡烛数据不包含点差，因此真实波幅仅基于最高价与最低价计算，相当于点差为零的情形。
* 原策略允许通过不同 magic number 同时持有多笔仓位，本实现按照 StockSharp 的常规做法合并为单一净头寸。
