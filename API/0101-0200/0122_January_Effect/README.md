# January Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades the January Effect across the year boundary. Enter on the first available finished candle on the first trading day on or after December 26. Close at the end of the first trading session on or after January 15, or on the first subsequent eligible candle if that closing candle is unavailable. There are no recurring monthly entry windows or short entries.

Uses finished 15-minute candles and the instrument's explicit `Security.Board` trading calendar, including holidays, special sessions and the board time zone. A missing calendar prevents startup. Only long positions are opened, with at most one entry attempt per annual season; a stop does not allow another entry that season. Pending orders block overlapping decisions.

`StopLoss = 2` configures local native protection at 2% from the actual filled entry price. Bid/ask updates of the execution instrument can trigger a market exit between candles. No take-profit is configured.
