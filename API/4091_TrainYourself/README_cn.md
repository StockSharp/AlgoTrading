# TrainYourself 策略

该策略是 MetaTrader 4 智能交易系统 **TrainYourself-V1_1-1** 的 StockSharp 版本。原始脚本依靠图表按钮来重建趋势线并手动下单，而移植后的版本在每根完成的 K 线后自动计算 Donchian 通道，并在价格脱离通道时发起突破交易，同时提供等效的手动调用方法。

## 交易逻辑

1. **构建通道**
   - 每当订阅的 `CandleType` K 线完成，就会计算长度为 `ChannelLength` 的 `DonchianChannels` 指标。
   - 指标的上下轨会额外加上 `BufferPoints` × `PriceStep` 的缓冲区，复制了原策略先在当前买价/卖价外侧放置 50 个点再向最近高低点滑动的行为。
   - 计算得到的 `UpperBand` 与 `LowerBand` 通过只读属性暴露，可用于自定义仪表盘或监控界面。

2. **准备突破**
   - 当已有持仓或 `EnableTrendTrade` 为假时，突破逻辑保持未激活。
   - 只有在没有持仓的情况下，价格收盘位于通道内部，并且距离上下轨至少 `ActivationPoints` × `PriceStep` 的安全边际时，内部标志 `_isArmed` 才会变为真，对应 MT4 版本中将变量 `q` 设为 1 的条件。

3. **执行突破**
   - 一旦准备完毕，收盘价突破上轨（≥ `UpperBand`）且允许做多 (`AllowBuyOpen`)，就按 `Volume` 下达市价买单。
   - 收盘价跌破下轨（≤ `LowerBand`）且允许做空 (`AllowSellOpen`)，则下达市价卖单。
   - 成交后策略会立即解除准备状态，直到仓位再次归零并满足重新激活条件。

4. **风险控制**
   - `StartProtection` 根据 `StopLossPoints` 与 `TakeProfitPoints` 自动创建保护性止损/止盈单。距离通过点值乘以合约的 `PriceStep` 计算，当交易所未提供步长时退回到 `0.0001`，与 MT4 的 `Point` 定义保持一致。

5. **手动控制**
   - MT4 中的图标（`BUY_TRIANGLE`、`SELL_TRIANGLE`、`CLOSE_ORDER`）对应到公开方法 `TriggerManualBuy()`、`TriggerManualSell()` 与 `ClosePositionManually()`。
   - 这些方法会检查 `AllowBuyOpen`/`AllowSellOpen` 以及 `IsFormedAndOnlineAndAllowTrading()`，并在执行后关闭自动突破的准备状态，防止手动仓位被自动信号立即覆盖。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | `30m` 周期 | 用于所有计算的主 K 线数据订阅。 |
| `ChannelLength` | `20` | Donchian 通道的历史长度。 |
| `BufferPoints` | `50` | 在最终通道之外额外扩张的 MetaTrader 点数。 |
| `ActivationPoints` | `2` | 价格在通道内必须保持的安全边际（点数）。 |
| `StopLossPoints` | `100` | 止损距离（点数），通过乘以 `PriceStep` 转换为价格。 |
| `TakeProfitPoints` | `100` | 止盈距离（点数），通过乘以 `PriceStep` 转换为价格。 |
| `EnableTrendTrade` | `true` | 是否启用自动突破交易；关闭后仅保留手动方法。 |
| `Volume` | `1` | 自动和手动交易使用的下单手数。 |

## 使用提示

- 原 EA 需要拖动图表对象来更新趋势线，移植版本在每根 K 线结束时都会重新计算通道，无需人工干预。
- 暴露的 `UpperBand`、`LowerBand` 和 `IsArmed` 属性便于在界面上还原原始脚本的状态提示。
- 将 `StopLossPoints` 或 `TakeProfitPoints` 设为 `0` 可以禁用对应的保护单，与 MT4 脚本中跳过修改的逻辑一致。
- 手动下单同样使用 `Volume` 参数，并自动继承已配置的止损/止盈距离。
- 若需要人工重置突破状态，可调用 `ClosePositionManually()`，或者等待价格重新进入通道并再次满足激活条件。
