# Candlestick Stochastic Confirmation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader Expert Advisor **Expert_CP_Stoch** inside StockSharp's high-level API. It blends Japanese candlestick reversal patterns with a %D stochastic oscillator filter to confirm entries and time exits. The system scans each completed candle, looks back three bars to detect bullish or bearish formations, and requires the stochastic signal line to be in an oversold or overbought zone before opening trades. Positions are closed whenever the opposite pattern appears or when the stochastic line crosses a configurable exit boundary.

The default configuration mirrors the original expert: %K period 33, %D period 37, slowing 30, oversold at 30, overbought at 70, and exit crossover levels at 20/80. Because StockSharp's stochastic oscillator uses high/low/close data, the behaviour corresponds to the original STO_LOWHIGH setting. Candle pattern recognition relies on the last twelve bodies (by default) to compute the average candle size used in pattern filtering.

## Details

- **Entry Criteria**:
  - **Long**: One of the bullish patterns (Three White Soldiers, Piercing Line, Morning Doji, Bullish Engulfing, Bullish Harami, Morning Star, Bullish Meeting Lines) is detected **and** the stochastic %D value on the previously closed bar is below the oversold threshold (default 30).
  - **Short**: One of the bearish patterns (Three Black Crows, Dark Cloud Cover, Evening Doji, Bearish Engulfing, Bearish Harami, Evening Star, Bearish Meeting Lines) is detected **and** the stochastic %D value on the previously closed bar is above the overbought threshold (default 70).
- **Exit Criteria**:
  - **Long**: Exit immediately when a bearish pattern appears or when %D crosses below the upper exit boundary (default 80) or below the lower boundary (default 20).
  - **Short**: Exit immediately when a bullish pattern appears or when %D crosses above the lower exit boundary (default 20) or above the upper boundary (default 80).
- **Long/Short**: Trades both directions with symmetrical rules.
- **Stops**: No fixed stop-loss/target; exits rely on pattern flips and stochastic crossings. You can add portfolio protection in the launcher if required.
- **Default Values**:
  - `Body Average Period` = 12 candles used to compute the reference body size for pattern qualification.
  - `Stochastic %K` = 33, `Stochastic %D` = 37, `Stochastic Smoothing` = 30.
  - `Oversold Threshold` = 30, `Overbought Threshold` = 70.
  - `Lower Exit Level` = 20, `Upper Exit Level` = 80.
- **Filters**:
  - Category: Pattern recognition + oscillator confirmation.
  - Direction: Long & short.
  - Indicators: Stochastic oscillator, multiple candle patterns.
  - Stops: Pattern/oscillator exits only (no mechanical stop/target).
  - Complexity: High (multi-condition pattern detection with historical averages).
  - Timeframe: Works on any timeframe; defaults to hourly candles.
  - Seasonality: None.
  - Neural networks: No.
  - Divergence: No explicit divergence; confirmation via oscillator levels.
  - Risk level: Medium-high due to absence of hard stops.

## How It Works

1. Subscribes to the selected candle series and binds a stochastic oscillator (%K, %D, slowing).
2. Keeps the last three completed candles and rolling averages of candle bodies/closes to replicate MetaTrader's pattern library logic.
3. Evaluates bullish/bearish pattern groups on every finished candle. Each pattern strictly follows the original mathematical definitions (averaged body checks, midpoint relationships, gap requirements, etc.).
4. Retrieves the stochastic %D values from the two previous candles to detect oversold/overbought conditions and crossovers.
5. Opens or closes market positions using StockSharp's high-level `BuyMarket`/`SellMarket` methods when both the pattern and oscillator conditions align.
6. Optionally, you can enable external risk modules (e.g., `StartProtection`) from the launcher if you need stop-loss management.

## Practical Notes

- Ensure you feed the strategy with at least `Body Average Period + 3` historical candles before expecting signals; otherwise, pattern checks will return false because the average body is undefined.
- The stochastic filter uses the **previous** candle's %D value, replicating the way MetaTrader's signal evaluated `StochSignal(1)`.
- Because candle pattern recognition is sensitive to gaps and relative candle sizes, the results can vary on instruments with thin liquidity or synthetic data.
- To speed up optimisation, you can fine-tune the oversold/overbought thresholds and the stochastic periods while keeping the candlestick definitions intact.
- If you require STO_CLOSECLOSE behaviour (close/close stochastic), replace StockSharp's oscillator with one configured for close-only calculations in a future enhancement.
