# DeMarker Gaining Position Volume Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor *"DeMarker gaining position volume"*. It uses the DeMarker oscillator to detect oversold and overbought extremes, gradually accumulating exposure when the market stays in a stretched condition. The implementation operates on completed candles and ensures that only one signal per bar is processed.

The C# version focuses on the core discretionary logic of the original script while adopting the high-level StockSharp API. Order management, volume growth, and optional reversal behaviour are available via strategy parameters, allowing the algorithm to be adapted to different markets and timeframes.

## Parameters
- **DeMarker Period** – number of candles used by the DeMarker indicator.
- **Upper Level** – oscillator threshold that prepares short exposure (default `0.7`).
- **Lower Level** – oscillator threshold that prepares long exposure (default `0.3`).
- **Trade Volume** – market order volume submitted on every signal.
- **Only One Position** – when enabled, the strategy flattens before opening a new trade so that net exposure never mixes longs and shorts.
- **Reverse Signals** – swaps buy and sell triggers, turning the strategy into a contrarian or trend-following version.
- **Candle Type** – timeframe of the candles used for the indicator and signal evaluation.

## Trading Logic
1. A candle subscription is opened for the selected timeframe and fed into a DeMarker indicator.
2. When the latest finished candle closes, the current DeMarker value is compared with the configured levels.
3. Without reversal:
   - If DeMarker is below the lower level, the strategy tries to build or add to a long position.
   - If DeMarker is above the upper level, the strategy tries to build or add to a short position.
4. With reversal enabled, the meaning of the levels is inverted (extreme lows trigger shorts and extreme highs trigger longs).
5. The algorithm remembers the bar time of the last executed trade to avoid multiple entries on the same candle.

## Position Management
- Before flipping direction the strategy checks the unrealised profit of the existing position. Opposite exposure is closed only if the current candle price exits the trade with a positive result, mirroring the protective behaviour of the original EA.
- Position averages are tracked internally. When additional orders are added in the same direction, the average price is recalculated to evaluate profitability correctly.
- The optional *Only One Position* parameter forces a flat state before entering a new trade, which is helpful when running in net position mode.
- `StartProtection()` is invoked once the strategy starts to ensure that emergency liquidation remains available if the position becomes non-zero and the algorithm stops.

## Notes
- The conversion is designed for the high-level StockSharp API and does not rely on any custom collections or direct indicator value polling.
- Risk sizing models from the MetaTrader version (fixed margin, percentage risk, etc.) are intentionally simplified to the constant `Trade Volume` parameter. Adjust position sizing externally if dynamic risk control is required.
- Because fills are simulated with market orders at candle close prices, remember to validate the configuration against actual broker execution and slippage requirements.
