# DCA Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy buys a fixed quote amount at regular intervals, either on a chosen weekday or after a set number of candles. It optionally closes all positions after a specified end date.

### Executable schedule and sizing

Default values: `CandleType=5m`, `CapitalToInvest=200` **quote-currency units**, `StartDate=DateTime.MinValue`, `EndDate=DateTime.MaxValue`, `CloseAllOnLastCandle=false`, `UseDayOfWeek=false`, `DayOfWeek=Wednesday`, `EveryXCandles=72`.

On a finished candle inside `StartDate <= candle date < EndDate`, in interval mode purchase after every **72 eligible finished candles**. In weekday mode purchase on the **first eligible finished candle each Wednesday**, at most once per calendar date. Compute `rawVolume = 200 / Close` and **floor** to `Security.VolumeStep`, honoring instrument minimum and maximum volume. Each purchase is a market **buy**; subsequent purchases add to the same accumulated long (no EMA signal and no shorts). At or after EndDate, stop buying; if `CloseAllOnLastCandle=true`, send one **sell** for the actual remaining position on the first eligible candle on/after that date. There is no independent price stop. A market fill can differ in quote value due to slippage. These defaults are implementation choices because the original README did not specify numerical values.



## Parameters
- Candle Type
- Capital to invest per trade
- Start date
- End date
- Close all on last candle
- Use day of week mode
- Day of week
- Every X candles
