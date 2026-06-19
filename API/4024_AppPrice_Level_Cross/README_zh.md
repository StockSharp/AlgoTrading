# App Price Level Cross 策略

## 概述
- 将 MetaTrader 4 专家顾问 **BT_v4**（位于 `MQL/8543/BT_v4.mq4`）转换到 StockSharp 平台。
- 全面采用 StockSharp 的高层策略 API：烛线订阅、无指标的数据绑定以及内置的风控工具。
- 核心思想：监控收盘价是否突破用户设定的水平价位 `AppPrice`，并按原脚本的逻辑开仓和平仓。

## 交易逻辑
1. 仅处理已经完成的蜡烛（`CandleStates.Finished`），与 MQL 版本一致，不会读取正在形成的 `Close[0]`。
2. 如果当前收盘价 **上穿** `AppPrice`，同时上一根收盘价仍在该水平之下或相等：
   - 只有在 `BuyOnly = true` 时才会执行（对应原版默认的“只做多”模式）。
   - 撤销所有挂单，若当前持有空单则通过同一笔市价单一并平掉，再建立新的多头仓位。
3. 如果当前收盘价 **下穿** `AppPrice`，同时上一根收盘价仍在该水平之上或相等：
   - 只有在 `BuyOnly = false` 时才会执行（对应原版的“只做空”模式）。
   - 撤销挂单，若当前持有多单则同时反向平仓，再建立新的空头仓位。
4. 由于直接在 `Bind` 回调中处理数据，策略无需附加指标，也不会调用任何历史值遍历函数。

## 仓位管理
- 当 `EnableMoneyManagement = false` 时，始终下单 `FixedVolume` 手数（等价于 MQL 中的 `Lots`）。
- 当 `EnableMoneyManagement = true` 时，按原脚本公式计算手数：
  
  \[
  \text{lot} = \text{round}_{\text{LotPrecision}} \left( \frac{\text{LotBalancePercent}}{100} \times \frac{\text{Balance}}{\text{divisor}} \right)
  \]
  
  - 当 `LotPrecision = 1` 时 `divisor = 1000`，当 `LotPrecision = 2` 时 `divisor = 100`，完全复刻 `LotPrec` 的处理方式。
  - 计算结果会被约束在 [`MinLot`, `MaxLot`] 区间内，然后再次按照品种的 `VolumeStep`、`VolumeMin`、`VolumeMax` 对齐。
  - 若投资组合暂未返回余额数据，则自动回退到固定手数。

## 风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 以“价格点”（最小报价跳动）表示。
- 当任意一个值大于 0 时，策略会调用 `StartProtection`，并使用 `Security.PriceStep` 转换为真实价差。
- 将距离设为 `0` 即可关闭对应的保护（与 MQL 中的行为一致）。

## 主要参数
| 参数 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `AppPrice` | 触发交易的价格水平。 | `0` |
| `BuyOnly` | `true`=只做多，`false`=只做空。 | `true` |
| `FixedVolume` | MM 关闭时的固定手数。 | `0.1` |
| `EnableMoneyManagement` | 是否启用余额百分比算法。 | `false` |
| `LotBalancePercent` | MM 模式下使用的余额百分比。 | `10` |
| `MinLot` / `MaxLot` | 计算结果的上下限。 | `0.1` / `5` |
| `LotPrecision` | 手数的小数位数。 | `1` |
| `StopLossPoints` | 止损距离（点，0=关闭）。 | `140` |
| `TakeProfitPoints` | 止盈距离（点，0=关闭）。 | `180` |
| `CandleType` | 用于判定的蜡烛周期。 | `1 分钟` |

## 实现细节
- 通过 `SubscribeCandles(...).Bind(...)` 直接在回调中获取收盘价，无需额外指标，也避免了 `GetValue()` 之类的禁用方法。
- 市价单数量会加上当前持仓的绝对值，从而在一笔订单内完成反向平仓与开仓，效果与原 EA “先平仓再开仓”一致。
- 在每次下单前调用 `CancelActiveOrders()`，防止旧有挂单被意外触发。
- MQL 输入参数中与颜色、滑点或 `Magic` 相关的设置在 StockSharp 中没有必要，因此未保留。
- 请确保证券元数据（`PriceStep`, `VolumeStep`, `VolumeMin`, `VolumeMax`）已正确填写，以便止损/止盈和手数对齐与券商规则一致。

## 使用建议
- 将 `AppPrice` 设置为希望监控的关键价位（如枢轴位、重要整数位等）。
- 若想运行原脚本的“只做空”模式，请将 `BuyOnly` 设为 `false`；保持默认则执行“只做多”。
- 启用资金管理时，请确认投资组合连接能返回余额；否则策略会自动使用固定手数。
- 根据要求，本次仅提供 C# 版本，未创建 Python 目录及脚本。
