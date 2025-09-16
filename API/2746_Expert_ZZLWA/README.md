# Expert ZZLWA Strategy

## Overview

This strategy is a StockSharp high-level port of the original **ExpertZZLWA** MetaTrader 5 expert advisor. The EA offered three distinct operating modes and optional martingale position sizing. The port keeps the structure of the original expert while adapting it to StockSharp candles and indicators:

1. **Original mode** – alternates between long and short trades on every completed bar as long as there is no open position.
2. **ZigZag Addition mode** – recreates the behaviour of the "ZigZag LW Addition" custom indicator by tracking fresh swing highs and lows through rolling highest/lowest values.
3. **Moving Average Test mode** – mirrors the smoothed MA (150) vs simple MA (10) crossover logic from the MQL code.

All modes use configurable protective stop loss and take profit offsets expressed in price points. The strategy supports optional martingale sizing where a new trade is increased by a multiplier after a realised loss, capped by a maximum volume.

## Trading Logic

### Original Mode

- Works with finished candles only.
- When no position is open, the strategy alternates between long and short market orders on each new bar.
- Stop loss and take profit are registered through the built-in `StartProtection` helper.
- Once a trade closes (either at stop or target), the opposite direction becomes active for the next bar.

### ZigZag Addition Mode

- Subscribes to the selected candle series and maintains rolling `Highest` and `Lowest` indicators.
- Detects a swing high when the candle high touches the current highest value while the previous swing direction was not upward. This recreates the buy/sell buffer signals from "ZigZag LW Addition".
- Detects a swing low when the candle low touches the rolling lowest value in the opposite manner.
- Generates a market order in the signalled direction immediately after the candle closes.

### Moving Average Test Mode

- Builds a smoothed moving average with length 150 and a simple moving average with length 10 (matching the MQL implementation).
- Produces a long signal when the smoothed MA crosses above the simple MA from the previous bar to the current bar.
- Produces a short signal when the smoothed MA crosses below the simple MA.
- Signals are processed on closed candles only.

### Martingale Handling

- After every own trade is received, the strategy tracks the net position and the average entry price.
- When a position is fully closed, the realised profit of the last trade is recorded.
- If the trade closed with a loss and martingale is enabled, the next order volume becomes `last_volume * MartingaleMultiplier` (capped by `MaximumVolume`).
- If the trade closed with profit or martingale is disabled, the strategy falls back to the base volume.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `StopLossPoints` | 600 | Distance to the protective stop in price points. |
| `TakeProfitPoints` | 700 | Distance to the take profit in price points. |
| `BaseVolume` | 0.01 | Default order size used when martingale is not applied. |
| `UseMartingale` | false | Enables martingale sizing when set to true. |
| `MartingaleMultiplier` | 2 | Multiplier applied to the last trade volume after a loss. |
| `MaximumVolume` | 10 | Maximum allowed volume for martingale sizing. |
| `Mode` | Original | Operating mode: `Original`, `ZigZagAddition`, or `MovingAverageTest`. |
| `ZigZagTerm` | LongTerm | Sensitivity preset for ZigZag mode (ShortTerm, MediumTerm, LongTerm). |
| `SlowMaPeriod` | 150 | Period of the smoothed MA used in MA Test mode. |
| `FastMaPeriod` | 10 | Period of the simple MA used in MA Test mode. |
| `CandleType` | 15-minute time frame | Candle type subscribed for processing. |

## Notes

- Stop/take offsets are multiplied by the instrument `PriceStep`, matching the `_Point` behaviour from MetaTrader.
- The strategy uses StockSharp high-level API (`SubscribeCandles` + indicator binding) exclusively.
- The ZigZag sensitivity presets map to Highest/Lowest lengths of 12 (Short), 24 (Medium), and 48 (Long). Adjust them if a different swing width is required.
- The martingale tracker relies on own trade notifications; ensure the strategy runs in an environment where fills are reported correctly.

## Conversion Differences vs MQL

- The MQL version interacted with a compiled `ZigZag LW Addition` indicator. In StockSharp we approximate the buffers using rolling highs/lows, which delivers similar signals without external binaries.
- Order placement relies on `BuyMarket` / `SellMarket` and the managed protection helper instead of manual order tickets.
- Historical lot calculation in the original expert used the terminal deal history. The port replicates this behaviour by analysing own trades in real time and storing the last closed trade volume and profit.
- Slip and magic number inputs from MQL are omitted because StockSharp does not need them for market orders in this context.

