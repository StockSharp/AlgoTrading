# Tengri Strategy (StockSharp Port)

This strategy is a high-level StockSharp recreation of the MetaTrader expert advisor *Tengri*. The original advisor trades EURUSD and USDCHF with a grid-and-scale approach driven by RSI, custom "Silence" volatility filters and an EMA trend gauge. The C# version keeps the behavioural core while adapting it to StockSharp conventions and net-position accounting.

## Core Ideas

- **Directional bias** – compare the current bid to the opening price of a higher timeframe candle (default 30 minutes). A positive difference biases the strategy long, a negative difference biases it short.
- **Momentum filter** – a 14-period RSI calculated on hourly candles must stay below 70 for long entries and above 30 for short entries, matching the MetaTrader logic.
- **Quiet-market filters** – the original custom "Silence" indicator is emulated with ATR values smoothed by EMAs on two different timeframes. Both filters must stay below configurable thresholds to permit entries or scale-ins.
- **Trend confirmation** – an EMA on a medium timeframe ensures long additions only happen above the EMA and short additions only below it.
- **Grid and martingale sizing** – the first trade uses either a fixed lot or an equity-proportional lot. Additional trades multiply the previous volume by configurable factors (1.70 before `StepX`, 2.08 afterwards by default).
- **Pip spacing** – the distance between grid orders follows two base steps (10 pips and 20 pips by default) and can be grown exponentially through `PipStepExponent`.

## Trading Workflow

1. **Entry evaluation** (per `EntryCandleType`, default M1):
   - Determine direction from the `DealCandleType` candle.
   - Check RSI and the first silence filter.
   - Ensure no active trades in the same direction (opposite direction positions are flattened first because StockSharp portfolios are netted).
   - Submit a market order with the calculated lot size. The first position stores a pip-based take-profit target.
2. **Scale-in evaluation** (per `ScaleCandleType`, default M1):
   - Confirm the EMA trend and the second silence filter.
   - Verify the last fill price is far enough from the current market using the pip-step rules.
   - Add another market order with martingale sizing while the direction stays valid and the trade count is below `MaxTrades`.
3. **Position management**:
   - Optional global profit target closes the position when both long and short stacks exist and the combined unrealised PnL exceeds `Equity / LimitDivisor`.
   - The first trade's take-profit acts as a simple exit: when the bid/ask reaches the stored target the entire position is flattened.
   - No automatic stop-loss is used, mirroring the MetaTrader code.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `DealCandleType` | Timeframe whose open price defines the directional bias. |
| `EntryCandleType` | Timeframe for evaluating entry signals. |
| `ScaleCandleType` | Timeframe for checking grid additions. |
| `MaCandleType` | Timeframe for the EMA trend filter. |
| `Silence1CandleType` / `Silence2CandleType` | Timeframes for ATR-based volatility filters. |
| `RsiPeriod` | RSI length (default 14). |
| `SilencePeriod1/2`, `SilenceInterpolation1/2`, `SilenceLevel1/2` | ATR smoothing and thresholds controlling the two silence filters. |
| `MaPeriod` | EMA period. |
| `PipStep`, `PipStep2`, `PipStepExponent` | Distances between scale-in trades. |
| `LotExponent1`, `LotExponent2`, `StepX` | Martingale factors for additional positions. |
| `LotSize`, `FixLot`, `LotStep` | Money management settings for the first position. |
| `SlTpPips` | Pip distance used to set a take-profit for the first trade (0 disables it). |
| `MaxTrades` | Maximum number of entries per direction. |
| `UseLimit`, `LimitDivisor` | Global profit lock configuration. |
| `CloseFriday`, `CloseFridayHour` | Optional late-Friday entry lockout. |

## Differences from the MetaTrader Version

- **Silence indicator replacement** – the proprietary "Silence" indicator is approximated with ATR values smoothed by EMAs. Thresholds retain the same numeric scale but can be tuned if the ATR proxy behaves differently.
- **Net position accounting** – StockSharp portfolios are netted, so the strategy flattens the opposite direction before opening a new stack instead of hedging both sides simultaneously.
- **Take-profit handling** – MetaTrader attaches TP only to the first order. The port closes the full net position when that target triggers. Additional orders intentionally have no TP, matching the original risk model.
- **Symbol choice** – the strategy uses the `Security` assigned to the strategy instance. Configure separate instances for EURUSD, USDCHF or any other instrument.

## Usage Notes

- Configure the volume step and min/max volumes on the target security so that `LotCheck` style rounding aligns with broker requirements.
- The strategy assumes the broker quotes provide best bid/ask updates. Without Level1 data the direction and TP checks cannot operate.
- Because there is no stop-loss, consider running the strategy with external risk controls (equity stop, manual supervision, etc.).

## Visualisation

To analyse the behaviour connect chart widgets to the subscribed candle series (direction, entry and scaling timeframes) plus overlay the EMA and ATR indicators. This mirrors the diagnostic tools used with the original advisor.
