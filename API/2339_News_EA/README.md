# News EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy pauses trading around scheduled economic news. It subscribes to a news feed and records times of upcoming events matching user-defined currency codes and importance levels. When the current time falls within the configured window before or after a news event, the strategy logs that trading is paused.

It demonstrates how to handle `NewsMessage` data within the StockSharp high level API and how to implement a simple news filter.

## Parameters
- **Minutes Before** – minutes to halt trading before a news event.
- **Minutes After** – minutes to halt trading after a news event.
- **Include Low** – process low impact news.
- **Include Medium** – process medium impact news.
- **Include High** – process high impact news.
- **Currencies** – comma separated list of currency codes to watch.
- **Candle Type** – type of candles used to drive time checks.

## Behavior
- Subscribes to market news for the current security.
- Records incoming news that match filters.
- During processing of candles, checks whether the current time overlaps with any stored news window.
- Logs "News time" when within a window; otherwise logs "No news".

This example does not place trades but can be extended to skip or close positions during major announcements.
