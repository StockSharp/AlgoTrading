# MACD Sample 1010 策略

## 概述
本模块将 MetaTrader 专家顾问 **macd_sample_1010.mq4** 迁移到 StockSharp 的高级 API。原始脚本在布林带的基础上叠加了简单的资金管理：当收盘价突破上轨并超过指定缓冲时开空单，跌破下轨并超过缓冲时开多单；随后在达到固定的盈利或亏损（以点数表示）时平仓。移植后的 StockSharp 策略订阅所选 K 线序列，绑定 `BollingerBands` 指标，并在回调中发送市价单与管理仓位，以完整复现上述流程。

该移植版本同样只在收盘后做出判断，确保突破与出场的判定与 MetaTrader 中基于收盘价的逻辑一致。同时保留了原脚本的 `LotIncrease` 动态加仓选项，可根据账户权益变化自动调整交易手数。

## 移植要点
- 采用 `SubscribeCandles` + `Bind` 的高级模式为 `BollingerBands` 指标提供数据，无需手动缓存或循环计算。
- 使用 `StrategyParam<T>` 声明所有输入参数，使其在界面中可视并可用于优化。
- 通过读取 `Security.PriceStep` 将点值换算为价格增量，从而在 `BuyMarket` / `SellMarket` 调用前加上与 MetaTrader 相同的缓冲距离。
- 利用 `Portfolio.CurrentValue`（若不可用则回退至 `BeginValue`）实现与 MQL4 版本一致的动态手数计算，且将结果限制在 500 手以内。
- 仅处理已完成的 K 线，避免原脚本通过计数器抑制的逐笔抖动。
- 在关键逻辑处加入英文注释，帮助理解各个处理步骤。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `ProfitTargetPips` | `decimal` | `3` | 单笔多空在获利 `ProfitTargetPips` 个点后平仓，设置为 `0` 则关闭止盈。 |
| `LossLimitPips` | `decimal` | `20` | 单笔多空在亏损 `LossLimitPips` 个点后平仓，设置为 `0` 则关闭止损。 |
| `BandDistancePips` | `decimal` | `3` | 在上轨之上/下轨之下额外增加的点数缓冲，用于确认突破。 |
| `BollingerPeriod` | `int` | `4` | 计算布林带的周期长度。 |
| `BollingerDeviation` | `decimal` | `2` | 布林带使用的标准差倍数。 |
| `BaseVolume` | `decimal` | `1` | 初始下单手数，同时也是动态手数算法的基准。 |
| `LotIncrease` | `bool` | `true` | 启用后，按照当前账户权益与初始权益的比例调整交易手数。 |
| `OneOrderOnly` | `bool` | `true` | 为 `true` 时只有在无持仓时才会再次开仓；即便关闭，由于 StockSharp 使用净头寸，也不会出现对冲持仓。 |
| `CandleType` | `DataType` | `TimeFrame(15m)` | 指标计算与信号判断使用的 K 线类型。 |

## 交易逻辑
1. `OnStarted` 中根据参数创建布林带指标，订阅 `CandleType` 所指的 K 线，并绑定 `ProcessCandle` 回调。
2. 每根完成的 K 线触发 `ProcessCandle`，若启用了 `LotIncrease` 会先更新当前下单手数。
3. 若收盘价高于上轨加上 `BandDistancePips`（换算成价格），策略发送市价卖单；若收盘价低于下轨减去缓冲，则发送市价买单。`OneOrderOnly` 为真时，仅在净头寸为零时允许新开仓。
4. 执行完可能的入场后检查当前头寸：
   - 多单在盈利达到 `ProfitTargetPips` 点或亏损达到 `LossLimitPips` 点时平仓。
   - 空单在盈利达到 `ProfitTargetPips` 点或亏损达到 `LossLimitPips` 点时平仓。
5. 所有点数比较均通过 `Security.PriceStep` 转换为价格增量，确保与 MQL4 版本的 pip 计算保持一致。

## 手数控制
- 关闭 `LotIncrease` 时始终按 `BaseVolume` 下单。
- 启用 `LotIncrease` 时，策略在首次计算时记录“每手对应的初始权益”，之后每根 K 线依据当前权益与该基准的比值重新计算手数，四舍五入到一位小数（等同于 MQL4 的 `NormalizeDouble(..., 1)`），并限制在 500 手以内。
- 当账户权益信息不可用时，会回退到固定手数模式。

## 使用建议
1. 在所选品种上启动策略前，确认 `Security.PriceStep` 与实际交易的最小价格变动相符。
2. 设置合适的 `CandleType` 时间框架。原脚本常用于 5–15 分钟级别，但也可使用其他周期。
3. 根据需求调整布林带参数、缓冲点数以及止盈止损阈值。
4. 决定是否启用余额驱动的手数放大功能 (`LotIncrease`)。
5. 启动策略并观察日志，确认入场与出场发生在预期的收盘价附近。

## 与 MetaTrader 版本的差异
- StockSharp 只维护净头寸，因此即便关闭 `OneOrderOnly` 也不会出现多空同时持仓的情形，而是通过增减净头寸实现加仓或反手。
- 止盈止损通过每根收盘 K 线检查来实现，而非在服务器上挂出预设的限价单，但行为与原脚本等效。
- 原脚本的日志与错误开关（`logging`、`logerrs`、`logtick`）在移植过程中省略，借助 StockSharp 自带的日志系统即可跟踪所有订单与成交。
- MetaTrader 版本在本地文件中记录统计数据，本移植依托 StockSharp 的组合与策略统计功能，不再生成额外文件。
