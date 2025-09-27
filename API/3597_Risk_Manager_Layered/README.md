# Layered Risk Protector Strategy

## Overview
The **Layered Risk Protector Strategy** is a direct conversion of the MetaTrader expert advisor "RiskManager". The algorithm continuously tracks the portfolio equity curve and adjusts market exposure using the Commodity Channel Index (CCI), Average True Range (ATR) multiples and a layered position sizing model. When risk metrics fall below configurable thresholds the strategy automatically switches into hedging mode, closes positions on profit or drawdown events, and can optionally flatten at break-even.

## Trading Logic
- **Indicator conditions** – The strategy subscribes to the primary candle series (configurable timeframe) and calculates:
  - CCI using the user-defined period. Long trades require the CCI to fall below the negative threshold and short trades require it to rise above the positive threshold.
  - ATR with a fixed period of 14 in order to derive volatility-adjusted take-profit and stop-loss distances for each opened layer.
  - A moving average of candle volumes. Trading is enabled only when the rolling average of the last 50 completed candles exceeds the previous candle volume, replicating the original "Active" filter.
- **Layered entries** – The maximum exposure is distributed across a configurable number of layers. Each new order uses the per-layer volume (`MaxVolume / Layers`). Additional entries are blocked when the relative layer usage (`Orders / Layers * 100`) exceeds the current system health.
- **Order management** – Every opened layer stores its entry price together with ATR-based stop-loss and take-profit levels. On each completed candle the high/low range is checked to decide whether any layer should be closed due to reaching its protective levels.
- **Hedging mode** – When `MultiPairTrading` is disabled and the calculated health percentage drops below `HedgeLevel`, the strategy records the current order count and starts opening opposite-side layers until the hedge ratio requirement is reached. Hedging is disabled automatically once health recovers above the threshold.
- **Equity controls** – Several protections mirror the original expert advisor:
  - Hard equity stop defined by `RiskLimit` (initial capital minus risk limit).
  - Profit target expressed as an additive offset over initial capital.
  - Rolling "close equity" level that adds `CloseProfitBuffer` to the current balance each time all positions are successfully flattened.
  - Optional break-even exit that closes all trades when equity reaches the stored break-even capital.
  - Manual "Hard Close" switch that forces a flat position immediately.

## Parameters
- `AllowLong` / `AllowShort` – Enable long or short entries respectively.
- `MaxVolume` – Total position volume allocated across all layers.
- `Layers` – Maximum number of layers that can be opened simultaneously.
- `CciLength` / `CciLevel` – Period and threshold for the CCI filter.
- `StopLossMultiple` / `TakeProfitMultiple` – ATR multipliers that define protective levels for each layer.
- `CloseProfitBuffer` – Profit added to the balance when recycling the rolling close-equity target. Also used when computing the break-even capital.
- `ManualCapital` – Overrides the initial capital used for all risk calculations (set to zero to use the live portfolio balance at startup).
- `RiskLimit` – Maximum drawdown tolerated from the initial capital.
- `ProfitTarget` – Additive profit target that pauses trading once reached.
- `MultiPairTrading` – When true, hedging is disabled even if health drops below the limit.
- `HedgeLevel` / `HedgeRatio` – Health percentage that starts hedging and ratio of additional layers required during hedge mode.
- `CloseAtBreakEven` – Enables the break-even exit logic.
- `HardClose` – Forces immediate flattening and pauses further trading while true.
- `CandleType` – Candle series used for indicator evaluation and trade decisions.

## Notes
- The strategy assumes immediate market order fills. When running on historical data the actual execution model depends on the backtesting settings in StockSharp.
- Equity and balance information is sourced from the connected portfolio (`Portfolio.CurrentValue`, `Portfolio.CurrentBalance`). Ensure the strategy portfolio is synchronized with the traded security.
- Hedging opens additional market positions on the same instrument. Verify that the broker or simulator allows opposite positions when hedging is enabled.
- Break-even tracking reuses the `CloseProfitBuffer` value just like the original MetaTrader version that operated with a "ClosePL" parameter.
