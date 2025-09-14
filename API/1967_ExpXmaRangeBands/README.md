# Exp XMA Range Bands Strategy

This strategy replicates the logic of the MetaTrader sample "Exp_XMA_Range_Bands" using StockSharp high level API. It employs a Keltner Channel to define dynamic support and resistance based on a moving average and average true range. Trades are triggered when price re-enters the channel after moving outside.

## How It Works

1. Build a Keltner Channel using:
   - EMA period `MaLength`
   - ATR period `RangeLength`
   - ATR multiplier `Deviation`
2. When a candle closes above the previous upper band, any short position is closed. If the next candle closes back inside the channel (close ≤ current upper band) a long position is opened.
3. When a candle closes below the previous lower band, any long position is closed. If the next candle closes back inside (close ≥ current lower band) a short position is opened.
4. Stop-loss and take-profit levels are expressed in points and applied once a position is entered.

## Parameters

- `MaLength` – EMA period for the channel center.
- `RangeLength` – ATR period used for channel width.
- `Deviation` – Multiplier applied to ATR to compute bands.
- `StopLoss` – Stop loss in points (converted to price by `Security.PriceStep`).
- `TakeProfit` – Take profit in points (converted to price by `Security.PriceStep`).
- `CandleType` – Candle series used for calculations.

## Indicators

- KeltnerChannels (EMA + ATR)

