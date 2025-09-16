# Color XMUV Time Strategy

This strategy ports the MetaTrader expert advisor **Exp_ColorXMUV_Tm** to StockSharp. It recreates the original Color XMUV smoothed
line and the time-window filter while using StockSharp's high-level trading API. The strategy follows the color of the smoothed
line: a transition to teal (rising) triggers long management while a transition to magenta (falling) drives short management.

## Core Logic
- For every finished candle the strategy builds a composite price similar to the MQL version (`(H + Close)/2` on bullish bars,
  `(L + Close)/2` on bearish bars, or `Close` for doji bars).
- The composite price is passed through the requested smoothing method. Common methods (SMA, EMA, SMMA/RMA, LWMA and Jurik) are
  implemented with StockSharp indicators. Exotic options such as T3 or VIDYA fall back to an EMA because StockSharp does not
  expose direct equivalents. The phase parameter is kept for configuration parity even when the underlying indicator ignores it.
- The Color XMUV "color" is reconstructed by comparing the latest smoothed value with the previous one. Rising slopes are mapped
  to bullish color, falling slopes to bearish color and unchanged values to neutral color.
- `SignalBar` defines how many fully-completed bars to look back when evaluating a signal (e.g. the default value of 1 means the
  logic waits for confirmation on the bar before the most recent one).
- A bullish flip (previous color not bullish, current color bullish) closes any short position and optionally opens or adds to a
  long position. A bearish flip performs the symmetric actions for short trades.
- The time filter mimics the original EA: outside the trading window the strategy immediately closes existing positions and
  ignores new entries. The filter supports overnight sessions (start time after end time).
- `StopLossPoints` and `TakeProfitPoints` are translated into absolute distances using the instrument's price step and are
  registered with `StartProtection` so that StockSharp manages exits server-side where possible.

## Risk and Position Management
- Orders are sized with the `OrderVolume` parameter. When flipping direction the strategy adds the absolute value of the current
  position so that the reversal closes the old trade and opens a new one in a single transaction.
- Optional stop-loss and take-profit are converted from point values to absolute price distances. Set either parameter to zero to
  disable the respective protection layer.
- Position exits triggered by the color flip respect the `EnableBuyExits` and `EnableSellExits` toggles, allowing independent
  control of long and short management.

## Parameters
- **Candle Type** – Candle series used for calculations (defaults to 4-hour candles).
- **Order Volume** – Base market order size.
- **Enable Long Entries / Enable Short Entries** – Allow opening positions on bullish/bearish flips.
- **Close Longs / Close Shorts** – Enable automatic exits on opposite color transitions.
- **Use Time Filter** – Restrict trading to the configured session.
- **Start Hour / Start Minute / End Hour / End Minute** – Trading session bounds. When the start is later than the end the session
  wraps over midnight.
- **Smoothing Method** – Moving-average algorithm for the Color XMUV line. Options without a native StockSharp implementation
  default to EMA and are documented above.
- **Length** – Smoothing length (must be positive).
- **Phase** – Auxiliary phase parameter retained for configuration compatibility.
- **Signal Bar** – Number of completed bars to delay the signal check. Set to zero to act on the most recent closed bar.
- **Stop Loss (pts) / Take Profit (pts)** – Offsets expressed in price points; zero disables the respective layer.

## Notes
- The MQL expert relies on external smoothing libraries. When such smoothing modes are unavailable in StockSharp (ParMA, VIDYA,
  T3) the implementation substitutes an EMA. Document these fallbacks when sharing the strategy with users.
- The strategy stores only the minimal color history required by `SignalBar`, complying with the repository guideline that
  discourages building custom data caches.
- All comments are provided in English as requested.
