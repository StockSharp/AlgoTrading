# Parabolic SAR Limit
[Русский](README_ru.md) | [中文](README_cn.md)

Parabolic SAR Limit is a direct port of the MT4 expert advisor **ytg_Parabolic_exp.mq4**. The system continuously keeps buy and sell limit orders glued to the Parabolic SAR value and lets the market pull the order into a trade. Once filled, the strategy supervises the open position and performs stop-loss or take-profit exits using candle extremes, mirroring the original MQL behaviour.

## Strategy Logic

1. The strategy subscribes to a configurable candle series (4-hour timeframe by default) and calculates the Parabolic SAR indicator with the same step and maximum values as the MT4 script.
2. On every finished candle:
   - If the SAR dot is *below* the bar's low and the best bid is at least `MinOrderDistancePoints` above the SAR price, a buy limit order is placed (or re-aligned) exactly at the SAR value.
   - If the SAR dot is *above* the bar's high and the best ask is at least `MinOrderDistancePoints` below the SAR price, a sell limit order is placed (or re-aligned) at that SAR price.
   - Only one pending order per side is maintained. When the SAR moves, the active pending order is cancelled and a new one is submitted at the updated level.
3. When a pending order is filled, the stop-loss and take-profit distances (expressed in points) are converted to absolute prices using the security price step. Those levels are stored as virtual protective boundaries.
4. Every new candle checks the recorded boundaries. If the candle range touches the stop or take level, the strategy closes the corresponding position immediately and resets the protective state.

## Parameters

- **CandleType** – timeframe for signal candles. Defaults to 4-hour candles to match the MT4 input parameter `timeframe`.
- **SarStep** – Parabolic SAR acceleration factor (`step` in MT4). Controls how quickly the SAR catches up with price.
- **SarMaximum** – maximum acceleration (`maximum` in MT4). Caps the SAR speed.
- **StopLossPoints** – distance in points between the entry price and the stop level. Set to `0` to disable.
- **TakeProfitPoints** – distance in points between the entry price and the take-profit level. Set to `0` to disable.
- **MinOrderDistancePoints** – mimics `MODE_STOPLEVEL` in MT4. Pending orders are submitted only if the market price is farther than this distance from the SAR value.
- **OrderVolume** – lots (volume) for each pending order. Align it with the instrument's `VolumeStep`.

All point-based distances are converted to prices using the instrument `PriceStep`, so the behaviour stays consistent across markets.

## Trading Behaviour

- Works in both directions simultaneously: a buy and a sell limit order can coexist if the SAR flips across price.
- Pending orders are always aligned to the latest SAR reading; stale orders are cancelled before a new one is registered.
- Stop-loss and take-profit exits are handled virtually via candle highs and lows, because high-level StockSharp strategies do not attach SL/TP directly to pending orders.
- The strategy relies on best bid/ask data when available; otherwise the candle close price is used as a fallback to evaluate distance conditions.

## Porting Notes

- `MinOrderDistancePoints` defaults to `0`, but you can set it to the broker's stop level if the trading venue enforces a minimum distance.
- Protective levels are reset automatically when the position is closed or when the pending order is cancelled, keeping the logic identical to the MT4 expert.
- Comments inside the C# code explain the high-level API usage, indicator binding, and order life-cycle for easier maintenance.

## Usage Tips

- Provide Level 1 quotes for precise distance checking; otherwise, ensure the candle close price is a good proxy for the current market price.
- Review your symbol's `PriceStep` and `VolumeStep` so that the point distances and the order volume convert to valid prices and quantities.
- Because exits are evaluated on completed candles, consider using shorter timeframes if you need finer granularity for stop-loss monitoring.
