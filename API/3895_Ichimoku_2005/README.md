# Ichimoku 2005 Strategy

This strategy is a direct port of the MetaTrader expert advisor `ichimok2005` tailored for the StockSharp high-level API. It focuses on identifying decisive breakouts above or below the Ichimoku Senkou Span B line and confirms momentum through consecutive candle bodies.

## Trading Logic

### Long setup
1. Evaluate the last `Shift + 2` completed candles (the default `Shift` is `1`, so the algorithm observes the previous three bars).
2. Require that:
   - The oldest reference candle (`Shift + 2`) opened below Senkou Span B.
   - The middle reference candle (`Shift + 1`) opened above Senkou Span B and closed above it.
   - The most recent reference candle (`Shift`) opened and closed above Senkou Span B.
   - The last two reference candles are bullish (close price is greater than open price).
3. Ensure that the Ichimoku Chinkou Span is not trapped inside the cloud when Senkou Span A is below Senkou Span B. This mimics the original expert advisor filter that avoids congested market phases.
4. If the strategy currently holds a short position, it is closed. Otherwise a new long trade is opened provided the previous signal was not already long.

### Short setup
1. Mirror the long conditions in the opposite direction:
   - Candle `Shift + 2` must open above Senkou Span B.
   - Candle `Shift + 1` must open and close below Senkou Span B.
   - Candle `Shift` must open and close below Senkou Span B.
   - The last two reference candles are bearish (close price is less than open price).
2. The Chinkou Span must stay outside the cloud when Senkou Span A is below Senkou Span B.
3. Close any existing long position, then open a new short position if the previous signal was not short.

Positions are managed with StockSharp's protective orders. Stop loss and take profit are measured in price steps and converted to absolute distances using the instrument's `PriceStep`. Protective orders are registered with market exits to replicate the MetaTrader behaviour of using market stops.

## Position Sizing

The original advisor supported two sizing modes:
- **Fixed volume** (`UseMoneyManagement = false`): trades are executed with the `OrderVolume` parameter (default 0.1 lots).
- **Money management** (`UseMoneyManagement = true`): the strategy uses the portfolio's current value and the `MaximumRisk` percentage to derive the order size. The result is snapped to the security's lot step and never falls below a single step.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `StopLossPoints` | Stop-loss distance in price steps. | 30 |
| `TakeProfitPoints` | Take-profit distance in price steps. | 60 |
| `Shift` | Number of bars used as an offset when validating the breakout structure. | 1 |
| `OrderVolume` | Fixed trade size when money management is disabled. | 0.1 |
| `MaximumRisk` | Portfolio percentage used to size orders when money management is enabled. | 10 |
| `UseMoneyManagement` | Enables risk-based position sizing. | false |
| `TenkanPeriod` | Tenkan-sen period of the Ichimoku indicator. | 9 |
| `KijunPeriod` | Kijun-sen period of the Ichimoku indicator. | 26 |
| `SenkouBPeriod` | Senkou Span B period of the Ichimoku indicator. | 52 |
| `CandleType` | Timeframe for all calculations (defaults to hourly candles). | 1 hour |

## Notes

- Only completed candles are processed, guaranteeing that the Ichimoku values are final.
- The strategy keeps track of the last executed direction (`_lastSignal`) to avoid repeating identical orders on consecutive signals, matching the MetaTrader expert behaviour.
- If the instrument does not publish `PriceStep`, the stop-loss and take-profit distances are treated as absolute price values.
