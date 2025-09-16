# Fractured Fractals Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Port of the classic MetaTrader "Fractured Fractals" expert advisor. The strategy tracks confirmed Williams fractals, places stop orders on fresh breakout levels, and trails a protective stop on the opposite fractal.

## Details

- **Source**: Converted from `MQL/20127/Fractured Fractals.mq5`.
- **Market Regime**: Breakout continuation on any instrument supported by StockSharp.
- **Order Types**: Uses stop orders for entries and protective stop orders for exits.
- **Position Sizing**: Risk-based, controlled by `MaximumRiskPercent` and the adaptive `DecreaseFactor` streak logic.
- **Default Parameters**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 10
  - `ExpirationHours` = 1 hour
  - `CandleType` = 1-hour time frame
- **Core Indicators**: Native five-bar Williams fractals calculated on the fly.
- **Strategy Type**: Long/short breakout with dynamic stop management.

## Strategy Logic

### Fractal sequence tracking

- Maintains queues of the last five candle highs and lows to mimic the `iFractals` buffer in MT5.
- Each confirmed fractal shifts three rolling slots: youngest, middle, and old. Duplicate values are ignored using the instrument price step for tolerance.
- Long signals require the newest up fractal to exceed the middle fractal; short signals require the newest down fractal to be lower than the previous one.

### Entry orders and expiration

- When no long position or pending buy stop exists, the strategy places a buy stop at the most recent up fractal with a stop loss at the latest down fractal.
- Symmetrically, short entries place a sell stop at the most recent down fractal with a protective stop at the latest up fractal.
- Pending orders inherit an expiration defined by `ExpirationHours`. If the candle time surpasses the expiry or the fractal hierarchy invalidates the setup (new lower up fractal for longs or higher down fractal for shorts), the order is cancelled.
- The bot keeps the book clean by cancelling opposite orders once a position opens.

### Protective trailing stops

- Every confirmed opposite fractal updates the protective stop order: long positions trail the newest down fractal, short positions trail the newest up fractal.
- Stops are only tightened—new levels must improve over the existing order price before a replacement occurs.
- When the position is closed, any remaining stop orders are cancelled immediately.

### Risk management and streak control

- `CalculateOrderVolume` replicates the MT5 risk calculation: risk per unit = entry price minus stop price (or vice versa for shorts).
- Target monetary risk equals `Portfolio.CurrentValue * MaximumRiskPercent / 100` with a fallback to the `Volume` property when portfolio valuation is unavailable.
- The resulting volume is normalised by lot size, volume step, minimum volume, and maximum volume constraints exposed by `Security`.
- After a losing trade the streak counter increments; profitable or flat trades reset the counter. If more than one consecutive loss occurs, the size is scaled down by `losses / DecreaseFactor`.

### Trade outcome tracking

- `OnOwnTradeReceived` aggregates fills to determine when a position cycle completes and whether it ended positive, negative, or flat.
- The streak counter and the last profitable time stamp mirror the original logic, allowing further extensions (e.g., analytics) if desired.

## Usage Notes

1. Attach the strategy to any security/portfolio pair, adjust `CandleType` to the desired resolution, and set the risk parameters according to account size.
2. Ensure the adapter/broker supports stop orders; otherwise replace the protective orders with manual exits in `UpdateTrailingStops`.
3. Because the implementation processes only finished candles, intra-bar spikes smaller than the candle resolution will not trigger orders exactly as in tick-based MT5 tests.
4. Consider enabling logging to review comment messages produced by the C# port, mirroring the original expert's feedback.
