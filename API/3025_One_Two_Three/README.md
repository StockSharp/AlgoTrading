# One Two Three Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The One Two Three strategy trades breakouts of the Chaikin oscillator after an extended period of flat accumulation. It emulates the original MetaTrader 5 expert by combining an accumulation/distribution line with two EMAs, validating that market pressure has stayed neutral for several bars, and then entering on a strong surge of Chaikin momentum. The StockSharp port keeps the lot sizing, stop management, and trailing logic configurable through strategy parameters.

## Concept

- Build the Chaikin oscillator as the difference between a fast and a slow exponential moving average applied to the accumulation/distribution line derived from the incoming candles.
- Track the last **BarsCount** oscillator readings and classify bars where the absolute Chaikin value stays within **FlatLevel**.
- Allow trading only when more than **FlatPercent** percent of those stored readings stayed inside the flat range, signalling quiet accumulation.
- When a new candle finishes, enter in the direction of the Chaikin surge if its magnitude exceeds **OpenLevel**.

## Entry Rules

- **Long**: The Chaikin oscillator on the just closed candle is greater than or equal to **OpenLevel** and the current net position is non-positive.
- **Short**: The Chaikin oscillator on the just closed candle is less than or equal to the negative **OpenLevel** and the current net position is non-negative.
- Orders are issued at market. If the strategy holds an opposite position, the order size is increased to flatten the existing exposure before establishing the new trade.

## Exit Rules

- A fixed stop-loss (**StopLossPips**) and take-profit (**TakeProfitPips**) are translated into price offsets using the security price step (1 pip = 1 price step) and applied immediately after entry.
- An optional trailing stop adjusts the protective stop once price moves in favour of the trade by at least **TrailingStopPips + TrailingStepPips**. The new stop is placed exactly **TrailingStopPips** away from the current close while requiring the step buffer to avoid premature tightening.
- If either the stop or the target is touched within the completed candle range, the position is closed at market.

## Risk and Money Management

- **OrderVolume** controls the quantity sent with every market order. The strategy automatically adds or subtracts the current position size when flipping direction so that reversals happen in a single trade.
- Setting any of the pip-based parameters to zero disables that component (for example, a zero take-profit keeps trades open until the stop or opposite signal occurs).

## Parameters

- **OrderVolume** – Base volume for entries.
- **StopLossPips** – Distance, in pips, between the entry price and the protective stop.
- **TakeProfitPips** – Distance, in pips, between the entry price and the profit target.
- **TrailingStopPips** – Distance, in pips, maintained between price and the trailing stop. Set to zero to disable trailing.
- **TrailingStepPips** – Minimum pip gain beyond the trailing distance required before the stop is moved again.
- **FastLength** – Period of the fast EMA in the Chaikin oscillator.
- **SlowLength** – Period of the slow EMA in the Chaikin oscillator.
- **FlatLevel** – Absolute Chaikin value that still counts as flat market behaviour.
- **OpenLevel** – Chaikin magnitude required to trigger a new trade once the flat condition is satisfied.
- **BarsCount** – Number of recent Chaikin values to evaluate when computing the flat ratio.
- **FlatPercent** – Minimum percentage of the stored values that must stay within the flat range to allow trading.
- **CandleType** – Candle data type or timeframe that feeds the indicator calculations.

## Notes

- The trailing logic mirrors the MetaTrader expert: if **TrailingStopPips** is non-zero, keep **TrailingStepPips** positive to avoid a stagnant stop.
- Because StockSharp strategies work with the security price step, the pip-based distances assume that one pip equals one price step; adjust the parameter values accordingly for instruments with different tick sizes.
- The strategy processes completed candles only and does not attempt to react intra-bar, matching the original expert that executes on new bar openings.
