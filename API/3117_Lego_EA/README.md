# Lego EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Lego EA Strategy** is a direct port of the MetaTrader "Lego EA" expert advisor. It uses a configurable combination of technical filters—Commodity Channel Index, dual moving averages, stochastic oscillator, Accelerator Oscillator, DeMarker and Awesome Oscillator—to validate entries and exits. Each filter can be toggled on or off independently for entries and exits, allowing you to rebuild the original "Lego" block-by-block or experiment with custom setups.

## Parameters
- `Volume` – base trading volume used when the previous trade was profitable.
- `LotMultiplier` – multiplier applied to the last executed volume after a losing trade (martingale-style recovery).
- `StopLossPips` – protective stop expressed in pips (converted internally using the symbol’s tick size).
- `TakeProfitPips` – profit target in pips.
- `UseCciForEntry` / `UseCciForExit` – enable the CCI filter when opening or closing positions.
- `UseMaForEntry` / `UseMaForExit` – use the fast/slow moving-average crossover for confirmations.
- `UseStochasticForEntry` / `UseStochasticForExit` – require stochastic %K/%D alignment within configured thresholds.
- `UseAcceleratorForEntry` / `UseAcceleratorForExit` – require Accelerator Oscillator acceleration patterns.
- `UseDemarkerForEntry` / `UseDemarkerForExit` – apply DeMarker level checks.
- `UseAwesomeForEntry` / `UseAwesomeForExit` – include Awesome Oscillator momentum confirmation.
- `CciPeriod` – period of the Commodity Channel Index.
- `MaFastPeriod` / `MaSlowPeriod` – lookback lengths for the fast and slow moving averages.
- `MaShift` – number of completed bars to shift moving-average values back in time, reproducing the MT5 horizontal shift parameter.
- `MaMethod` – smoothing method (simple, exponential, smoothed, or weighted).
- `MaPrice` – candle price source supplied to both moving averages.
- `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlow` – stochastic oscillator configuration.
- `StochasticLevelUp` / `StochasticLevelDown` – overbought/oversold thresholds used for signals.
- `DemarkerPeriod`, `DemarkerLevelUp`, `DemarkerLevelDown` – DeMarker oscillator settings.
- `CandleType` – timeframe of the candle series used by all indicators.

## Trading workflow
1. On every completed candle the strategy collects indicator values from the selected filters.
2. Each filter computes buy/sell readiness based on the previous fully formed bar (matching the original EA’s `iGetArray(..., 1)` offset).
3. A long entry is permitted only when **all enabled entry filters** agree on a bullish signal. Likewise, a short entry requires unanimous bearish confirmation.
4. If the account is flat and a valid entry signal appears, a market order is sent using either the base `Volume` or the last losing trade volume multiplied by `LotMultiplier`.
5. When already in position, the enabled exit filters are evaluated the same way. The position is closed only when all exit filters agree on an opposing signal.
6. Stop-loss and take-profit protection is automatically installed using `StartProtection`, converting pip inputs into absolute price distances based on the symbol’s tick size.

## Money management
- After a winning trade the next order reverts to the base `Volume`.
- After a losing trade the volume is multiplied by `LotMultiplier`, emulating the original EA’s lot escalation logic.
- Exchange-imposed volume bounds (step, min and max) are enforced before each order.

## Notes and differences vs. MetaTrader version
- Indicator price sources map to StockSharp equivalents. CCI uses the typical price internally and moving averages use the selected `MaPrice` source.
- All indicator calculations rely on fully closed candles. This avoids partially formed data and mimics the EA’s "new bar" processing.
- Freeze-level checks and manual SL/TP price placement are handled by StockSharp’s `StartProtection` service.
- Partial position exits update the loss-tracking state only when the entire position is flat, matching the EA’s `DEAL_ENTRY_OUT` logic.

## Usage tips
- Start with the original configuration (MA filter enabled, other filters disabled) to reproduce baseline behaviour, then enable additional filters to tighten signal quality.
- Monitor account exposure when using high `LotMultiplier` values; risk grows quickly during streaks of losses.
- Combine the strategy with the Backtester to confirm whether your chosen filter mix aligns with the instruments you plan to trade.

This strategy currently has no Python version.
