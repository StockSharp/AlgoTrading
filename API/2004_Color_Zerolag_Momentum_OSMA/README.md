# Color Zerolag Momentum OSMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy builds a custom zero-lag momentum OSMA oscillator using five momentum calculations.
When the oscillator's value two bars ago is below the value three bars ago the trend is considered upward.
In this case short positions are closed and a new long can be opened if the most recent value is above the value two bars ago.
When the value two bars ago is above the value three bars ago the trend is downward, long positions are closed and a short may be opened if the last value is below the value two bars ago.

## Parameters

- `Smoothing1` – first smoothing factor for the slow trend.
- `Smoothing2` – second smoothing factor for the OSMA line.
- `Factor1-5` – weights applied to each momentum component.
- `MomentumPeriod1-5` – periods for the momentum indicators.
- `CandleType` – candle timeframe for calculations.
- `BuyOpen` – allow opening long positions.
- `SellOpen` – allow opening short positions.
- `BuyClose` – allow closing long positions.
- `SellClose` – allow closing short positions.

