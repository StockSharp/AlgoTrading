# Liquidity Grab Volume Trap Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy waits for a bearish liquidity grab on flat volume that forms a fair value gap. When price closes above the gap top while volume stays near its moving average, it places a limit buy at the gap bottom with a symmetrical stop loss and take profit.

## Details

- **Entry Condition**: `Close[2] < Open[1]` && `Close > High[1]` && bearish break with flat volume
- **Exit Conditions**: stop loss at gap bottom minus gap height, take profit at `High[1]`
- **Type**: Reversal
- **Indicators**: Volume SMA
- **Timeframe**: 1 minute (default)
