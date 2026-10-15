# Automatische-Handel-mit-Bollinger-Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Bollinger Bands, RSI und den Stochastik-Oszillator, um innerhalb eines festgelegten GMT-Zeitfensters automatisch Trades zu eröffnen. Eine Short-Position wird eröffnet, wenn die vorherige Kerze oberhalb des oberen Bollinger Bands schließt, während der RSI über 75 und der Stochastik-%K über 85 liegt. Eine Long-Position wird eröffnet, wenn die Kerze unterhalb des unteren Bollinger Bands schließt, mit einem RSI unter 25 und einem Stochastik-%K unter 155. Pro Richtung ist nur eine Position erlaubt. Ein Trailing Stop in Punkten schützt offene Positionen.

## Parameter

- `OpenBuy` – Long-Positionen eröffnen aktivieren.
- `OpenSell` – Short-Positionen eröffnen aktivieren.
- `GmtTradeStart` – Handelsstartzeit in GMT (exklusiv).
- `GmtTradeStop` – Handelsendzeit in GMT (exklusiv).
- `BbPeriod` – Periode für Bollinger Bands.
- `RsiPeriod` – Periode für den RSI-Indikator.
- `StochKPeriod` – %K-Periode für den Stochastik-Oszillator.
- `StochDPeriod` – %D-Periode für den Stochastik-Oszillator.
- `StochSlowing` – Glättungsfaktor für den Stochastik-Oszillator.
- `TrailingStop` – Trailing-Stop-Abstand in Punkten.
- `CandleType` – für Berechnungen verwendeter Kerzen-Zeitrahmen.
