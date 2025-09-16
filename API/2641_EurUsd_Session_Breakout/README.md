# EURUSD Session Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the classic EUR/USD breakout idea where a narrow European morning range is used as a springboard for the
US session. It monitors a rolling 24-candle window (15-minute candles by default) to measure the pre-US trading range, filters out
days where the range exceeds a configurable pip threshold, and then trades breakouts that occur fully outside of that band.
Only one long and one short attempt are allowed per trading day.

## How It Works

1. **Session tracking** – at the start of the configured US session hour the strategy locks the EU range captured by the 24 most
   recent completed candles (excluding the current bar). The range is adjusted to pip values automatically for 3- or 5-digit
   forex quotes.
2. **Range filter** – trading is enabled only if the captured EU range is smaller than the *Small EU Session (pips)* threshold.
3. **Breakout validation** – during the allowed US session hours, and only between `(EU start hour + 5)` and `(EU start hour + 10)`,
   the strategy looks for candles whose entire body traded outside of the stored range with an additional buffer measured in points.
4. **Order execution** – a market buy is sent when the bar’s low stays above the top of the range plus buffer. A market sell is
   sent when the bar’s high stays below the bottom of the range minus buffer. Long and short trades are independent flags so each
   direction can be attempted once per day.
5. **Risk management** – stop loss and take profit levels are defined in pips, converted to absolute price distances, and tracked
   on every finished candle using high/low extremes.

## Parameters

- **EU Session Start / US Session Start / US Session End** – hours (0–23) defining when the EU monitoring begins and when the US
  breakout window is open.
- **Small EU Session (pips)** – maximum size of the EU range that still allows trading.
- **Trade On Monday** – enables or disables Monday trading while still blocking weekends.
- **Stop Loss (pips)** – distance between entry price and protective stop, automatically scaled by tick size and digits.
- **Take Profit (pips)** – profit target distance, handled in the same way as the stop.
- **Breakout Buffer (points)** – number of price steps added to the breakout trigger so that the confirming bar must be entirely
  beyond the stored range.
- **Candle Type** – data type for candle subscription; defaults to 15-minute time frame because the original script was designed
  for M15 charts.

## Additional Notes

- The strategy assumes netting accounts: protective levels flatten the whole position using market orders.
- Daily state is reset at midnight so the range and breakout flags do not leak across sessions, while open positions retain their
  price targets.
- Because stop-loss and take-profit levels are simulated with candle extremes, intrabar spikes that do not appear in historical
  bars will not be detected.
