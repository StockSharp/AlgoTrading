# Santa Claus Rally Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades the year-end Santa Claus Rally. Enter on the first eligible finished candle in the last five trading sessions of December. Close at the end of the second trading session of January, or on the first subsequent eligible candle if that closing candle is unavailable.

Uses finished 15-minute candles and the instrument's explicit `Security.Board` trading calendar, including holidays, special sessions and the board time zone. A missing calendar prevents startup. Only long positions are opened, with at most one entry attempt per annual season; a stop does not allow another entry that season. Pending orders block overlapping decisions.

`StopLoss = 2` configures local native protection at 2% from the actual filled entry price. Bid/ask updates of the execution instrument can trigger a market exit between candles. No take-profit is configured.

The season is recorded when the entry is submitted. A held position keeps that season's January exit deadline even if the next available candle arrives in December of a later year; it is closed on that candle rather than rolled into a new season.
