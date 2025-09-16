# Vortex Indicator Duplex Strategy

This strategy converts the MetaTrader expert **Exp_VortexIndicator_Duplex** to the StockSharp high level API. Two independent Vortex indicator streams are maintained: one governs long trades and the other governs short trades. Each stream can use its own timeframe, indicator length and bar shift, allowing asymmetric behaviour between bullish and bearish setups.

## How it works

1. Two candle subscriptions are opened according to `LongCandleType` and `ShortCandleType`. Each feed updates its own `VortexIndicator` instance.
2. On every finished candle the strategy records the latest VI+ and VI- values. The `LongSignalBar`/`ShortSignalBar` parameters define how many closed candles back should be used for signal evaluation, matching the MetaTrader `SignalBar` input.
3. **Long entry** – allowed when `AllowLongEntries = true`. A buy order is sent if the current long-stream VI+ value is above VI- while the previous sampled value had VI+ less than or equal to VI-. Any existing short exposure is flattened before the new long position is established.
4. **Long exit** – enabled through `AllowLongExits`. The long position is closed when the long-stream VI- value rises above VI+. In addition, protective stop-loss and take-profit levels expressed in price steps (`LongStopLossSteps`, `LongTakeProfitSteps`) are monitored on every candle; hitting either threshold also closes the trade.
5. **Short entry** – governed by `AllowShortEntries`. A sell order is placed when the short-stream VI+ drops below VI- after previously being above it. Existing long exposure is flattened during the reversal.
6. **Short exit** – controlled by `AllowShortExits`. The short position is covered when VI+ climbs back above VI-. Protective distances (`ShortStopLossSteps`, `ShortTakeProfitSteps`) close the trade if reached.
7. Position sizing uses the `TradeVolume` parameter. The strategy relies on the instrument `PriceStep` to convert step counts into absolute price distances; setting a step parameter to zero disables the corresponding protective rule.

The stop/take checks are evaluated on every finished candle from both timeframes. If the account has no position, cached entry data is cleared to mirror the MetaTrader implementation.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `LongCandleType` | H4 | Timeframe used for the long-side Vortex indicator. |
| `ShortCandleType` | H4 | Timeframe used for the short-side indicator. |
| `LongLength` | 14 | VI period applied to the long stream. |
| `ShortLength` | 14 | VI period applied to the short stream. |
| `LongSignalBar` | 1 | Closed-candle offset for the long evaluation (0 = current finished bar). |
| `ShortSignalBar` | 1 | Closed-candle offset for the short evaluation. |
| `AllowLongEntries` | true | Enables opening long positions. |
| `AllowLongExits` | true | Enables closing long positions. |
| `AllowShortEntries` | true | Enables opening short positions. |
| `AllowShortExits` | true | Enables closing short positions. |
| `LongStopLossSteps` | 1000 | Stop-loss distance for long trades, expressed in price steps. |
| `LongTakeProfitSteps` | 2000 | Take-profit distance for long trades, expressed in price steps. |
| `ShortStopLossSteps` | 1000 | Stop-loss distance for short trades, expressed in price steps. |
| `ShortTakeProfitSteps` | 2000 | Take-profit distance for short trades, expressed in price steps. |
| `TradeVolume` | 1 | Base market order size used when entering a position. |

## Execution notes

- The strategy closes any opposite position before opening a new one, effectively reproducing the MT5 behaviour where separate magic numbers managed long and short signals.
- Protective distances are converted via `distance = steps * Security.PriceStep`. Ensure the instrument has a valid price step; otherwise the strategy falls back to 1.0.
- Set any stop/take parameter to zero to disable that protection path while keeping signal-based exits active.
- Because both timeframes can trigger risk management, choose `TradeVolume` carefully to avoid repeated reversals on thin markets.
