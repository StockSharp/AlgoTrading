# AIS3 Trading Robot Template

## Overview
The AIS3 Trading Robot Template is a MetaTrader breakout system that relies on two coordinated timeframes. The primary timeframe
captures the structure of the previous candle, while a secondary timeframe gauges recent volatility to control trailing updates.
This StockSharp port faithfully reproduces the original order sizing, entry checks, and trailing logic, but it is implemented on
top of the high-level strategy API so it can run inside Designer, Shell, or any custom StockSharp host.

## Trading Workflow
- **Market data subscriptions**: the strategy subscribes to two candle series. The primary series (default 15 minutes) provides
the previous candle high, low, close, midpoint, and range. The secondary series (default 1 minute) measures the fast range used
for trailing stops. A live order book feed keeps the current best bid/ask prices in sync with the original MQL `MarketInfo`
requests.
- **Breakout validation**:
  - A long setup triggers when the previous close is above the midpoint and the current ask price breaks out above the previous
    high plus the measured spread. The entry price is the current ask.
  - A short setup requires the previous close to stay below the midpoint and the bid to pierce the previous low. The entry price
    is the current bid.
  - Both directions inherit the broker safety checks from the template: the distance between entry and the projected stop/target
    must exceed the configured stop buffer, and the stop must remain on the correct side of the entry price even after adding the
    spread.
- **Protective orders**:
  - Stop-loss distance equals `primaryRange × StopMultiplier` and is anchored above (for longs) or below (for shorts) the
    breakout candle as described in the integration manual.
  - Take-profit distance equals `primaryRange × TakeMultiplier` and is placed from the entry price in the trade direction.
- **Trade management**:
  - When a position is open, the secondary timeframe range multiplied by `TrailMultiplier` defines the trailing distance.
  - The trailing stop is only updated if the trade is in profit, the new level is farther than the configured freeze and stop
    buffers, and the distance between the current and proposed stop exceeds `TrailStepMultiplier × spread`. This mirrors the
    template requirement that the price must advance by at least one trail step before modifying the stop.
  - Positions are closed with market orders whenever the bid/ask touches the stored stop-loss or take-profit levels.

## Risk Management
- **Account reserve**: `AccountReserve` keeps a fraction of portfolio equity locked. The strategy refuses to open new positions
  if the reserved capital would fall below the requested order budget. This matches the template behaviour where the risk
  reserve shields the account from cascading losses.
- **Order reserve**: `OrderReserve` controls the portion of the remaining capital that may be risked per trade. The position size
  is calculated as `riskBudget / |entry - stop|` and then aligned to the security volume step. If no portfolio metrics are
  available, the fallback `BaseVolume` parameter is used instead.
- **Stop & freeze buffers**: `StopBufferTicks` and `FreezeBufferTicks` translate broker stop limitations (e.g. `MODE_STOPLEVEL`
and `MODE_FREEZELEVEL` from MetaTrader) into price units using the security price step. They prevent the strategy from issuing
orders that would violate exchange constraints or from moving the trailing stop too aggressively.
- **Trailing step multiplier**: `TrailStepMultiplier` mirrors the `acd.TrailStepping` constant from the MQ4 template. It ensures
  that trailing updates only happen when the new stop is at least one spread-multiple away from the previous value.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `AccountReserve` | Fraction of equity kept as safety reserve (0–0.95).
| `OrderReserve` | Fraction of tradable equity allocated to the risk budget per trade (0–0.5 by default).
| `PrimaryCandleType` | Working timeframe for breakout detection (default 15-minute candles).
| `SecondaryCandleType` | Faster timeframe that controls trailing distance (default 1-minute candles).
| `TakeMultiplier` | Multiplier of the primary range used to place the take-profit order.
| `StopMultiplier` | Multiplier of the primary range used to compute the protective stop.
| `TrailMultiplier` | Multiplier of the secondary range defining the trailing distance.
| `BaseVolume` | Fallback position size when portfolio metrics are unavailable.
| `StopBufferTicks` | Extra distance, in price ticks, that must remain between entry and stop/target levels.
| `FreezeBufferTicks` | Additional buffer that avoids stop updates too close to the broker freeze level.
| `TrailStepMultiplier` | Spread multiplier that defines the minimal increment between trailing adjustments.

## Usage Notes
- Feed the strategy with both candle series and a level-1 or order book stream so the best bid/ask prices are available. Running
  it on last-trade data only will alter the breakout checks because they rely on the spread.
- The default parameter values replicate the MQ4 template example (`TakeMultiplier = 1`, `StopMultiplier = 2`,
  `TrailMultiplier = 3`). Adjust them to match the assets you trade or to experiment with the breakout intensity.
- The trailing stop is virtual—orders are not modified on the exchange. When the trailing condition is met the strategy simply
  issues a market exit, mirroring how the original expert advisor managed stops internally.
- Combine the strategy with StockSharp's built-in protection module (already enabled in the constructor) to maintain emergency
  stop-loss handling even if the strategy is temporarily disconnected.
