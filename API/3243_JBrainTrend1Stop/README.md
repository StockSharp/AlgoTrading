# JBrainTrend1Stop Strategy

The **JBrainTrend1Stop Strategy** is a StockSharp port of the MetaTrader 5 expert advisor `Exp_JBrainTrend1Stop`. It combines two Average True Range measurements, a Stochastic oscillator and Jurik moving averages to detect BrainTrading trend reversals. Whenever the Jurik-smoothed price makes a sufficiently large swing and the Stochastic leaves its neutral zone, the strategy switches bias, updates the BrainTrend stop line and (optionally) reverses the net position after a configurable delay.

## Trading logic

1. Subscribe to candles defined by `CandleType` and feed them into:
   - A primary `AverageTrueRange` with length `AtrPeriod`.
   - An extended `AverageTrueRange` with period `AtrPeriod + StopDPeriod`.
   - A `StochasticOscillator` with `StochasticPeriod` and a single-bar %K smoothing (to match the MT5 settings).
   - Three `JurikMovingAverage` instances (high, low and close) configured with `JmaLength` and `JmaPhase`.
2. For each finished candle compute:
   - `range = ATR / 2.3` (matching the original constant `d = 2.3`).
   - `range1 = ATR_extended * 1.5` (matching `s = 1.5`).
   - `val3 = |JMA_close - JMA_close[shift 2]|` which reproduces the MT5 buffer difference.
3. When `val3 > range` and the Stochastic leaves its neutral band:
   - If `%K < 47` the strategy enters the bearish BrainTrend state (`_trendState = -1`), seeds the sell stop at `JMA_high + range1 / 4` and raises a **sell** signal.
   - If `%K > 53` the strategy enters the bullish state (`_trendState = 1`), seeds the buy stop at `JMA_low - range1 / 4` and raises a **buy** signal.
4. While the state remains unchanged the BrainTrend stop is trailed toward price by `range1` (`JMA_high + range1` for bearish trends, `JMA_low - range1` for bullish trends).
5. Signals are released after `SignalBar` completed bars. Upon execution:
   - A buy signal closes short positions (if `SellClose` is enabled) and optionally opens a new long (if `BuyOpen` is enabled).
   - A sell signal closes long positions (if `BuyClose` is enabled) and optionally opens a new short (if `SellOpen` is enabled).

Charts automatically display the Jurik-smoothed close and the Stochastic oscillator alongside trade markers.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle series processed by the strategy. | H4 (4-hour time frame) |
| `AtrPeriod` | Length of the primary ATR used for the BrainTrend trigger. | 7 |
| `StochasticPeriod` | Period for %K/%D of the Stochastic oscillator (single-bar %K smoothing). | 9 |
| `StopDPeriod` | Extra bars added to the secondary ATR period (`AtrPeriod + StopDPeriod`). | 3 |
| `JmaLength` | Jurik moving average length applied to high/low/close. | 7 |
| `JmaPhase` | Phase argument forwarded to the Jurik moving averages (clamped to [-100; 100]). | 100 |
| `SignalBar` | Number of completed bars to wait before firing a new signal. | 1 |
| `BuyOpen` / `SellOpen` | Allow entering long/short positions after a signal. | `true` |
| `BuyClose` / `SellClose` | Allow closing existing long/short positions on an opposite signal. | `true` |

Use the strategy's `Volume` property or broker configuration to control order size.

## Differences vs. the MT5 version

- The original money-management block (`MM`, `MMMode`, `Deviation_`, dynamic lot sizing) is replaced by StockSharp's standard order sizing via `Volume` and market orders. Slippage control is not reproduced.
- Absolute stop-loss and take-profit distances (`StopLoss_`, `TakeProfit_`) are not implemented. Protection can be configured manually through the hosting environment if required.
- The BrainTrend stop levels are used internally for signal timing; they are not placed as pending orders.
- The Jurik moving averages rely on StockSharp's `JurikMovingAverage` implementation. The phase parameter is applied through reflection, matching the behaviour of other BrainTrading ports in this repository.

## Usage

1. Attach the strategy to a security and set `CandleType` (e.g., 4-hour candles for consistency with the EA).
2. Tune the indicator parameters (`AtrPeriod`, `StochasticPeriod`, `StopDPeriod`, `JmaLength`, `JmaPhase`) to align with the desired BrainTrend sensitivity.
3. Adjust `SignalBar` to delay signal execution by several completed bars if needed.
4. Configure `Volume` and the open/close toggles to reflect the preferred trading direction.
5. (Optional) Add external risk management such as stop-loss or portfolio limits via the hosting platform.

Once running, the strategy will track BrainTrend reversals, close opposing positions and optionally flip the direction after the configured delay.
