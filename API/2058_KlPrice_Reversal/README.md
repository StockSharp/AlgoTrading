# KlPrice Reversal Strategy

This strategy is a C# conversion of the original MQL5 expert **exp_i-KlPrice.mq5**. It implements a reversal system based on a normalized price oscillator. The oscillator compares the current price to a smoothed price band derived from a moving average and the average true range (ATR). Crossing predefined boundaries generates trading signals.

## How It Works

1. A simple moving average (SMA) smooths the closing price.
2. An Average True Range (ATR) estimates market volatility.
3. The oscillator is calculated as:
   
   `jres = 100 * (Close - (SMA - ATR)) / (2 * ATR) - 50`
4. The oscillator value is mapped to five color zones:
   - **4** – above the upper level
   - **3** – between zero and the upper level
   - **2** – between the upper and lower levels
   - **1** – between the lower level and zero
   - **0** – below the lower level
5. A long position opens when the oscillator leaves zone 4. A short position opens when it leaves zone 0. Existing positions close when the oscillator crosses zero.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Time frame for price data. |
| `PriceMaLength` | SMA period for price smoothing. |
| `AtrLength` | ATR period used to compute the price band. |
| `UpLevel` | Upper threshold of the oscillator. |
| `DownLevel` | Lower threshold of the oscillator. |
| `EnableBuy` | Allow opening long positions. |
| `EnableSell` | Allow opening short positions. |

## Usage

1. Create an instance of `KlPriceReversalStrategy`.
2. Set desired parameters.
3. Attach the strategy to a portfolio and security.
4. Start the strategy to receive signals and place orders.

The strategy uses market orders via `BuyMarket` and `SellMarket`. Position protection is activated through `StartProtection()`.

## Notes

- The implementation approximates the original MQL indicator by using built-in StockSharp indicators (`SimpleMovingAverage` and `AverageTrueRange`).
- All calculations are performed on finished candles only.
