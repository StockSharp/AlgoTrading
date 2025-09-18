# Exp XPVT Strategy

The **Exp XPVT Strategy** is a conversion of the MetaTrader 5 expert advisor *Exp_XPVT*. The system trades crossovers between the Price and Volume Trend (PVT) indicator and a configurable moving average applied to the PVT series. When the raw PVT line drops below its smoothed variant the strategy opens long positions, while upward crosses trigger short entries. Optional stop-loss and take-profit distances emulate the original expert advisor behaviour.

## Indicator Logic
- The Price and Volume Trend accumulates volume-weighted percentage price changes using the selected applied price (close, open, median, etc.).
- A smoothing filter (SMA, EMA, smoothed MA, LWMA, Jurik, T3, VIDYA approximation, or Kaufman AMA) produces the signal line.
- A historical offset (`Signal Bar`) recreates the MT5 logic: the strategy compares the smoothed and raw values from one and two bars ago to detect crossovers and exit conditions.
- Tick or real volume can be used for weighting. If the requested volume type is missing, the strategy falls back to the other source automatically.

## Trading Rules
1. On each finished candle, compute the PVT value from the configured applied price and volume type.
2. Update the smoothing indicator and store the most recent values according to `Signal Bar`.
3. If the previous bar showed PVT above the signal line, close any short position. If, in addition, the latest stored PVT is below or equal to the signal line, open a long position (if long entries are enabled).
4. If the previous bar showed PVT below the signal line, close any long position. If, in addition, the latest stored PVT is above or equal to the signal line, open a short position (if short entries are enabled).
5. After entering a trade, optional stop-loss and take-profit orders are attached using the configured distances (expressed in price steps).
6. Money management mimics the original expert advisor: new orders use the configured base `Order Volume` and include the opposite exposure to fully reverse when switching direction.

## Parameters
- **Order Volume** – base volume for new orders and reversals.
- **Stop Loss** – distance in price steps for the protective stop (0 disables it).
- **Take Profit** – distance in price steps for the profit target (0 disables it).
- **Allow Buy Entry / Allow Sell Entry** – enable opening long or short positions.
- **Allow Buy Exit / Allow Sell Exit** – enable automatic closing of existing positions when the opposite setup appears.
- **Candle Type** – timeframe used for indicator calculations.
- **Volume Source** – choose tick or real volume for PVT weighting.
- **Smoothing Method / Length / Phase** – moving average applied to the PVT line. The phase parameter is used only by Jurik-style methods.
- **Applied Price** – price formula feeding the PVT (close, open, trend-following, DeMark, etc.).
- **Signal Bar** – historical shift (in bars) used to evaluate the crossover, reproducing the MT5 implementation.

## Notes
- The strategy processes only finished candles to ensure indicator stability.
- Jurik-style smoothing uses reflection to forward the phase parameter when the indicator exposes it.
- When neither tick nor real volume is available, the strategy falls back to zero volume, preventing spurious accumulations.
- The optional `StartProtection` call activates StockSharp's built-in position monitoring, matching the single invocation in the original expert advisor.
