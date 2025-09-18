# Close Agent Strategy

## Overview
Close Agent Strategy is a risk-management assistant that mirrors the MQL CloseAgent expert advisor. The strategy does not open new trades. Instead, it monitors existing positions and closes them when price stretches beyond the Bollinger Bands while the Relative Strength Index (RSI) reaches extreme zones. The tool can watch for positions created manually or by other automated strategies and optionally liquidate everything once a global profit target is achieved.

## Indicators and Data
- **Candles:** configurable timeframe (default: 5-minute) used to calculate indicators.
- **Bollinger Bands (length 21, width 2):** detects price excursions above the upper band or below the lower band.
- **Relative Strength Index (length 13):** confirms whether the market is overbought (>70) or oversold (<30).
- **Level1 data:** captures the latest bid and ask quotes to evaluate exit conditions as accurately as possible.

## Parameters
- **Close Mode (`CloseMode`):** selects which positions are eligible for closing.
  - `Manual` – only positions without this strategy identifier (manual trades or other bots).
  - `Auto` – only positions opened by this strategy instance.
  - `Both` – monitor every position on the strategy symbol.
- **Candle Type (`CandleType`):** timeframe used to calculate Bollinger Bands and RSI.
- **Operation Mode (`OperationMode`):**
  - `LiveBar` – use the latest forming candle; reacts faster but may use unfinished data.
  - `NewBar` – waits for a candle to close before generating a signal (safer but slower).
- **Close All Target (`CloseAllTarget`):** if the floating profit (`PnL`) reaches this absolute value, every monitored position is closed immediately.
- **Enable Alerts (`EnableAlerts`):** when true, logs a message every time an exit is triggered, including the realized profit estimate.

## Trading Logic
1. Subscribes to Level1 quotes and the configured candle series. Bollinger Bands and RSI are updated for each incoming candle.
2. Maintains a compact history buffer so the strategy can reference the most recent closed candle when `OperationMode` is set to `NewBar`.
3. Checks if the global profit target is reached. When `CloseAllTarget` > 0 and `PnL` exceeds the threshold, all eligible positions are flattened at market prices.
4. For each monitored position on the strategy symbol:
   - **Long positions:** closed when the best bid is above the upper Bollinger Band, RSI is above 70, and price remains above the entry average price.
   - **Short positions:** closed when the best ask is below the lower Bollinger Band, RSI is below 30, and price remains below the entry average price.
5. Uses bid/ask quotes when available; otherwise falls back to the last processed candle close to avoid missed exits.

## Usage Notes
- The strategy is designed as a protective layer and assumes positions might be opened externally.
- Because the logic acts on market exits only, the strategy should run alongside other trading systems to manage risk exposure.
- Alerts appear in the Designer log when `EnableAlerts` is active, matching the original MQL alerts.
