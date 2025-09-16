# ColorJFatl Digit NN3 MMRec Strategy (StockSharp Conversion)

This strategy is a StockSharp high-level port of the MetaTrader 5 expert *Exp_ColorJFatl_Digit_NN3_MMRec*. The original robot used a custom ColorJFatl_Digit indicator together with money-management recovery rules. The StockSharp version focuses on the core signal engine and expresses it through three independent modules working on different timeframes.

Each module applies a Jurik Moving Average (JMA) to the selected price source and monitors the slope of that average. When the slope turns positive the module treats it as a bullish regime, closes short exposure and optionally opens a new long position. When the slope turns negative the module performs the mirror logic for short trades. All modules share the same portfolio and therefore always work with the net position of the strategy.

## Trading Logic

1. Subscribe to candles on three timeframes (defaults: 1 day, 8 hours, 3 hours).
2. For every finished candle:
   - Convert the candle to the configured applied price (close, open, typical price, DeMark price, etc.).
   - Process the value through a Jurik Moving Average to obtain a smoothed series.
   - Compare the current JMA value with the previous one to determine the slope direction. A positive slope yields an "up" state, a negative slope yields a "down" state, a flat slope keeps the previous state.
   - Buffer the states according to the *SignalBar* delay so that the strategy can act on historical bars if desired (the original expert supported delayed signals).
3. Whenever a module detects a transition:
   - **Up transition**: optionally close any short position and open a long position with the module volume.
   - **Down transition**: optionally close any long position and open a short position with the module volume.
4. Opposite signals from another module can flatten or reverse the position depending on the enable flags.

Stops and profits are not hard-coded; instead, the strategy relies on opposite signals and the built-in StockSharp protections (`StartProtection()`) for safety.

## Parameters

The parameters are grouped per module (A, B, C) to mirror the MT5 template. Each group exposes the following values:

- **CandleType** – timeframe for incoming candles.
- **JmaLength** – period of the Jurik Moving Average.
- **JmaPhase** – stored for documentation; StockSharp's JMA does not expose phase adjustment.
- **SignalBar** – number of finished bars to wait before acting on a signal (0 = immediate).
- **AppliedPrice** – price transformation used as input for JMA (close, open, median, typical, weighted, simple, quarter, trend-follow, DeMark).
- **AllowBuyOpen / AllowSellOpen** – permission to open positions in the corresponding direction.
- **AllowBuyClose / AllowSellClose** – permission to close existing positions when the module issues an opposite signal.
- **Volume** – order size the module uses when opening a new trade.

Because modules share a single account position, only one net long or net short position can exist at a time. If a module attempts to open a trade while the portfolio already carries exposure in the same direction, the order is skipped; if an opposite direction is open, it is closed before the new trade is placed (subject to the permission flags).

## Usage Notes

- The strategy subscribes to all configured timeframes through `GetWorkingSecurities()`, ensuring the simulation or live environment loads the required candle series.
- Signals operate strictly on finished candles to prevent intrabar repainting.
- The *AppliedPrice* enum reproduces the options from the original indicator, including two trend-follow price variants and the DeMark price.
- Money-management recovery logic from the MQL version is not reproduced. Instead, risk can be managed via StockSharp protections or by adjusting module volumes.
- English comments inside the code explain each step of the conversion for easier maintenance and future Python porting.

## Extending the Strategy

- To add stop loss or take profit rules, replace the default `StartProtection()` call with the desired configuration.
- Additional modules can be created by cloning the `SignalModule` configuration pattern.
- For advanced position management (e.g., tracking per-module exposure), StockSharp child strategies or virtual portfolios can be added on top of this foundation.
