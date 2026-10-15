# Billy Expert Pullback-Käufer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Billy Expert ist eine Long-only-Pullback-Strategie, die aus dem MetaTrader 5-Experten «Billy expert» konvertiert wurde. Sie wartet auf eine Sequenz fallender Hochs und öffnet auf dem Basis-Zeitrahmen, dann prüft sie bullische Bestätigungen von zwei Stochastik-Oszillatoren, die auf verschiedenen höheren Zeitrahmen berechnet werden. Wenn beide Oszillatoren übereinstimmen, dass Aufwärtsmomentum vorhanden ist, fügt das System eine neue Long-Position hinzu, bis zu einem konfigurierbaren Limit.

Die Konvertierung folgt den StockSharp-High-Level-API-Richtlinien. Handelsvolumen, maximale gleichzeitige Einstiege, Schutz-Stops und Take-Profits werden durch Strategieparameter gesteuert, sodass das Verhalten der ursprünglichen MQL-Logik entspricht.

## Funktionsweise
1. Abonnieren der primären Kerzenserie (Standard 1 Minute) und zwei höherer Zeitrahmen für die Stochastik-Oszillatoren (Standard 5 und 6 Minuten).
2. Verfolgen der letzten vier abgeschlossenen Kerzen auf dem Basis-Zeitrahmen. Ein gültiger Pullback erfordert streng fallende Hochs *und* Eröffnungen über diese vier Bars.
3. Bewertung der schnellen und langsamen Stochastik-Oszillatoren. Die Strategie verlangt, dass für jeden Oszillator sowohl der aktuelle als auch der vorherige Wert von %K über %D bleibt, was signalisiert, dass das Momentum bereits auf beiden Zeitrahmen aufwärts gedreht hat.
4. Wenn der Pullback und die Momentum-Filter bestätigen und die Anzahl der offenen Long-Trades unter `MaxPositions` liegt, wird eine Kaufmarktorder mit der Größe `TradeVolume` gesendet.
5. Optionale Stop-Loss- und Take-Profit-Levels, in Pips ausgedrückt, werden über den `PriceStep` des Instruments in absolute Preisabstände umgerechnet. Wenn eine Distanz auf null gesetzt wird, wird die entsprechende Schutzorder weggelassen.
6. Positionen werden nur über diese Schutzlevels geschlossen, was das ursprüngliche Expertenverhalten nachahmt.

## Parameter
- `TradeVolume` – Ordergröße für jeden Einstieg (Standard `0.01`).
- `StopLossPips` – Stop-Distanz in Pips (Standard `0`, deaktiviert).
- `TakeProfitPips` – Gewinnziel in Pips (Standard `32`).
- `MaxPositions` – maximale gleichzeitige Long-Trades (Standard `6`).
- `Signal Candle` – Basis-Zeitrahmen für Preismuster (Standard `1` Minute).
- `Fast Stochastic TF` – Zeitrahmen für den schnellen Oszillator (Standard `5` Minuten).
- `Slow Stochastic TF` – Zeitrahmen für den langsamen Oszillator (Standard `6` Minuten). Muss länger als der schnelle Zeitrahmen sein.

## Filter und Verhalten
- **Richtung**: Nur Long.
- **Einstiegsauslöser**: Vier-Bar-Pullback mit fallenden Eröffnungen und Hochs.
- **Momentum-Filter**: Doppelter Stochastik-Oszillator mit %K über %D bei aktuellen und vorherigen Werten.
- **Risikomanagement**: Optionaler pip-basierter Stop-Loss und Take-Profit. Keine Trailing-Logik.
- **Positionsgröße**: Festes `TradeVolume` pro Einstieg, begrenzt durch `MaxPositions`.
- **Märkte**: Entwickelt für Forex-Paare mit fraktionalen Pips, funktioniert aber mit jedem Instrument mit einem gültigen `PriceStep`.

## Verwendungshinweise
- Stellen Sie sicher, dass `Fast Stochastic TF` streng kürzer als `Slow Stochastic TF` ist, sonst stoppt die Strategie beim Start.
- Da Ausstiege ausschließlich auf Schutzorders beruhen, passen Sie `StopLossPips` und `TakeProfitPips` an die Volatilität des Instruments an.
- Die Strategie ignoriert bärische Signale und skaliert nicht aus; verwenden Sie Portfolio-Level-Risikokontrollen für zusätzlichen Schutz.
- Für Backtesting stellen Sie genügend Aufwärmkerzen bereit, damit beide Stochastik-Oszillatoren vor dem ersten Trade gebildet werden können.
