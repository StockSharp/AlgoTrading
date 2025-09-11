# Aurora Divergence Strategy

This strategy trades divergences between price and On-Balance Volume (OBV). It compares linear regression slopes of price and OBV to detect potential reversals.

## Key Features

- Linear regression slope comparison for divergence signals.
- Optional z-score filter to avoid overextended prices.
- Higher timeframe moving average filter for trend confirmation.
- ATR-based volatility threshold and risk management with dynamic stop and target.
- Cooldown after each trade and maximum bars in position.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Candle timeframe for main calculations. |
| `Lookback` | Period for slope calculations. |
| `ZLength` | Lookback for mean and standard deviation in z-score filter. |
| `ZThreshold` | Maximum absolute z-score to allow entries. |
| `UseZFilter` | Enable or disable z-score filter. |
| `HtfCandleType` | Higher timeframe for trend moving average. |
| `HtfMaLength` | Moving average length on higher timeframe. |
| `AtrLength` | ATR period for volatility and risk. |
| `AtrThreshold` | Minimum ATR value to allow trading. |
| `StopAtrMultiplier` | ATR multiplier for stop-loss distance. |
| `ProfitAtrMultiplier` | ATR multiplier for take-profit distance. |
| `MaxBarsInTrade` | Maximum bars to hold a position. |
| `CooldownBars` | Bars to wait after a trade before signaling again. |

## Complexity

Intermediate

