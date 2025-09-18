# Auto RXD v1.67 Strategy

## Overview
Auto RXD v1.67 is a rule-based strategy that emulates the MetaTrader expert advisor of the same name. The approach uses three linear perceptrons: a supervisor that decides whether to look for bullish or bearish signals, plus a dedicated perceptron for each direction. Every perceptron operates on linear weighted moving averages (LWMAs) calculated from candle close and Robbie Ruan's "weighted price" (high + low + 2 × close) inputs. The StockSharp port executes on completed candles only and uses the high-level `BindEx` data flow to keep the indicator calculations synchronized with the trading loop.

## Market Data and Indicators
- **Candles** – Default timeframe is 30-minute candles. The timeframe can be changed through the `CandleType` parameter.
- **Average True Range (ATR)** – Provides both adaptive take profit and stop loss distances when `UseAtrTargets` is enabled. ATR period is controlled by `AtrPeriod`.
- **Relative Strength Index (RSI)** – Optional filter enforcing long trades above the neutral 50 level and shorts below 50 when `UseRsiFilter` is true.
- **Commodity Channel Index (CCI)** – Optional trend filter that requires readings above +100 for longs and below -100 for shorts when `UseCciFilter` is active.
- **Moving Average Convergence Divergence (MACD)** – Optional momentum confirmation. Long entries require the MACD line above the signal line, while shorts need the MACD line below the signal line when `UseMacdFilter` is true.
- **Average Directional Index (ADX)** – Optional strength filter that checks ADX is above the configured threshold and that +DI versus -DI aligns with the desired direction when `UseAdxFilter` is enabled.

## Trading Logic
1. **Perceptron Data Preparation** – For each candle the strategy updates buffers with the latest close and weighted prices. The buffers feed LWMA snapshots, generating four lagged features separated by the configured `Step` values for short, long, and supervisor perceptrons.
2. **Supervisor Decision** – The supervisor perceptron evaluates the lagged deltas using the weight parameters `SupervisorX1…X4` and `SupervisorThreshold`. A positive score unlocks the long perceptron; a negative score unlocks the short perceptron. If the supervisor score is zero or unavailable (not enough data), the candle is skipped.
3. **Directional Specialists** – The matching perceptron (long or short) validates its own score using the same LWMA feature set and direction-specific weights (`LongX*` or `ShortX*`). A positive value triggers the next validation stage.
4. **Indicator Filters** – When `UseIndicatorFilters` is false the strategy trades solely on the perceptron signal. When true, each enabled filter (RSI, CCI, MACD, ADX) must agree with the proposed direction. Missing indicator data or failing conditions cancel the signal.
5. **Order Execution** – The strategy ensures there are no active orders, flattens any opposite exposure, and enters using market orders sized by `OrderVolume`. Entry prices default to the best quote when available, otherwise the candle close.

## Risk Management
- **Protective Orders** – After filling an entry the strategy immediately computes take profit and stop loss distances through `CalculateProtectiveDistances`. When `UseAtrTargets` is true the distances scale ATR by the configured multipliers (`AtrTakeProfitFactor`, `AtrStopLossFactor`) and by the original MQL point-based TP/SL magnitudes. If ATR targeting is disabled, fixed point distances are converted to price steps.
- **Order Management** – The helper `SetProtectiveOrders` translates raw distances into price-step counts and registers stop-loss and take-profit orders relative to the entry price. The strategy avoids duplicate orders by checking `HasActiveOrders()` before submitting new trades.
- **Start Protection** – `StartProtection()` is called once in `OnStarted`, enabling the framework’s built-in protection handling whenever the position becomes non-zero.

## Parameters
The StockSharp implementation exposes the full MQL parameter set grouped for optimization and UI clarity. Key parameters include:

### Trading
- `OrderVolume` – Lot size for new positions.
- `CandleType` – Candle data type used for binding.

### Risk
- `UseAtrTargets` – Toggle between ATR-based and fixed-point protective distances.
- `AtrPeriod`, `AtrTakeProfitFactor`, `AtrStopLossFactor` – ATR configuration for adaptive targets.
- `LongTakeProfitPoints`, `LongStopLossPoints`, `ShortTakeProfitPoints`, `ShortStopLossPoints` – Point-based TP/SL references reused by both ATR and fixed modes.

### Indicator Filters
- `UseIndicatorFilters` – Master switch for all filters.
- `UseAdxFilter`, `AdxPeriod`, `AdxThreshold` – ADX confirmation settings.
- `UseMacdFilter`, `MacdFast`, `MacdSlow`, `MacdSignal` – MACD confirmation settings.
- `UseRsiFilter`, `RsiPeriod` – RSI confirmation settings.
- `UseCciFilter`, `CciPeriod` – CCI confirmation settings.

### Perceptron Specialists
- `ShortMaPeriod`, `ShortStep`, `ShortX1…ShortX4`, `ShortThreshold` – Short perceptron configuration.
- `LongMaPeriod`, `LongStep`, `LongX1…LongX4`, `LongThreshold` – Long perceptron configuration.
- `SupervisorMaPeriod`, `SupervisorStep`, `SupervisorX1…SupervisorX4`, `SupervisorThreshold` – Supervisor perceptron configuration.

All numeric parameters mirror the MQL defaults, enabling a like-for-like behavior between the original expert advisor and this StockSharp port while exposing the configuration through the `StrategyParam` system for optimization campaigns.
