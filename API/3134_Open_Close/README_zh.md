# Open Close

## 概述
Open Close 是 MetaTrader 5 智能交易系统 `Open Close.mq5`（编号 23090）的移植版本。策略对比最近两根已完成 K 线的开盘价与收盘价关系，一次只持有一笔仓位：当最新 K 线相对于上一根出现反转时开仓，当两根 K 线同向运行时平仓。C# 实现保留了原版的自适应下单量逻辑，可在连续亏损后自动收缩仓位。

## 策略逻辑
### K 线形态过滤
* 仅处理 `CandleType` 参数指定的、状态为完成的 K 线。
* 始终保存最近两根完结 K 线（记为 `previous` 与 `older`）。
* 逐根比较它们的开盘价与收盘价：
  * **多头反转**：`previous.Open > older.Open` 且 `previous.Close < older.Close`。
  * **空头反转**：`previous.Open < older.Open` 且 `previous.Close > older.Close`。

### 入场规则
* 当账户为空仓且出现多头反转形态时，下达市价买单。
* 当账户为空仓且出现空头反转形态时，下达市价卖单。
* 仅允许单一持仓，在当前仓位关闭之前不会响应相反信号。

### 出场规则
* 持有多头时，若两根监控 K 线同时下行（`previous.Open < older.Open` 且 `previous.Close < older.Close`）则立即市价平仓。
* 持有空头时，若两根 K 线同时上行（`previous.Open > older.Open` 且 `previous.Close > older.Close`）则市价回补。
* 原策略没有止损和止盈，移植版本同样仅依赖上述 K 线关系管理仓位。

### 仓位与连亏管理
* 订单手数主要由 `MaximumRiskPercent` 决定，即每笔交易投入的权益比例。基础公式：`Portfolio.CurrentValue × MaximumRiskPercent ÷ referencePrice`，其中参考价使用最近收盘价。
* 当无法获取账户估值或价格时，使用安全的备用手数 `FallbackVolume`。
* 每次平仓都会记录实际盈亏，并在最近 `HistoryDays` 天内统计连续亏损次数。
  * 当连续亏损超过一次时，下一笔订单的手数减少 `volume × losses ÷ DecreaseFactor`，以复现 MT5 的“减仓”规则。
* 最终手数会匹配交易品种的成交量步长，同时遵循最小/最大下单限制。

### 额外实现细节
* 仅当 `CandleStates.Finished` 时才进行计算，确保信号基于完整数据。
* 入场与出场判断在最新一根 K 线收盘时完成。原版在下一根 K 线开盘处下单，高级别周期差异可以忽略，但在超短周期下需注意滑点影响。
* StockSharp 的账户字段近似替代 MetaTrader 的保证金与余额统计。如合约乘数不同，可调节 `MaximumRiskPercent` 或 `FallbackVolume`。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MaximumRiskPercent` | `decimal` | `0.02` | 每笔交易投入的权益比例（0.02 代表 2%）。 |
| `DecreaseFactor` | `decimal` | `3` | 连续亏损时缩减手数所用的除数，数值越大缩减越缓和。 |
| `HistoryDays` | `int` | `60` | 统计连续亏损时回溯的自然日数。 |
| `FallbackVolume` | `decimal` | `0.1` | 无法按风险计算时采用的备用手数。 |
| `CandleType` | `DataType` | `TimeFrame(15m)` | 触发信号所使用的 K 线周期。 |

## 与 MetaTrader 版本的差异
* 保证金检查基于 `Portfolio.CurrentValue`，原始 EA 使用 `AccountFreeMargin`。仅在两端估值接近时才能得到一致的仓位结果。
* 连续亏损统计来源于策略自身的成交记录，而非终端的全局历史。请让策略运行足够长时间以积累统计样本。
* 同样保留“单仓”模型且不自动设置保护性订单，如需止损/止盈可在外部风险控制模块中补充。
