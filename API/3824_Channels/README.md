# Channels Strategy

This strategy is a direct port of the MetaTrader 4 "Channels" expert advisor included in Gordago's public library. It combines a very fast exponential moving average (EMA) with three EMA-based envelopes to detect moments when price escapes from compressed zones. Once a single position is open the strategy relies on stop orders and optional trailing stops to manage exits, just like the original MQL implementation.

## Trading logic

- The strategy subscribes to hourly candles by default and calculates:
  - A fast EMA (length 2) using candle **close** prices.
  - A second fast EMA (length 2) using candle **open** prices, required by the short entry rules of the expert advisor.
  - A slow EMA (length 220) on closes that serves as the base for three envelope deviations: ±1.0%, ±0.7% and ±0.3%.
- A **long** position is opened when the close-based fast EMA satisfies any of the six historical cross checks:
  1. It crosses upward through the outer 1% lower envelope.
  2. It crosses upward through the 0.7% lower envelope.
  3. It spends two consecutive bars below the 0.3% lower envelope (oversold condition).
  4. It crosses upward through the slow EMA itself.
  5. It crosses upward through the 0.3% upper envelope.
  6. It crosses upward through the 0.7% upper envelope.
- A **short** position is opened when the open-based fast EMA triggers any of the symmetric short rules:
  1. It crosses downward through the outer 1% upper envelope.
  2. It crosses downward through the 0.7% upper envelope.
  3. It crosses downward through the 0.3% upper envelope.
  4. It crosses downward through the slow EMA.
  5. It crosses downward through the 0.3% lower envelope.
  6. It crosses downward through the 0.7% lower envelope.
- Only one market position can exist at a time. A new signal is ignored while a trade is active, matching the behaviour of the MetaTrader expert.

## Risk management

- Individual stop-loss and take-profit distances can be configured for long and short trades. When set to zero those protective orders are skipped, which replicates the disabled-by-default state from the original source.
- Optional trailing stops tighten the protective order once price moves in favour of the position by more than the trailing distance measured in points.
- All protective orders are cancelled automatically when the position is flattened or the strategy stops.

## Parameters

| Name | Description |
| ---- | ----------- |
| `Candle Type` | Timeframe used for price analysis (default: 1 hour). |
| `Volume` | Order size used for all entries. |
| `Fast EMA` / `Slow EMA` | Periods for the fast and slow EMAs. |
| `Envelope 1%`, `Envelope 0.7%`, `Envelope 0.3%` | Percentage width of the three envelope bands. |
| `Buy Stop-Loss`, `Sell Stop-Loss` | Distance in points between the entry price and the initial stop-loss for long or short trades. |
| `Buy Take-Profit`, `Sell Take-Profit` | Distance in points for the optional fixed take-profit levels. |
| `Buy Trailing`, `Sell Trailing` | Trailing stop distance in points for long or short positions. |
| `Use Trading Hours` | Enables the time window filter. |
| `From Hour`, `To Hour` | Inclusive hour-of-day boundaries for opening new positions. The window wraps around midnight if `From` is greater than `To`. |

## Usage notes

1. Because the stop distances are defined in points they are multiplied by the security `PriceStep` internally. Make sure this step matches the instrument used for trading.
2. The fast EMA length is intentionally very short to mirror the MT4 expert. Increasing it will dramatically change signal frequency.
3. The original advisor also allowed account whitelisting and sound alerts. Those were omitted as they are platform specific and do not affect order logic.
