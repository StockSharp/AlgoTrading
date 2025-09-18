# Casino111 Strategy

## Overview
Casino111 is a counter-trend breakout system that originates from the MetaTrader 4 expert advisor with the same name. On every new bar the strategy compares the current open price with reference levels derived from the previous daily candle. If the open gaps beyond the daily extremes (plus configurable buffers) the algorithm immediately opens a market position in the opposite direction and relies on symmetric stop-loss / take-profit protection. The StockSharp port keeps the single-position behaviour of the original robot and adds extensive parameterization for research and optimization.

## Entry and exit logic
1. The previous daily high and low are retrieved from a dedicated daily candle subscription. Two offsets (`UpperOffsetPoints` and `LowerOffsetPoints`) expressed in MetaTrader points expand the reference channel.
2. On each finished trading candle the strategy inspects the previous and current opens:
   - When the new open jumps above the daily high plus the upper offset, a **short** position is opened (fade of the gap).
   - When the new open drops below the daily low minus the lower offset, a **long** position is opened.
3. Only one position is allowed at a time. Any active orders must be filled before a new signal is considered.
4. `StartProtection` mirrors the original fixed stop and take target, both located `BetPoints` away from the entry price (converted to price steps).

## Money management
- `UseMoneyManagement = false` keeps the trade size fixed (`BaseVolume`).
- `UseMoneyManagement = true` activates the martingale progression seen in the MT4 code:
  - After every losing or break-even trade the next order volume is multiplied by `(BetPoints * 2) / (BetPoints - spreadPoints)`.
  - Spread is estimated from the latest best bid/ask quotes gathered via the order book subscription. When no quotes are available the multiplier defaults to `2`.
  - Wins reset the position size to `BaseVolume`. All volumes are aligned to the instrument `VolumeStep` and capped by `MaxVolume`.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Allow long entries triggered by gaps below the daily channel. |
| `EnableSell` | `bool` | `true` | Allow short entries triggered by gaps above the daily channel. |
| `BetPoints` | `decimal` | `400` | Symmetric stop-loss and take-profit distance in MetaTrader points (converted to price steps for StockSharp). |
| `UpperOffsetPoints` | `decimal` | `97` | Buffer added above the previous daily high to detect bearish gap reversals. |
| `LowerOffsetPoints` | `decimal` | `77` | Buffer subtracted below the previous daily low to detect bullish gap reversals. |
| `UseMoneyManagement` | `bool` | `false` | Enable the martingale-style lot progression. |
| `MaxVolume` | `decimal` | `4` | Ceiling applied to the calculated volume when money management is active. |
| `BaseVolume` | `decimal` | `0.1` | Starting order size used after a profitable trade or when money management is disabled. |
| `CandleType` | `DataType` | `H1` | Primary timeframe used to evaluate the open-gap conditions (default is 1 hour). |
| `DailyCandleType` | `DataType` | `D1` | Candle type that supplies the previous day high/low (default is 1 day). |

## Implementation notes
- The strategy relies on StockSharp’s high-level API: `SubscribeCandles` provides both the trading and daily streams, while `SubscribeOrderBook` keeps the latest spread for the money-management multiplier.
- `StartProtection` manages both the stop-loss and take-profit legs, so every entry immediately receives symmetrical exits just like in MT4.
- English inline comments highlight each decision point for easier maintenance.
- All calculations avoid indicator history lookups; only the current candle open values are required, mirroring the `Time[0]` / `Open[0]` logic from MetaTrader.

## Usage tips
- Choose a trading timeframe that matches your study. The default one-hour candles replicate the common MT4 setup, but any `DataType` supported by StockSharp can be supplied.
- When using money management make sure that `MaxVolume` respects broker limits; the alignment helper clamps the result to `VolumeStep`, `MinVolume`, and `MaxVolume`.
- Because the system always keeps at most one position open, it pairs well with StockSharp charts that plot entry/exit markers for manual inspection.
- Test the strategy inside a replay environment before connecting it to a live venue—the gap-fading approach is aggressive and depends on reliable spreads.
