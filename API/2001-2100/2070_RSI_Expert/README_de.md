# RSI-Experte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie **RSI-Experte** handelt mit dem Relative Strength Index (RSI). Sie wartet darauf, dass der RSI-Wert vordefinierte Überkauft- oder Überverkauft-Levels kreuzt, und eröffnet Positionen in Richtung der Kreuzung.

## Logik

- RSI für jede Kerze berechnen.
- Wenn der RSI **über** das Überverkauft-Level kreuzt, wird eine Long-Position eröffnet.
- Wenn der RSI **unter** das Überkauft-Level kreuzt, wird eine Short-Position eröffnet.
- Vor dem Eröffnen einer neuen Position wird die entgegengesetzte geschlossen.
- Optionale Schutzmaßnahmen für Take‑Profit, Stop‑Loss und Trailing Stop können aktiviert werden.

Die Strategie verarbeitet nur **abgeschlossene Kerzen** und verwendet StockSharp's High‑Level‑API mit Indikator-Bindung.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `RsiPeriod` | RSI-Berechnungsperiode. | `14` |
| `LevelUp` | Überkauft-Level zum Auslösen von Shorts. | `70` |
| `LevelDown` | Überverkauft-Level zum Auslösen von Longs. | `30` |
| `TakeProfitPercent` | Take-Profit-Prozentsatz. `0` deaktiviert. | `0` |
| `StopLossPercent` | Stop-Loss-Prozentsatz. `0` deaktiviert. | `0` |
| `TrailingStopPercent` | Trailing-Stop-Prozentsatz. `0` deaktiviert. | `0` |
| `CandleType` | Kerzen-Zeitrahmen für Berechnungen. | `1 Minute` |

## Hinweise

Der Trailing Stop verwendet den integrierten `StartProtection`-Mechanismus. Wenn `TrailingStopPercent` größer als null ist, ersetzt er den regulären Stop-Loss und folgt dem Preis automatisch.
