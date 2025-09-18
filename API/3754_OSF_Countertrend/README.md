# OSF Countertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy reproduces the Open Source Forex "Overbought/Oversold" countertrend expert.
It approximates the original oscillator by averaging several RSI readings and interprets
the distance from the equilibrium level (50) as both a direction and position size signal.
Trades are executed on finished candles and closed by a fixed take-profit measured in
instrument points.

## Trading Rules

- **Data**: Finished candles of the configured `CandleType`.
- **Indicator**: RSI with period defined by `RsiPeriod`. The original MQL expert averaged five
  identical RSI values, therefore a single RSI is sufficient here.
- **Signal logic**:
  - When RSI > 50, the market is considered overbought and a short position is opened.
  - When RSI < 50, the market is considered oversold and a long position is opened.
  - The absolute distance |RSI − 50| determines the traded volume through `VolumePerPoint`.
- **Cooldown**: After each trade the strategy waits for `CooldownBars` finished candles before
  evaluating a new entry. This mimics the bar smoothing behaviour from the source code.
- **Exits**: Each entry places a manual take-profit at `TakeProfitPoints` * `PriceStep` away from
  the fill price. No stop-loss is used, exactly as in the original expert.
- **Reversals**: Opening a trade in the opposite direction closes any existing position first
  by adjusting the market order volume.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `RsiPeriod` | RSI length used to approximate the OSF oscillator (default 14). |
| `VolumePerPoint` | Volume traded for each RSI point away from the 50 level (default 0.01). |
| `TakeProfitPoints` | Distance to the take-profit target expressed in instrument points (default 150). |
| `CooldownBars` | Number of finished candles to skip after each trade (default 5). |
| `CandleType` | Candle type for indicator calculations (default 1-minute time frame). |

## Notes

- The strategy assumes that `PriceStep` is defined for the selected instrument; otherwise a unit
  step of 1 is used to compute the take-profit level.
- Because the original expert had no protective stop-loss, risk management should be added
  manually when deploying the strategy live.
