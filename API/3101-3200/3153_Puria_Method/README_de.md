# Puria Method-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Puria Method-Strategie ist ein Trendfolge-System, das ursprünglich für MetaTrader entwickelt wurde. Es kombiniert drei gleitende Durchschnitte mit einem MACD-Trendfilter, um Momentum-Ausbrüche zu erkennen. Die StockSharp-Konvertierung behält die ursprüngliche Einstiegslogik bei und fügt moderne Risikokontrollen hinzu, wie partielle Gewinnmitnahmen und automatisierte Trailing-Stops.

## Handelslogik
- Berechnung von drei gleitenden Durchschnitten mit konfigurierbaren Glättungsmethoden und Preisquellen.
- Auswertung der Differenz zwischen dem langsameren Basis-MA und den beiden schnelleren MAs der vorherigen Kerze. Ein bullisches Signal erfordert, dass beide schnellen MAs mindestens 0,5 Punkte über der Basis liegen; ein bearisches Signal erfordert, dass die Basis um denselben Abstand führt.
- Bestätigung der Marktrichtung mit der MACD-Hauptlinie. Long-Trades erfordern, dass der vorherige MACD-Wert positiv ist und der jüngste MACD-Verlauf für die konfigurierte Anzahl von Kerzen nicht-fallend ist. Short-Trades erfordern die entgegengesetzten Bedingungen.
- Wenn ein Einstieg ausgelöst wird, schließt die Strategie eine entgegengesetzte Position (falls vorhanden) und öffnet eine neue Nettoposition in Signalrichtung.

## Risikomanagement
- **Stop Loss / Take Profit:** Preise werden vom Einstieg aus mit Pip-Abständen berechnet und auf den Kursschritt des Instruments normiert.
- **Trailing Stop:** Sobald die Position über den Trailing-Schwellenwert plus Schritt hinausgeht, wird der Stop bei jedem weiteren Trailing-Schritt nachgezogen.
- **Teilausstieg:** Nachdem der Preis einen minimalen Gewinnabstand zurückgelegt hat, wird ein konfigurierbarer Anteil der Position geschlossen, um Gewinne zu sichern.
- **Positionsverwaltung:** Der Algorithmus verfolgt den höchsten (Long) oder niedrigsten (Short) Preis nach dem Einstieg, um Stop- oder Gewinnregeln auszulösen, wenn Kerzen diese Niveaus durchbrechen.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `StopLossPips` | Stop-Loss-Abstand in Pips. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. |
| `TrailingStepPips` | Minimaler Gewinnvorschub, bevor der Trailing-Stop aktualisiert wird. |
| `MinProfitStepPips` | Minimaler Abstand in Pips vor der Teilgewinnmitnahme. |
| `MinProfitFraction` | Anteil der Position, der bei Erreichen des Mindestgewinnschritts geschlossen wird. |
| `CandleType` | Primäre Kerzenserie, die von der Strategie verwendet wird. |
| `Ma0Period`, `Ma1Period`, `Ma2Period` | Perioden für die drei gleitenden Durchschnitte. |
| `Ma0Shift`, `Ma1Shift`, `Ma2Shift` | Optionale Kerzenverschiebungen für jeden gleitenden Durchschnitt. |
| `Ma0Method`, `Ma1Method`, `Ma2Method` | Glättungsmethoden für gleitende Durchschnitte (einfach, exponentiell, geglättet, linear gewichtet). |
| `Ma0Price`, `Ma1Price`, `Ma2Price` | Kerzenpreisquellen für die gleitenden Durchschnitte. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD-Konfiguration. |
| `MacdTrendBars` | Anzahl der Kerzen zur Verifikation des monotonen MACD-Trends (mindestens 3). |
| `MacdPrice` | Kerzenpreisquelle für die MACD-Berechnung. |

## Hinweise
- Die Strategie verwendet die vorherige abgeschlossene Kerze für MA- und MACD-Vergleiche, um Abhängigkeit von unvollständigen Kerzendaten zu vermeiden.
- Die Pip-Größe wird automatisch aus dem Kursschritt des Instruments und der Dezimalgenauigkeit abgeleitet.
- Trailing- und Teilausstiegsfunktionen erfordern Konfigurationswerte ungleich null; andernfalls bleiben die entsprechenden Blöcke inaktiv.
- Die konvertierte Version basiert ausschließlich auf abgeschlossenen Kerzen (`CandleStates.Finished`) und sollte mit einer Kerzenserie verwendet werden, die dem ursprünglichen Chartzeitrahmen entspricht.
