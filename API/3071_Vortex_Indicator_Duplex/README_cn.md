# Vortex Indicator Duplex 策略

该策略将 MetaTrader 专家顾问 **Exp_VortexIndicator_Duplex** 迁移到 StockSharp 高级 API。策略维护两个独立的 Vortex 指标流：一个负责多头信号，另一个负责空头信号。每个流都可以配置自己的时间周期、指标周期以及信号偏移，从而实现对多空逻辑的差异化设置。

## 运行逻辑

1. 根据 `LongCandleType` 与 `ShortCandleType` 打开两条蜡烛订阅，每条订阅驱动各自的 `VortexIndicator` 实例。
2. 每根蜡烛收盘后记录最新的 VI+ 与 VI- 数值。参数 `LongSignalBar` / `ShortSignalBar` 指定在回看多少根已收盘蜡烛时产生信号，行为与原始 EA 的 `SignalBar` 输入保持一致。
3. **做多开仓**（当 `AllowLongEntries = true` 时）：若当前长周期 VI+ 高于 VI-，且上一采样点 VI+ 小于等于 VI-，则发送买入市价单。如果账户中存在空头仓位，会先平掉空头再建立多头。
4. **做多平仓**（当 `AllowLongExits = true` 时）：当长周期 VI- 上穿 VI+ 时平掉多头。同时监控以价格步长表示的保护距离 `LongStopLossSteps` 与 `LongTakeProfitSteps`；一旦触发也会立即平仓。
5. **做空开仓**（当 `AllowShortEntries = true` 时）：若当前短周期 VI+ 低于 VI-，且上一采样点 VI+ 大于等于 VI-，则发送卖出市价单，并在必要时平掉多头仓位。
6. **做空平仓**（当 `AllowShortExits = true` 时）：当短周期 VI+ 再次上穿 VI- 时回补空头，同时监控 `ShortStopLossSteps` 与 `ShortTakeProfitSteps`。
7. 交易量由 `TradeVolume` 控制。策略使用品种的 `Security.PriceStep` 将“步长”转换成实际价格差，若参数为 0 则关闭相应保护规则。

所有止损/止盈检查都会在两个时间框架的每根收盘蜡烛上执行。当账户没有持仓时，会重置缓存的入场信息，行为与原始 MT5 版本保持一致。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `LongCandleType` | H4 | 多头 Vortex 指标使用的时间周期。 |
| `ShortCandleType` | H4 | 空头 Vortex 指标使用的时间周期。 |
| `LongLength` | 14 | 多头指标的 VI 周期。 |
| `ShortLength` | 14 | 空头指标的 VI 周期。 |
| `LongSignalBar` | 1 | 多头信号使用的已收盘蜡烛偏移（0 表示最新收盘）。 |
| `ShortSignalBar` | 1 | 空头信号使用的已收盘蜡烛偏移。 |
| `AllowLongEntries` | true | 是否允许开多。 |
| `AllowLongExits` | true | 是否允许平多。 |
| `AllowShortEntries` | true | 是否允许开空。 |
| `AllowShortExits` | true | 是否允许平空。 |
| `LongStopLossSteps` | 1000 | 多头止损距离，单位为价格步长。 |
| `LongTakeProfitSteps` | 2000 | 多头止盈距离，单位为价格步长。 |
| `ShortStopLossSteps` | 1000 | 空头止损距离，单位为价格步长。 |
| `ShortTakeProfitSteps` | 2000 | 空头止盈距离，单位为价格步长。 |
| `TradeVolume` | 1 | 开仓时使用的基础交易量。 |

## 使用提示

- 在开立新仓前会先平掉反向头寸，对应了 MT5 中通过不同 Magic 编号管理多空的做法。
- 价格步长转换公式为 `distance = steps * Security.PriceStep`；若品种未提供步长，则默认使用 1。
- 将任意保护参数设为 0 可以关闭该保护，而信号型出场仍然有效。
- 由于两个时间框架都会触发风控检查，请根据市场流动性合理设置 `TradeVolume`，避免频繁反复开仓。
