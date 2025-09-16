# KWAN RDP Trend Strategy

This strategy is a StockSharp conversion of the MetaTrader expert `Exp_KWAN_RDP`. The logic calculates the KWAN RDP oscillator by combining three standard indicators and smoothing their product:

1. **DeMarker** – measures the relationship between recent highs and lows to gauge momentum exhaustion.
2. **Money Flow Index** – evaluates price and volume to detect overbought or oversold conditions.
3. **Momentum** – captures the speed of price changes using the selected period.
4. The raw value `100 * DeMarker * MFI / Momentum` is smoothed with a configurable moving average (SMA, EMA, SMMA, WMA, or Jurik).

The slope of the smoothed oscillator produces trade signals:

- **Bullish turn (rising slope)**: close short positions and optionally open a long position.
- **Bearish turn (falling slope)**: close long positions and optionally open a short position.
- Neutral bars (flat slope) do not trigger actions.

## Parameters

- `CandleType` – candle series for indicator calculations (default: H1 time frame).
- `DeMarkerPeriod` – period of the DeMarker indicator.
- `MfiPeriod` – period of the Money Flow Index.
- `MomentumPeriod` – period of the Momentum indicator.
- `SmoothingLength` – length of the smoothing moving average.
- `Smoothing` – smoothing method (Simple, Exponential, Smoothed, Weighted, Jurik).
- `EnableLongEntries` / `EnableShortEntries` – allow opening long or short positions.
- `CloseLongsOnReverse` / `CloseShortsOnReverse` – close opposing positions when a reversal signal appears.
- `TakeProfitPercent` / `StopLossPercent` – optional percentage-based protection applied through `StartProtection`.

## Trading Rules

1. Subscribe to the configured candle series and calculate DeMarker, MFI, Momentum, and the smoothed KWAN value on each finished candle.
2. Detect the slope direction of the latest oscillator value versus the previous one.
3. When the slope turns up, close shorts (if enabled) and open a long if long trading is allowed and no long position is active.
4. When the slope turns down, close longs (if enabled) and open a short if short trading is allowed and no short position is active.
5. Use the optional stop-loss and take-profit percentages to guard positions with platform protection.

## Notes

- Signals are only processed on completed candles to avoid intra-bar noise.
- The DeMarker calculation uses internal smoothing to match the MetaTrader implementation.
- All comments in the C# code are written in English as required by the project guidelines.
