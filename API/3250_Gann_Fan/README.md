# Gann Fan Strategy

This StockSharp strategy reproduces the **GANN_FAN** MetaTrader expert using the high-level API. It combines trend filters from linear weighted moving averages with momentum confirmation, a MACD direction gate, and a fractal-based reconstruction of the Gann fan to determine bullish or bearish bias. Risk management mirrors the original robot with stacked martingale-style entries, fixed stops, trailing protection, and optional break-even moves.

## Trading Logic

1. **Trend Filter** – Two linear weighted moving averages (LWMA) built on the typical price (H+L+C)/3 define the fast and slow trend. Long trades require the fast LWMA to stay above the slow LWMA; short trades need the inverse crossover.
2. **Momentum Confirmation** – The strategy calculates the classic momentum oscillator as `100 * Close / Close(n)` and evaluates the deviation from the neutral 100 level over the last three closed candles. At least one deviation must exceed the configured threshold to confirm strength in the direction of the trade.
3. **MACD Direction** – A configurable MACD signal (fast, slow, and signal EMA periods) must agree with the trend. Long entries require the MACD line to be greater than the signal line, while shorts need the MACD line to remain below the signal line.
4. **Gann Fan Orientation** – Confirmed Bill Williams fractals rebuild the bullish and bearish fan rays. The two most recent down fractals form the bullish ray; its slope must be positive to allow longs. The two latest up fractals define the bearish ray; its slope must be negative to authorize short sales.
5. **Position Stacking** – When a new signal arrives, the strategy can add to an existing position up to the configured maximum. Each additional order increases volume by multiplying the base lot by the lot exponent, emulating the martingale sizing used in the MQL version.

## Risk Management

- **Fixed Stop-loss and Take-profit** – Expressed in instrument price steps, automatically converted by the strategy using `Security.PriceStep`.
- **Break-even Control** – When enabled, once profit reaches the trigger distance the stop is advanced to entry plus/minus the configured offset.
- **Trailing Stop** – Activates after reaching the trigger distance. The stop can follow the market either by a fixed distance from the close or by locking in the lowest (for longs) / highest (for shorts) value from the most recent candles plus a padding factor.
- **Force Exit Switch** – Setting `Force Exit` to `true` immediately liquidates any open exposure on the next finished candle.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Volume** | Base order size used for the first entry. |
| **Fast LWMA / Slow LWMA** | Periods of the linear weighted moving averages used for the trend filter. |
| **Momentum Period / Threshold** | Lookback of the momentum calculation and minimal deviation from 100 required to trade. |
| **MACD Fast / Slow / Signal** | EMA periods for the MACD confirmation filter. |
| **Fractal History** | Maximum number of confirmed fractal points stored to build the Gann fan rays. |
| **Max Trades** | Maximum number of stacked entries allowed in a single direction. |
| **Lot Exponent** | Multiplier applied to the base volume for each additional entry. |
| **Stop Loss / Take Profit** | Protective distances in price steps. |
| **Enable Trailing** | Enables trailing stop management. |
| **Trail Trigger / Distance / Padding** | Profit trigger, trailing distance, and extra padding (in price steps) used when trailing via candle extremes. |
| **Use Candle Trail** | Enables candle-based trailing in addition to the fixed-distance trail. |
| **Trailing Candles** | Number of recent finished candles considered when computing candle-based trailing levels. |
| **Enable Break-even** | Switches break-even logic on or off. |
| **Break-even Trigger / Offset** | Profit trigger and offset (in price steps) for moving the stop to break-even. |
| **Use Gann Filter** | Enforces bullish/bearish fan orientation for entries. |
| **Force Exit** | Forces the strategy to close all positions on the next bar. |
| **Candle Type** | Candle series used for calculations and order generation. |

## Notes

- All indicator calculations work exclusively on finished candles provided by `SubscribeCandles` and `Bind` to comply with StockSharp high-level API best practices.
- Trailing and break-even distances automatically adapt to the instrument tick size. When `PriceStep` is unavailable, protective features remain idle until it is provided by the connector.
- The strategy keeps separate states for long and short positions, ensuring trailing and break-even levels are reset whenever the exposure changes direction.
- To mimic the MetaTrader expert closely, alerts, notifications, and explicit chart objects from the original code are replaced with StockSharp-native fan reconstruction using fractals.
