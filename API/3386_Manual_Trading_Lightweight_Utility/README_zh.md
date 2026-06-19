# Manual Trading Lightweight Utility 策略

## 概述
原版 MetaTrader "Manual Trading Lightweight Utility" 专家顾问是一套轻量级的手动交易面板，可以快速在市价、限价、止损订单之间切换，分别调整买卖手数，并为每笔交易自动设置止损与止盈。本 C# 版本将该面板完全移植到 StockSharp 中——所有按钮都变成了策略参数。策略本身不会生成任何信号，只会等待人工指令，通过高层 API 执行下单，同时监控防护性离场。

## 复刻的能力
- **单次买/卖请求。** `BuyRequest` 和 `SellRequest` 两个布尔参数模拟面板按钮。将其设为 `true` 会根据选定模式提交一张市价、限价或止损订单，并在执行后自动恢复为 `false`。
- **自动或手动挂单价格。** 每个方向都可以选择沿用 MetaTrader 的点值偏移（`LimitOrderPoints` 和 `StopOrderPoints`），或直接输入绝对价格。自动模式优先使用当前最优买卖价，若缺少报价则回退到最近一根收盘价。
- **独立手数。** 可以共享一个默认手数，也可以启用 `UseIndividualVolumes` 为买单和卖单分别设定手数，对应原脚本中的 Lot Control 开关。
- **基于点值的风控。** `TakeProfitPoints` 与 `StopLossPoints` 按交易所的 `PriceStep` 转换成价格偏移。策略监控已完成的蜡烛，一旦穿越防护水平，就用市价单平仓。
- **带注释的日志。** 每次手动动作都会在日志中记录 `OrderComment`、订单类型、价格和手数，替代原始面板的视觉反馈。

## 工作流程
1. 策略订阅由 `CandleType` 指定的蜡烛序列。收盘价用于计算偏移和跟踪风险。
2. 每当产生一根完整蜡烛时，策略会：
   - 将基类 `Volume` 更新为 `DefaultVolume`，便于在界面中观察。
   - 检查 `BuyRequest` 和 `SellRequest` 的变化，并标记待执行的命令。
   - 在 `IsFormedAndOnlineAndAllowTrading()` 返回 true 后，根据所选模式执行命令、计算挂单价格，并写入日志。
   - 调用风控模块，在仓位变化时更新入场价，并在蜡烛突破止损或止盈时通过市价单平仓。
3. 当仓位回到零时，内部状态全部重置，下一次手动指令将从干净状态开始。

## 参数说明
- **`CandleType`** – 用于计算价格偏移和风控的蜡烛类型。
- **`BuyOrderMode` / `SellOrderMode`** – 为买/卖方向选择 `MarketExecution`、`PendingLimit` 或 `PendingStop`。
- **`UseAutomaticBuyPrice` / `UseAutomaticSellPrice`** – 打开自动定价模式。关闭后需要提供 `BuyManualPrice` / `SellManualPrice`。
- **`BuyManualPrice` / `SellManualPrice`** – 自动模式关闭时使用的挂单绝对价格（0 表示忽略）。
- **`DefaultVolume`** – 未启用独立手数时通用的下单手数。
- **`UseIndividualVolumes`** – 打开后使用 `BuyVolume` / `SellVolume` 覆盖默认手数。
- **`BuyVolume` / `SellVolume`** – 买入与卖出各自的手数。
- **`TakeProfitPoints` / `StopLossPoints`** – 止盈/止损点数，0 表示禁用。
- **`LimitOrderPoints` / `StopOrderPoints`** – 自动模式下限价与止损价格的点数偏移。
- **`BuyRequest` / `SellRequest`** – 模拟按钮的瞬时开关，执行后自动复位。
- **`OrderComment`** – 执行命令时写入日志的备注文字。

## 使用建议
1. 根据需要的精度选择 `CandleType`。默认的一分钟蜡烛与原脚本对行情的响应速度接近，同时适用于历史回测。
2. 确定手数管理方式。若使用共享手数，请保证 `DefaultVolume` 为正值；若需要分开控制，则启用 `UseIndividualVolumes` 并设置 `BuyVolume` 与 `SellVolume`。
3. 决定挂单价格的计算方式。自动模式会按点数乘以 `PriceStep` 得出偏移；手动模式则直接使用绝对价格。
4. 设置 `TakeProfitPoints` 和 `StopLossPoints`。当它们大于 0 时，策略会转换成价格距离；若品种未配置 `PriceStep`，系统会记录警告并跳过防护。
5. 需要下单时，将 `BuyRequest` 或 `SellRequest` 从 `false` 改为 `true`。策略会在下一根收盘蜡烛执行命令、记录日志，并将开关恢复为 `false`，防止重复触发。
6. 若要重新执行某个动作，只需再次切换对应参数。如果由于价格配置无效（如手动价格为 0）导致无法下单，请修正参数后重新触发。

## 与原版的差异
- 图形界面的按钮被 StockSharp 参数取代，可以通过属性网格或自动化脚本进行控制。
- 防护单以市价平仓实现，而不是另行挂出止损/止盈订单，避免在高层 API 中维护订单生命周期。
- 当缺少最优买卖报价时，自动价格会回退到最近一根收盘价，确保在没有深度行情的回测中仍能复现行为。

## 备注
- 每次仓位变化都会刷新入场价，若分批加仓，新的止损/止盈会以最新蜡烛的收盘价为基准。
- 止损距离会加上当前已知的点差；若无法获取点差，则使用一个价格步长作为补偿，保持与原脚本一致的保守性。
- 日志记录包含备注、订单类型、价格（挂单）和手数，为后续审计提供清晰的轨迹。
