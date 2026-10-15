# AIS1 EURUSD Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den ursprünglichen AIS1 "A System: EURUSD Daily Metrics" Expert Advisor unter Verwendung von StockSharp's High-Level-API. Sie handelt EURUSD-Ausbrüche, indem sie die aktuelle Kursbewegung mit der Range des Vortages vergleicht und Trades mit adaptiver Positionsgröße plus einem Vier-Stunden-Trailing-Stop verwaltet.

## Strategieüberblick

- **Markt**: EURUSD Spot/CFD/Forex-Instrumente.
- **Primärer Zeitrahmen**: Tageskerzen liefern das Referenzhoch, -tief und den Schlusskurs.
- **Sekundärer Zeitrahmen**: 4-Stunden-Kerzen steuern Trailing-Stop-Updates und Einstiegsprüfungen.
- **Richtung**: Long- und Short-Trades sind erlaubt.
- **Stil**: Ausbruchsfortsetzung mit volatilitätsskalierten Zielen und Stops.

## Handelslogik

1. Die vorherige abgeschlossene Tageskerze verfolgen. Mittelpunkt, Range und abgeleitete Stop-/Take-Abstände mit konfigurierbaren Multiplikatoren (`StopFactor`, `TakeFactor`) berechnen.
2. Jede abgeschlossene 4-Stunden-Kerze auswerten:
   - **Long-Einstieg**: Vorheriger Tagesschluss liegt über dem Mittelpunkt und das 4-Stunden-Hoch bricht über das vorherige Tageshoch.
   - **Short-Einstieg**: Vorheriger Tagesschluss liegt unter dem Mittelpunkt und das 4-Stunden-Tief bricht unter das vorherige Tagestief.
3. Die Positionsgröße wird aus dem aktuellen Portfolio-Eigenkapital und dem konfigurierten Risikoanteil (`OrderReserve`) ermittelt. Das Volumen wird auf Instrument-Handelsschritte gerundet.
4. Für offene Positionen wendet die Strategie drei Ebenen der Ausgangssteuerung an:
   - Fester Stop-Loss auf der gegenüberliegenden Seite der Tagesrange, skaliert mit `StopFactor`.
   - Fester Take-Profit bei einer Distanz von `TakeFactor` × Tagesrange.
   - Dynamischer Trailing-Stop unter Verwendung der vorherigen 4-Stunden-Range multipliziert mit `TrailFactor`. Der Trailing-Stop aktiviert sich erst, nachdem der Trade in Gewinn geht.
5. Eine Fünf-Sekunden-Abkühlung nach jedem Trade oder Ausstieg spiegelt das ursprüngliche EA-Verhalten wider und verhindert schnelle Modifikationen.

## Risikomanagement

- `OrderReserve` definiert den Anteil des aktuellen Eigenkapitals, der beim nächsten Trade riskiert werden kann. Wenn die berechnete Größe unter dem Instrument-Minimum liegt, wird der Trade übersprungen.
- `AccountReserve` verfolgt das Spitzen-Eigenkapital und stoppt das Öffnen oder Verwalten von Trades, sobald der Eigenkapital-Drawdown `AccountReserve - OrderReserve` überschreitet (16% mit Standard-Eingaben).
- Trailing-Ausstiege und feste Ziele stellen sicher, dass Positionen geschlossen werden, auch wenn neue Trades durch den Drawdown-Guard blockiert sind.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `AccountReserve` | Anteil des Eigenkapitals, der vom Handel ausgeschlossen ist, zur Berechnung des erlaubten Drawdowns vor der Handelspause. |
| `OrderReserve` | Eigenkapitalanteil, der pro Trade riskiert wird. Bestimmt den maximalen Verlust unter Verwendung des Stop-Abstands. |
| `TakeFactor` | Multiplikator, der auf die vorherige Tagesrange angewendet wird, um den Take-Profit-Abstand festzulegen. |
| `StopFactor` | Multiplikator, der auf die vorherige Tagesrange angewendet wird, um den Stop-Loss-Abstand festzulegen. |
| `TrailFactor` | Multiplikator, der auf die vorherige 4-Stunden-Range angewendet wird, um den Trailing-Stop zu bewegen, sobald die Position profitabel ist. |
| `EntryCandleType` | Kerzentyp (standardmäßig täglich) für Ausbruchslevels. |
| `TrailCandleType` | Kerzentyp (standardmäßig 4 Stunden) für Intraday-Auswertung und Trailing. |

## Hinweise zur Konvertierung

- Die StockSharp-Version löst Einstiege und Trailing-Updates bei abgeschlossenen 4-Stunden-Kerzen aus. Der ursprüngliche MQL Expert Advisor reagierte auf jeden Tick; die Verwendung von Kerzen hält die Logik robust innerhalb der High-Level-API.
- Stop-Loss, Take-Profit und Trailing-Ausstiege werden mit Market Orders ausgeführt, wenn die jeweiligen Preisniveaus innerhalb der verarbeiteten Kerze berührt werden.
- Margin-Prüfungen aus der MQL-Version werden durch eigenkapitalbasiertes Sizing ersetzt, um plattformneutral zu bleiben und gleichzeitig die ursprünglichen Risikobeschränkungen zu respektieren.
