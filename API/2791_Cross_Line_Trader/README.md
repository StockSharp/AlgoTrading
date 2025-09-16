# Cross Line Trader Strategy

## Overview
The strategy emulates the original MetaTrader "Cross Line Trader" expert by reacting to price interactions with user-defined synthetic lines. Instead of listening to manual chart objects, the StockSharp version receives all line descriptions through a single parameter, parses them at start and continuously monitors finished candles. When a candle open moves through an active line, the strategy places a market order in the corresponding direction and deactivates that line so it cannot trigger again.

## Trading logic
1. The strategy subscribes to the candle type selected in the **Candle Type** parameter and only processes candles in the `Finished` state to avoid intrabar noise.
2. Synthetic lines are created from the **Line Definitions** parameter. Each line keeps its own state (active/expired, number of processed bars and geometry).
3. For **Trend** or **Horizontal** lines the algorithm compares the previous candle open with the next one relative to the line's price trajectory:
   - A long signal occurs when the previous open is below the line and the current open moves above it.
   - A short signal occurs when the previous open is above the line and the current open moves below it.
4. **Vertical** lines behave like timed triggers. Once the configured number of bars has elapsed the strategy opens a position immediately at the current candle open.
5. Direction is resolved according to **Direction Mode**:
   - `FromLabel` compares each line label against **Buy Label** and **Sell Label**.
   - `ForceBuy` and `ForceSell` treat all lines as the same direction regardless of labels.
6. Every successful trigger sends a market order with the volume from **Trade Volume**, logs the activation and marks the line as inactive.
7. Optional stop-loss and take-profit distances are applied on every new candle by evaluating the last entry price against candle highs and lows.

## Line definition format
The **Line Definitions** string uses semicolons to separate entries. Each entry must follow:

```
Name|Type|Label|BasePrice|SlopePerBar|Length|Ray
```

- **Name** – identifier shown in logs. Any string without semicolons.
- **Type** – `Horizontal`, `Trend` or `Vertical` (case-insensitive).
- **Label** – free text used when **Direction Mode** is `FromLabel`.
- **BasePrice** – initial price of the line at the first processed candle. Required for every non-vertical line (decimal, invariant culture).
- **SlopePerBar** – price change per candle for a trend line. Use `0` for horizontal lines.
- **Length** – meaning depends on the line type:
  - For trend or horizontal lines without a ray it defines how many bars the right anchor is away from the start. After this count the line expires automatically.
  - For ray lines the value is ignored because the line extends indefinitely.
  - For vertical lines it specifies how many bars to wait before firing. The minimum accepted value is `1`.
- **Ray** – `true` keeps the line active indefinitely to the right, `false` restricts it to the specified length.

Example:

```
TrendLine|Trend|Buy|1.1000|0.0005|8|false;HorizontalSell|Horizontal|Sell|1.1050|0|0|true;VerticalImpulse|Vertical|Buy|0|0|1|false
```

The example creates a rising buy trend line, a horizontal sell level that never expires and a one-off vertical trigger for the next candle.

## Parameters
- **Candle Type** – market data type used for calculations. Defaults to 1-minute time frame.
- **Trade Volume** – order size for new entries. Must be positive.
- **Direction Mode** – determines how the entry side is selected (`FromLabel`, `ForceBuy`, `ForceSell`).
- **Buy Label** / **Sell Label** – label values for identifying lines when `Direction Mode` is `FromLabel`.
- **Line Definitions** – raw string that describes every synthetic line (see format above).
- **Stop Loss Offset** – distance in price units for protective exits on long and short positions (0 disables the check).
- **Take Profit Offset** – price distance for profit targets (0 disables the check).

## Risk management
The strategy does not place separate stop or take profit orders. Instead it monitors every finished candle:
- Long positions close if the candle low breaches `EntryPrice - StopLossOffset` or the high exceeds `EntryPrice + TakeProfitOffset`.
- Short positions close if the candle high breaches `EntryPrice + StopLossOffset` or the low goes below `EntryPrice - TakeProfitOffset`.

If both offsets are zero the position will only be closed by the opposite signal or manual intervention.

## Implementation notes
- All comments in the source code are in English to keep consistency with the project guidelines.
- The strategy ignores invalid line definitions silently; ensure the format is correct to avoid missing triggers.
- Re-starting the strategy clears the internal state, so line counters and activation timers begin again from the first processed candle.
- The approach focuses on candle open prices just like the original EA and will not react to intrabar touches.

## Usage
1. Configure the trading security and desired candle type.
2. Adjust **Line Definitions** to describe every manual line you want to trade against.
3. Set **Direction Mode** to either rely on labels or to force one-sided trading.
4. Optionally set stop-loss and take-profit offsets for automatic exits.
5. Start the strategy and monitor the logs: each triggered line is reported together with its direction and activation price.
