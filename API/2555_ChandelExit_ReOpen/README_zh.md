# Chandel Exit 再入场策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 智能交易系统“Exp_ChandelExitSign_ReOpen”移植到 StockSharp 的高级 API。它利用 Chandelier Exit 指标捕捉趋势突破，并在趋势持续时自动加仓。信号基于可配置的高周期 K 线计算，同时通过基于 ATR 的止损和可选的止盈保护仓位。

核心思想是把 Chandelier Exit 既当作趋势过滤器，又当作动态的追踪边界。当下轨向上穿越上轨时判定为多头信号，反向穿越则给出空头信号。多空逻辑完全对称，并且每类信号都可以通过参数单独启用或关闭。开仓后，只有当价格朝有利方向移动至少 `PriceStepPoints` 个最小价位时，系统才允许再次加仓，且总加仓次数受 `MaxAdditions` 限制，避免仓位无限膨胀。

## 交易逻辑

- **信号计算**
  - `RangePeriod`（配合 `Shift` 偏移）决定 Chandelier Exit 所使用的最高价和最低价窗口。
  - `AtrPeriod` 与 `AtrMultiplier` 共同生成波动缓冲区，将退出带从价格中移开。
  - `SignalBar`（默认 1）让策略在上一根完整 K 线上执行，复现 MT5 的延迟逻辑。
- **入场条件**
  - **做多**：当下轨穿越上轨（`IsUpSignal`）且 `EnableBuyEntries = true` 时触发。若已有空头持仓且 `EnableSellExits = true`，策略先尝试平掉空单。
  - **做空**：当上轨穿越下轨（`IsDownSignal`）且 `EnableSellEntries = true` 时触发。若有多头持仓，只在 `EnableBuyExits = true` 时才会先行平仓。
- **出场条件**
  - **多单**：在 `EnableBuyExits = true` 且出现空头信号时全部平仓，或当止损/止盈被击中时平仓。
  - **空单**：在 `EnableSellExits = true` 且出现多头信号时全部平仓，或当保护位触发时平仓。
  - 当多空同时允许开平仓时，策略会回溯更早的指标值，确保即使当前 K 线只产生入场，也能找到合适的出场信号。
- **加仓规则**
  - 每次成交后都会记录最近的成交价。只有当价格朝有利方向移动不少于 `PriceStepPoints * PriceStep` 时，才会按 `Volume` 加仓一次，最多执行 `MaxAdditions` 次。
  - 每次加仓都会重新计算止损/止盈，使保护价格紧跟最新仓位。
- **风险控制**
  - `StopLossPoints` 与 `TakeProfitPoints` 以最小价位为单位设置止损和止盈距离，填写 0 即可关闭相应功能。
  - 每根完成的 K 线都会检查保护条件，若价格在柱内触发保护位，则立即以市价平仓。

## 默认参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | `TimeSpan.FromHours(4).TimeFrame()` | 计算指标时使用的时间周期。 |
| `RangePeriod` | 15 | 计算最高/最低价时的窗口长度。 |
| `Shift` | 1 | 在计算窗口之前跳过的最近 K 线数量。 |
| `AtrPeriod` | 14 | ATR 的周期。 |
| `AtrMultiplier` | 4 | ATR 乘数。 |
| `SignalBar` | 1 | 信号回溯的完成 K 线数量。 |
| `PriceStepPoints` | 300 | 允许加仓前价格需移动的最小价位数量。 |
| `MaxAdditions` | 10 | 首次开仓后允许的最大加仓次数。 |
| `StopLossPoints` | 1000 | 止损距离（以最小价位表示）。 |
| `TakeProfitPoints` | 2000 | 止盈距离（以最小价位表示）。 |
| `EnableBuyEntries` / `EnableSellEntries` | `true` | 是否开启多头/空头入场。 |
| `EnableBuyExits` / `EnableSellExits` | `true` | 是否允许多头/空头出场。 |

## 使用建议

- `Volume` 决定基础下单手数，加仓同样使用该手数。若想降低风险，可调小 `Volume` 或减少 `MaxAdditions`。
- 由于阈值按最小价位定义，请确保合约的 `PriceStep` 信息正确，否则距离计算会失真。
- 将 `SignalBar` 设为 1 可以避免在产生信号的同一根 K 线上立即成交；若希望更激进，可以设置为 0。
- 策略适用于可做多做空的市场。如果只想交易单边，可通过参数禁用另一方向。
- 当图表区域可用时，策略会自动调用 `DrawCandles`、`DrawIndicator` 和 `DrawOwnTrades`，便于观察指标与成交情况。

## 示例流程

1. 观察到下轨向上穿越上轨，出现多头信号。
2. 若无持仓且允许做多，则按 `Volume` 下市价买单，同时根据成交价设置止损和止盈。
3. 当价格向上运行不少于 `PriceStepPoints * PriceStep` 时，在不超出 `MaxAdditions` 的前提下再次买入加仓。
4. 出现反向信号、触及止损或止盈时平掉全部多单；空头流程与之相反。

该说明在保持 MT5 原始逻辑的同时，遵循 StockSharp 的常见约定：通过策略参数管理配置、使用高级 K 线订阅以及显式的仓位管理。
