# Center of Gravity OSMA Strategy

This strategy uses the **Center of Gravity OSMA** oscillator to detect potential trend reversals.
The oscillator multiplies simple and weighted moving averages, smooths the result twice and tracks
changes in direction. When the indicator forms a local minimum and turns upward, the strategy
closes short positions and may open a new long position. When a local maximum turns downward,
long positions are closed and optional shorts are opened.

## How It Works
1. Close price is used as input for the custom indicator.
2. The indicator calculates:
   - Simple moving average (`SMA`) with length `Period`.
   - Weighted moving average (`WMA`) with the same length.
   - Product of these two averages.
   - Two additional smoothing steps with lengths `SmoothPeriod1` and `SmoothPeriod2`.
3. Trading rules:
   - If previous value was lower than the value before it and the current value is higher than the previous one, the oscillator turned up. Any short position is closed and a long may be opened.
   - If previous value was higher than the value before it and the current value is lower than the previous one, the oscillator turned down. Any long position is closed and a short may be opened.
   - Optional stop loss and take profit values in price units protect open positions.

## Parameters
- `Period` – base period for SMA and WMA.
- `SmoothPeriod1` – length of the first smoothing stage.
- `SmoothPeriod2` – length of the second smoothing stage.
- `StopLoss` – stop loss distance in price units (0 to disable).
- `TakeProfit` – take profit distance in price units (0 to disable).
- `BuyPosOpen` – allow opening long positions.
- `SellPosOpen` – allow opening short positions.
- `BuyPosClose` – allow closing long positions on a sell signal.
- `SellPosClose` – allow closing short positions on a buy signal.
- `CandleType` – candle type (timeframe) for calculations.

## Notes
- Only the C# version is provided. The Python folder is intentionally absent.
- Use tabs for indentation when modifying the code.
