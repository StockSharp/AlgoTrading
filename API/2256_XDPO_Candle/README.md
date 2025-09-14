# XDPO Candle Strategy

This strategy is a conversion of the original MQL5 expert **Exp_XDPOCandle**. It builds synthetic candles by applying two consecutive exponential moving averages to the open and close prices. The color of the resulting candle (bullish, bearish or neutral) drives trading decisions.

## Strategy Logic

1. Each incoming market candle is smoothed twice:
   - The first smoothing uses an EMA of length `FastLength`.
   - The second smoothing applies another EMA of length `SlowLength` to the result of the first one.
2. If the smoothed close is above the smoothed open, the candle is considered *bullish*.
3. If the smoothed close is below the smoothed open, the candle is considered *bearish*.
4. The strategy opens a long position when a bullish candle appears after a non-bullish one. It opens a short position when a bearish candle appears after a non-bearish one.
5. Existing opposite positions are closed automatically by reversing through market orders.

## Parameters

| Name | Description |
|------|-------------|
| `FastLength` | Length of the first EMA applied to prices. |
| `SlowLength` | Length of the second EMA applied to the first EMA result. |
| `CandleType` | The timeframe and type of candles used for calculation. |

## Usage

1. Attach the strategy to a security within StockSharp environment.
2. Configure the parameters if needed. Default values are tuned to match the original expert settings.
3. Start the strategy. It will subscribe to the specified candle type and trade on color changes of the smoothed candles.

## Notes

- Risk management is handled by `StartProtection()` with default settings. Adjust `Volume` and protection parameters externally as required.
- This repository currently contains only the C# version; the Python port is not provided.
