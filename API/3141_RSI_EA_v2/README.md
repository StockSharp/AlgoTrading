# RSI EA v2 Strategy

This strategy is a StockSharp port of the MetaTrader 5 expert advisor **"RSI EA v2"**. It automates trading around Relative Strength Index (RSI) threshold crossings while mirroring the original advisor's money-management, trailing-stop, and trading window controls. By default the strategy processes one-minute candles, but any candle type can be supplied through parameters.

## Trading Logic

- **Entry conditions**
  - Long positions open when RSI rises above the configured *Buy level* after being below it on the previous finished candle, and trading hours allow new orders.
  - Short positions open when RSI falls below the configured *Sell level* after being above it previously, and the trading window is open.
  - When an opposite position already exists, the strategy sizes the new market order to both flatten the current exposure and establish the requested direction (net positions only).
- **Exit conditions**
  - Protective stop-loss and take-profit levels expressed in pips are attached as soon as a new position is detected.
  - A trailing stop mimics the original EA: it activates after price advances by *Trailing stop + Trailing step* and then moves by at least the trailing step.
  - Optional “close by signal” logic exits long positions when RSI crosses down through the sell level, and exits short positions when RSI crosses up through the buy level.
  - Stops and signals are evaluated on finished candles only, keeping behaviour deterministic in backtests.

## Risk and Trade Management

- **Stop-loss / Take-profit** – defined in pips, converted to price increments that respect the instrument’s precision (including 3/5 decimal forex symbols).
- **Trailing stop** – disabled when distance is zero. A positive trailing step is required whenever the trailing distance is non-zero.
- **Position sizing** – either a fixed volume or an automatic volume computed from risk percentage and stop distance. Risk sizing requires access to portfolio equity and valid price-step metadata.
- **Trading window** – optional daily filter defined by inclusive start and exclusive end hours (0–23). When start equals end the market is considered closed.

## Parameters

| Name | Description |
| ---- | ----------- |
| `OpenBuy` / `OpenSell` | Toggle long or short entries independently. |
| `CloseBySignal` | Enables exits on opposite RSI crosses. |
| `StopLossPips` | Stop-loss distance in pips (0 disables the stop). |
| `TakeProfitPips` | Take-profit distance in pips (0 disables the target). |
| `TrailingStopPips` | Trailing stop distance in pips. Must be zero if no trailing is desired. |
| `TrailingStepPips` | Additional progress (in pips) required before moving the trailing stop. Must be positive when trailing is active. |
| `RsiPeriod` | RSI indicator length. |
| `RsiBuyLevel` / `RsiSellLevel` | Thresholds for long and short entries/exits. |
| `UseRiskSizing` | Switch between fixed volume and risk-percentage sizing. |
| `FixedVolume` | Base order size for fixed-volume mode or fallback when risk sizing cannot be computed. |
| `RiskPercent` | Percentage of portfolio equity risked per trade. Used only when `UseRiskSizing` is true and a positive stop distance exists. |
| `UseTimeControl` | Enables the daily trading window filter. |
| `StartHour` / `EndHour` | Inclusive start and exclusive end hour (0–23) of the trading window. |
| `CandleType` | Candle data type driving indicator calculations. |

## Implementation Notes

- Uses the high-level candle subscription API with `RSI` indicator binding.
- Converts pip distances using instrument precision (`PriceStep` and `Decimals`) to match MetaTrader’s 3/5 digit logic.
- Normalises order volumes to the instrument’s volume step and bounds (min/max volume).
- Trailing logic updates internal stop references only; exits are performed with market orders when levels are breached.
- Maintains separate state for long and short positions to preserve trailing and protective levels between candles.

## Usage

1. Attach the strategy to a StockSharp connector with appropriate security and portfolio metadata.
2. Configure thresholds, pip distances, and optional time window to match the desired market.
3. Enable risk-based sizing if portfolio information is available; otherwise leave it disabled to use a fixed lot.
4. Start the strategy – it will wait for finished candles, apply the RSI logic, and manage active positions according to the configured protections.
