# Spot-Futures-Arbitrage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Arbitragiert die Preisdifferenz zwischen einem Spot-Asset und seinem Futures-Kontrakt.
Eröffnet Long Spot/Short Futures, wenn der Future das Spot um einen Schwellenwert übersteigt, und umgekehrt, wenn er darunter liegt.
Schwellenwerte können dynamisch auf Basis des Spread-Durchschnitts und der Standardabweichung sein. Trades werden geschlossen, wenn der Spread revertiert oder nach einer maximalen Haltezeit.

## Parameter
- **Spot** — Spot-Wertpapier.
- **Future** — Futures-Wertpapier.
- **CandleType** — Kerzen-Zeitrahmen.
- **MinSpreadPct** — Mindest-Spread-Prozentsatz für den Einstieg.
- **LookbackPeriod** — Periode für Spread-Statistiken.
- **AdaptiveThreshold** — dynamische Schwellenwerte aktivieren.
- **MaxHoldHours** — maximale Positionshaltedauer in Stunden.
