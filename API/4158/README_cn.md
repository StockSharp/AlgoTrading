# Casino111 策略

## 概览
Casino111 是源自同名 MetaTrader 4 智能交易系统的逆势突破策略。策略在每根新 K 线出现时，会把当前开盘价与上一日 K 线计算出的参考区间进行比较。如果开盘价越过了前一日的高点或低点（并加上可配置的缓冲），算法会立即在反方向开仓，同时放置对称的止损与止盈。移植到 StockSharp 后仍然保持原机器人“单笔持仓”的特性，并提供丰富的参数以便回测与优化。

## 入场与出场逻辑
1. 通过单独的日线订阅获取上一日的最高价与最低价，再利用 `UpperOffsetPoints` 与 `LowerOffsetPoints`（以 MetaTrader 点数表示）扩展参考通道。
2. 每当一根交易周期的 K 线收盘，策略会检查前一根和当前 K 线的开盘价：
   - 若当前开盘价跳空至“上一日高点 + 上轨缓冲”之上，则做空（反向博弈缺口）。
   - 若当前开盘价跌破“上一日低点 − 下轨缓冲”，则做多。
3. 同一时间只允许持有一笔仓位；必须等待已有订单成交后才会响应新的信号。
4. `StartProtection` 会在入场后立即设置固定的止损与止盈，距离均为 `BetPoints`（会自动换算成价格步长）。

## 资金管理
- 当 `UseMoneyManagement = false` 时，仓位大小固定为 `BaseVolume`。
- 当 `UseMoneyManagement = true` 时，启用与 MT4 版本一致的马丁格尔加仓：
  - 每次亏损或保本的交易之后，下一笔订单的数量会乘以 `(BetPoints * 2) / (BetPoints - spreadPoints)`。
  - 点差通过订单簿订阅获取的最新买一/卖一报价估算；若暂时没有报价，则乘数默认为 `2`。
  - 获利交易会把仓位恢复为 `BaseVolume`。所有数量都会对齐到合约的 `VolumeStep`，并受到 `MaxVolume` 限制。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | 允许在价格跌破通道下沿时开多。 |
| `EnableSell` | `bool` | `true` | 允许在价格突破通道上沿时开空。 |
| `BetPoints` | `decimal` | `400` | 对称的止损与止盈距离（MetaTrader 点数，内部转换为价格步长）。 |
| `UpperOffsetPoints` | `decimal` | `97` | 加在上一日高点之上的缓冲区，用于识别看空缺口。 |
| `LowerOffsetPoints` | `decimal` | `77` | 减在上一日低点之下的缓冲区，用于识别看多缺口。 |
| `UseMoneyManagement` | `bool` | `false` | 是否启用马丁格尔式的仓位管理。 |
| `MaxVolume` | `decimal` | `4` | 启用资金管理时允许的最大下单量。 |
| `BaseVolume` | `decimal` | `0.1` | 获利后或关闭资金管理时使用的基础下单量。 |
| `CandleType` | `DataType` | `H1` | 用于检测缺口的交易周期（默认 1 小时）。 |
| `DailyCandleType` | `DataType` | `D1` | 提供上一日高/低价的日线数据类型（默认 1 天）。 |

## 实现细节
- 策略完全基于 StockSharp 的高阶 API：`SubscribeCandles` 同时提供交易周期与日线数据，`SubscribeOrderBook` 则记录最新点差供资金管理使用。
- `StartProtection` 负责止损与止盈的下单流程，因此每次入场都会立刻得到与 MT4 版本一致的对称保护。
- 代码中的英文注释标记了主要决策点，方便阅读与维护。
- 计算过程只依赖当前 K 线的开盘价，完全复刻了 MT4 中 `Time[0]` / `Open[0]` 的逻辑，无需访问历史指标数据。

## 使用建议
- 可以根据研究需求调整交易周期；默认的一小时 K 线贴近原版设置，也可替换为任意 StockSharp 支持的 `DataType`。
- 启用资金管理时请确认 `MaxVolume` 符合经纪商限制；辅助函数会自动对齐至 `VolumeStep`、`MinVolume` 与 `MaxVolume`。
- 策略始终最多只有一笔持仓，适合配合 StockSharp 图表观察进出场标记，便于人工复核。
- 建议先在回放或模拟环境中测试，因为反向交易缺口需要稳定的点差和快速执行。 
