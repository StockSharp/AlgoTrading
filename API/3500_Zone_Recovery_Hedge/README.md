# Zone Recovery Hedge Strategy

The **Zone Recovery Hedge Strategy** is a StockSharp port of the MetaTrader expert advisor *Zone Recovery Hedge V1*. The algorithm alternates buy and sell positions around an anchor price so that a new order is placed whenever price crosses the configured recovery zone. The sequence expands position volume following a martingale schedule until either the floating profit target or the optional loss protection is reached.

## Strategy logic

1. **Entry filter** – When *RSI Multi-Timeframe* mode is selected the strategy inspects a configurable list of RSI readings (from M1 up to MN1) and requires that each enabled timeframe leaves an overbought/oversold area simultaneously. Crossing from oversold generates a buy cycle, while crossing from overbought starts a sell cycle. In *Manual* mode the helper methods `StartManualMarketCycle` and `StartManualPendingCycle` can be called to begin a new sequence without automatic signals.
2. **Initial trade** – The first trade uses either the fixed lot size or a risk-based size derived from portfolio equity and the planned stop distance. When ATR sizing is active the stop distance and zone width are derived from the daily ATR; otherwise broker points are used.
3. **Recovery grid** – If price travels against the active direction by the recovery zone distance, the strategy opens the opposite side with an increased volume (custom lot ladder, multiplier, or additive step). The cycle keeps alternating directions around the original anchor price, building volume until the profit target is hit or the maximum number of trades is reached.
4. **Profit control** – The target is evaluated in account currency, using either the base take-profit distance or the dedicated recovery take-profit (with optional ATR fractions). Commissions can be simulated through the *Test Commission* parameter. When the floating profit exceeds the target plus costs the entire cycle is closed.
5. **Risk guard** – If `MaxTrades` is non-zero and `SetMaxLoss` is enabled, reaching the maximum trade count while the floating PnL breaches the `MaxLoss` limit will close all positions and reset the cycle.

> **Note:** StockSharp strategies are netted by default. The port reproduces the recovery logic by reversing the net position rather than holding simultaneous hedged positions. This keeps the profit math compatible with StockSharp while preserving the alternating recovery steps of the original advisor.

## Parameters

| Group | Parameter | Description |
| --- | --- | --- |
| General | `CandleType` | Primary timeframe that drives the entry logic. |
| General | `Mode` | `Manual` disables signals, `RsiMultiTimeframe` activates the RSI filter. |
| Signals | `RsiPeriod`, `OverboughtLevel`, `OversoldLevel` | RSI calculation period and thresholds. |
| Signals | `UseM1Timeframe` … `UseMonthlyTimeframe` | Enable/disable the RSI confirmations for the corresponding timeframe. |
| Signals | `TradeOnBarOpen` | Use the previous bar as the confirmation bar (original EA behaviour). |
| Recovery | `RecoveryZoneSize`, `TakeProfitPoints` | Zone width and base take-profit when ATR is disabled. |
| Recovery | `UseAtr`, `AtrPeriod`, `AtrZoneFraction`, `AtrTakeProfitFraction`, `AtrRecoveryFraction`, `AtrCandleType` | ATR based sizing settings. |
| Recovery | `UseRecoveryTakeProfit`, `RecoveryTakeProfitPoints` | Dedicated take-profit distance when the cycle is already in recovery. |
| Risk | `MaxTrades`, `SetMaxLoss`, `MaxLoss` | Limit the number of trades and define a money-based loss guard. |
| Risk | `TestCommission` | Estimated commission (in money) applied per trade volume when evaluating the profit target. |
| Money Management | `RiskPercent`, `InitialLotSize`, `LotMultiplier`, `LotAddition`, `CustomLotSize1` … `CustomLotSize10` | Controls how volumes are generated for each step in the cycle. |
| Timer | `UseTimer`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute`, `UseLocalTime` | Restrict trading to a daily time window. |
| Manual | `PendingPrice` | Reference price used by `StartManualPendingCycle`. |

## Usage tips

- Attach the strategy to a data source that provides the highest timeframe you wish to use for RSI confirmations. Higher timeframes can be built from the base timeframe by the internal aggregator.
- When the *Manual* mode is active, call `StartManualMarketCycle(true)` or `StartManualMarketCycle(false)` to open a buy or sell cycle at the current price, or `StartManualPendingCycle` to anchor the cycle at a custom price level.
- Balance-based position sizing caps the risk percentage at 10% just like the original EA.
- The recovery logic assumes that `Security.PriceStep` and `Security.StepPrice` are filled by the connector. Without them the profit target cannot be computed.

## Differences from the MetaTrader version

- The StockSharp port works with net positions instead of hedged long/short baskets. The recovery sequence still alternates trade directions but positions are reversed when switching direction.
- Graphical elements (buttons, lines, comments) from the MT4 panel are not reproduced. Timer and manual commands are exposed through strategy parameters and helper methods.
- Spread-based cost modelling is omitted; only the `TestCommission` value is subtracted from the profit target.
