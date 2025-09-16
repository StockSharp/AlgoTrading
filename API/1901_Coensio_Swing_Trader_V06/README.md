# Coensio Swing Trader V06 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the breakout logic of the original Coensio Swing Trader. It uses the Donchian Channel to define dynamic support and resistance. A trade is opened when price breaks above the upper band or below the lower band by a configurable threshold.

## Details

- **Entry**:
  - **Long**: Close price breaks above the Donchian upper band + `Entry Threshold` pips.
  - **Short**: Close price breaks below the Donchian lower band - `Entry Threshold` pips.
- **Exits**:
  - Fixed `Stop Loss` and `Take Profit` in pips measured from the entry price.
  - Optional move to break-even after `Break Even` pips of profit.
  - Optional trailing stop that follows price by `Trailing Step` pips after break-even.
- **Stops**: Stop-loss, take-profit, break-even, trailing stop.
- **Default Values**:
  - `Channel Period` = 20
  - `Entry Threshold` = 15 pips
  - `Stop Loss` = 50 pips
  - `Take Profit` = 80 pips
  - `Break Even` = 25 pips
  - `Trailing Step` = 5 pips
  - `Enable Trailing` = false
  - `Candle Type` = 15 minute candles
