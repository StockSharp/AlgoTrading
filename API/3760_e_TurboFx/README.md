# e-TurboFx Classic Strategy

## Overview
The **e-TurboFx Classic** strategy is a direct C# port of the MetaTrader 4 expert adviser found in `MQL/7262/e-TurboFx.mq4`. It detects momentum exhaustion after a streak of strong candles with progressively larger bodies and enters in the opposite direction. The StockSharp version uses the high-level strategy API with candle subscriptions, automatic protective orders and UI-friendly parameters.

## Trading logic
1. Subscribe to the configured candle type and inspect only finished candles.
2. Measure the candle body size (`|close - open|`) to detect expansion.
3. Maintain two counters:
   - **Bearish sequence** – counts consecutive bearish candles with bodies larger than the previous bearish candle.
   - **Bullish sequence** – counts consecutive bullish candles with bodies larger than the previous bullish candle.
4. Reset both sequences when a doji (open equals close) appears or whenever a position is already open. This mimics the original EA behaviour that keeps only one trade at a time.
5. **Long entry:** when the bearish sequence length reaches the configured `SequenceLength`, send a market buy order and immediately reset the counters.
6. **Short entry:** when the bullish sequence length reaches `SequenceLength`, send a market sell order and reset the counters.
7. Optional stop-loss and take-profit levels are translated from point distances into StockSharp price steps.

The algorithm therefore waits for a capitulation-like move where each candle accelerates in the same direction. The following reversal order attempts to fade that extreme momentum.

## Implementation details
- Uses `SubscribeCandles().Bind(ProcessCandle)` to process finished candles without manual indicator management.
- Integrates with `StartProtection` so that stop-loss and take-profit distances are converted into exchange price steps (`UnitTypes.Step`).
- Parameters are registered through `Param(...)` so they appear in the UI and can be optimised.
- The strategy works with any instrument that exposes a valid `PriceStep`; otherwise stop/target distances should stay at `0`.
- While a position is active the signal detection is paused and internal counters are cleared, just like the original MQL script which refused to stack orders.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `SequenceLength` | Number of consecutive finished candles with expanding bodies required to trigger an entry. | `3` |
| `TakeProfitSteps` | Take-profit distance measured in price steps (ticks). `0` disables the target. | `120` |
| `StopLossSteps` | Stop-loss distance measured in price steps (ticks). `0` disables the stop. | `70` |
| `TradeVolume` | Volume for market entries. Changing it updates the `Volume` property instantly. | `0.1` |
| `CandleType` | Candle timeframe used for analysis. Defaults to 1-hour candles. | `1 hour` |

## Usage notes
- The strategy expects clean candle data. When switching instruments or timeframes allow the caches to rebuild so that the counters reflect fresh candles only.
- Because the system relies on strict body expansion, tiny or equal candle bodies reset the sequence. Adjust `SequenceLength` when trading on noisier timeframes.
- Backtest multiple timeframe/volume combinations to find instruments where exhaustion moves are frequent enough to compensate for spreads and slippage.
- Always validate the behaviour in a sandbox environment before enabling live trading.
