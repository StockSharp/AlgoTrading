# MACD-Flächen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die MACD-Flächen-Strategie bewertet das Gleichgewicht zwischen bullischem und bärischem Momentum anhand der MACD-Hauptlinie. Für jede Kerze akkumuliert die Strategie die Summe aller positiven MACD-Werte und die absolute Summe aller negativen MACD-Werte über ein konfigurierbares Verlaufsfenster. Die dominante Seite definiert die Handelsrichtung: eine stärkere positive Fläche begünstigt Long-Positionen, während eine stärkere negative Fläche Short-Positionen bevorzugt. Ein Umkehrschalter ermöglicht den Handel gegen den erkannten Trend wenn nötig.

Die Implementierung verwendet die High-Level-API von StockSharp mit Kerzenabonnements und Indikatorbindungen. Nur abgeschlossene Kerzen werden verarbeitet, und die gesamte Handelslogik ist im `ProcessCandle`-Handler gekapselt.

## Indikatoren und Daten
- **MACD (Moving Average Convergence Divergence)** mit konfigurierbaren schnellen, langsamen und Signalperioden.
- **Kerzen** eines benutzerdefinierten Zeitrahmens (standardmäßig 30 Minuten).

## Handelsregeln
1. **Long-Einstieg** – Wenn die kumulierte positive MACD-Fläche größer ist als die kumulierte absolute negative Fläche. Bei aktiviertem Umkehrmodus wird die Bedingung invertiert.
2. **Short-Einstieg** – Wenn die kumulierte absolute negative MACD-Fläche dominiert. Der Umkehrmodus tauscht das Verhalten aus.
3. **Positionsverwaltung** – Wenn ein neues Einstiegssignal erscheint, schließt die Strategie jede entgegengesetzte Position, bevor die neue eröffnet wird, sodass nur eine einzelne gerichtete Position gehalten wird.

## Risikomanagement
- **Stop Loss** – Feste Distanz in Pips gemessen vom Einstiegspreis. Automatisch in Preiseinheiten umgerechnet mit dem Wertpapier-Preisschritt.
- **Take Profit** – Festes Gewinnziel in Pips mit denselben Umrechnungsregeln.
- **Trailing Stop** – Optionaler Trailing Stop, der aktiviert wird, sobald sich die Position um `TrailingStopPips + TrailingStepPips` in den Gewinn bewegt. Der Stop folgt dann dem Preis mit einem Abstand definiert durch `TrailingStopPips` und bewegt sich nur vorwärts, wenn der Preis um mindestens `TrailingStepPips` mehr voranschreitet. Beide Werte müssen größer als null sein, um die Trailing-Logik zu aktivieren.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Auftragsvolumen für Markteinstiege. | 1 |
| `HistoryLength` | Anzahl der gespeicherten Kerzen für den MACD-Flächenvergleich. | 60 |
| `MacdFastLength` | Schnelle EMA-Periode für den MACD. | 12 |
| `MacdSlowLength` | Langsame EMA-Periode für den MACD. | 26 |
| `MacdSignalLength` | Signal-EMA-Periode für den MACD. | 9 |
| `ReverseSignals` | Bei Aktivierung werden Long- und Short-Einstiegsbedingungen vertauscht. | false |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | 100 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | 150 |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. Auf null setzen zum Deaktivieren. | 5 |
| `TrailingStepPips` | Zusätzlicher Fortschritt erforderlich, bevor der Trailing Stop aktualisiert wird. Auf null setzen zum Deaktivieren. | 5 |
| `CandleType` | Kerzenzeitrahmen für das Abonnement. | 30-Minuten-Zeitrahmen |

## Verwendungshinweise
1. Die Strategie an ein Portfolio und ein Wertpapier anhängen, dann die Parameter für den Zielmarkt anpassen.
2. Sicherstellen, dass sowohl `TrailingStopPips` als auch `TrailingStepPips` größer als null sind, um den Trailing-Schutz zu aktivieren. Andernfalls wird Trailing ignoriert und nur Stop-Loss/Take-Profit-Niveaus sind aktiv.
3. Protokollmeldungen für Informationen über Stop-Loss-, Take-Profit- und Trailing-Ereignisse überwachen. Alle Protokolle werden auf Englisch erstellt wie erforderlich.

## Ursprüngliche Idee
Die Konvertierung basiert auf dem MetaTrader 5 "Area MACD"-Expertenberater. Die StockSharp-Version behält das Kernkonzept des Vergleichs von MACD-Flächen bei und integriert gleichzeitig Risikomanagement und Indikatorhandling durch die High-Level-API des Frameworks.
