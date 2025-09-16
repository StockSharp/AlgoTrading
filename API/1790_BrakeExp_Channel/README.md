# BrakeExp Channel Strategy

This strategy trades based on the **BrakeExp** indicator, which builds an exponential channel around price movements. The indicator switches between long and short regimes and generates buy or sell signals when price crosses the dynamic channel borders.

## How It Works

- The indicator maintains an exponential curve that follows price.
- When the curve is below price (uptrend), the strategy looks for buy signals.
- When the curve is above price (downtrend), the strategy looks for sell signals.
- A crossing from one side to the other produces an entry signal in the new direction and closes the opposite position.

## Parameters

- `Candle Type` – timeframe of processed candles.
- `Volume` – order volume used for market entries.
- `A`, `B` – parameters defining the shape of the BrakeExp curve.
- `Buy Open` / `Sell Open` – permission to open long or short positions.
- `Buy Close` / `Sell Close` – permission to close short or long positions.

## Notes

This implementation focuses on the core logic of the BrakeExp indicator and does not include stop-loss or take-profit management. Additional risk controls can be added if required.
