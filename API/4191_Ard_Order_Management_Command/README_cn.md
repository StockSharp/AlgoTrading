# ARD Order Management Command 策略
[English](README.md) | [Русский](README_ru.md)

## 策略概述
**ARD Order Management** 策略将 MetaTrader 4 专家顾问 `ARD_ORDER_MANAGEMENT_.mq4` 迁移到 StockSharp 的高级框架。原始脚本提供了四个手动命令（买入、卖出、关闭、修改），每个命令都会根据账户可用保证金重新计算下单手数，开/反转市价单，并按照固定的点数距离设置止损和止盈。

在 StockSharp 版本中，交互方式保持一致：通过修改参数 `Command` 来驱动执行。一旦该参数不为 `None`，策略会在下一次 Level 1 行情更新时执行相应动作，并自动把参数恢复为 `None`。每次新开仓或执行修改命令时都会重新生成保护性订单，使得止损/止盈距离始终与当前参数保持一致。

## 命令执行流程
1. **接收命令** – 当 `Command` 为 `Buy` 或 `Sell` 时，策略会记录请求并立即调用 `ClosePosition()` 平掉当前持仓。同时取消所有尚未成交的保护订单，完全复刻原始 MQL 中遍历 `OrderClose` 的行为。
2. **计算手数** – 每次执行前都会重新计算下单手数。逻辑使用 `Portfolio.CurrentValue`（若无则使用 `Portfolio.BeginValue`），除以 `LotSizeDivisor` 并再乘以 `1/1000`，与 MetaTrader 中的 `AccountFreeMargin()/lotsize/1000` 完全一致。然后按照 `LotDecimals` 进行四舍五入，并确保结果不低于 `MinimumVolume`。
3. **等待持仓归零** – 如果命令到达时仍有持仓，新的开仓会等待到 `Position` 变为 0。策略在每个 Level 1 行情事件中检查该条件，以避免与异步执行管线发生竞争。
4. **市价下单** – 在仓位归零后提交 `BuyMarket` 或 `SellMarket`。策略保存最新的买价/卖价，为后续的止损止盈定价提供参考。
5. **下保护单** – 止损和止盈以单独的 stop/limit 订单形式提交。多头仓位的止损价为 `bid − StopLossPoints * PriceStep`，止盈价为 `ask + TakeProfitPoints * PriceStep`；空头仓位则反向处理。`Modify` 命令会使用 `ModifyStopLossPoints` 与 `ModifyTakeProfitPoints` 重建保护单。
6. **关闭命令** – 设置 `Command = Close` 时，策略会取消所有保护性订单并调用 `ClosePosition()`。如果已经空仓，则只记录一条日志，不会执行额外操作。

## 资金管理
- **保证金驱动的手数** – 手数随投资组合价值自动调整。若 `LotSizeDivisor` 被错误地设置为 0，策略会给出警告并退回到 `MinimumVolume`。
- **手数取整** – `LotDecimals` 控制小数位数，内部使用 `Math.Round`（`MidpointRounding.AwayFromZero` 模式），与 MetaTrader 的 `NormalizeDouble` 行为一致。
- **最小手数** – 经过取整后，手数会与 `MinimumVolume` 比较，确保不会低于该值，重现原 EA 中“低于 `lotmax` 时强制为 0.1 手”的逻辑。

## 止损与止盈
- 每次开仓或修改都会取消旧的保护订单并重新下单。
- 下单前会检查 `Security.PriceStep`。如果价格步长缺失或小于等于 0，将跳过保护单并写入警告日志。
- `Modify` 命令仅重新构建保护单，不改变当前仓位规模。

## 数据与执行
- 通过 `SubscribeLevel1()` 订阅 Level 1 行情，获取 `Bid`/`Ask` 信息，无需蜡烛或指标。
- 全程使用 StockSharp 的高级封装方法（`BuyMarket`、`SellMarket`、`BuyStop`、`SellStop`、`BuyLimit`、`SellLimit`、`CancelOrder`）。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `SlippageSteps` | int | 4 | 允许的滑点（按价格步数表示）。保持与原版一致，StockSharp 的市价单不会直接使用该值。 |
| `LotDecimals` | int | 1 | 手数保留的小数位数。 |
| `StopLossPoints` | decimal | 50 | 初始止损距离（价格点数）。 |
| `TakeProfitPoints` | decimal | 100 | 初始止盈距离（价格点数）。 |
| `LotSizeDivisor` | decimal | 5 | 将投资组合价值转换为手数时使用的除数（`freeMargin / divisor / 1000`）。 |
| `ModifyStopLossPoints` | decimal | 20 | 执行 `Modify` 命令时使用的止损距离。 |
| `ModifyTakeProfitPoints` | decimal | 100 | 执行 `Modify` 命令时使用的止盈距离。 |
| `MinimumVolume` | decimal | 0.1 | 取整之后的最低手数限制。 |
| `OrderComment` | string | `"Placing Order"` | 写入到每张订单的备注。 |
| `Command` | `ArdOrderCommand` | `None` | 要执行的命令，完成后会自动重置为 `None`。 |

## 使用提示
- 可通过界面或代码设置 `Command`。若要重复同一动作，需要先把它改回 `None` 再次设置。
- 由于止损/止盈以独立订单形式存在，经纪商必须支持对应的 stop/limit 订单类型；若不支持，需要自行实现合成退出逻辑。
- `SlippageSteps` 只保留为说明用途，StockSharp 的市价单不会读取该参数。
- 策略使用 `LogInfo`/`LogWarn` 记录关键事件，方便审计和排查。

## 与原版 EA 的差异
- MetaTrader 将止损/止盈直接附加在订单上；StockSharp 通过独立订单实现相同功能。
- StockSharp 采用事件驱动的异步模型，新开仓会等待旧仓位确认平掉，避免订单重叠。
- 资金来源改为投资组合对象的 `CurrentValue`/`BeginValue`，请确保适配器能提供这些信息。
- 错误处理依赖 StockSharp 的日志输出，而不是重复调用 `OrderSend` 的循环。

## 测试建议
- 在模拟环境中使用 Level 1 行情测试，确认保护订单价格与预期一致。
- 在实盘前根据经纪商的合约规格调整 `LotSizeDivisor` 和 `LotDecimals`。
