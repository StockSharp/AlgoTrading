# MicuRobert EMA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses two Zero Lag Exponential Moving Averages (ZLEMA) to trade crossovers. It can restrict trading to a given session and optionally uses a trailing stop.

## Details

- **Entry Criteria:**
  - **Long:** fast ZLEMA crosses above slow ZLEMA, or price crosses above fast ZLEMA while fast is above slow.
  - **Short:** fast ZLEMA crosses below slow ZLEMA, or price crosses below fast ZLEMA while fast is below slow.
- **Exit Criteria:** positions close on trailing stop or fixed stop-loss and take-profit levels.
- **Stops:** optional trailing stop with fixed take-profit and stop-loss.
- **Filters:** session time filter.
