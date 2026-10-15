# Freeman ATR MA RSI Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader "Freeman"-Expert Advisor mit der High-Level-API von StockSharp. Sie schichtet mehrere Marktpositionen, während ein durch eine Steigung des gleitenden Durchschnitts gemessener Trend mit einer RSI-Bestätigung übereinstimmt. Jeder Einstiegs- und Ausstiegsabstand ist in Pips definiert und wird mit der Tick-Größe des Instruments in Preiseinheiten umgerechnet, damit das Verhalten der ursprünglichen Forex-Implementierung entspricht.

## Handelslogik
1. Eine einzelne Kerzenserie (konfigurierbarer Zeitrahmen) abonnieren und die ATR-, gleitenden Durchschnitts- und RSI-Indikatoren bei jeder abgeschlossenen Kerze aktualisieren.
2. Ein direktionales Signal erzeugen wenn:
   - Die Steigung des gleitenden Durchschnitts positiv oder negativ ist, indem der neueste Wert mit dem vorherigen Bar verglichen wird (optionaler Trendfilter).
   - Der Preis weit genug vom gleitenden Durchschnitt entfernt ist, um Einstiege direkt auf der Linie zu vermeiden.
   - Der RSI den oberen oder unteren Schwellenwert kreuzt, wenn der RSI-Filter aktiviert ist. Die MetaTrader-Logik bleibt intakt, einschließlich der Eigenheit, wo eine RSI-Verkaufsbestätigung `-11` zurückgibt, daher bevorzugen beide aktivierten Filter nur Long-Trades.
3. Die maximale Anzahl gleichzeitig offener Positionen respektieren. Zusätzliche Einstiege in dieselbe Richtung sind nur erlaubt, wenn der Preis um die konfigurierte Pip-Distanz gegen die letzte Füllung bewegt hat, wodurch effektiv ein Grid aufgebaut wird.
4. Jeder Einstieg verwendet ATR-basierte Stop-Loss- und Take-Profit-Niveaus. Trailing Stops verschärfen den schützenden Stop, sobald sich der Preis um den Trailing-Schritt plus Trailing-Stop-Distanz bewegt.
5. Ausstiege werden über entgegengesetzte Marktorders ausgeführt, wenn der Kerzenbereich das Stop-, Ziel- oder Trailing-Niveau erreicht.

## Risikomanagement
- ATR-Multiplikatoren steuern die festen Stop-Loss- und Take-Profit-Abstände. Einen Multiplikator auf null setzen deaktiviert diesen Schutz.
- Trailing Stops sind optional und werden durch zwei Pip-Parameter definiert: die tatsächliche Trailing-Distanz und der zusätzliche Schritt, der erforderlich ist, bevor der Stop erneut bewegt wird.
- Die Strategie verlässt sich auf die Basis-`Volume`-Eigenschaft für die Größenbestimmung; kein automatisiertes Geldmanagement wird über das Positionslimit hinaus angewendet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für Indikatorberechnungen. |
| `MaxPositions` | Maximale Anzahl gleichzeitig offener Positionen (Summe von Long und Short). |
| `DistancePips` | Mindest-Pip-Abstand zwischen aufeinanderfolgenden Einstiegen in dieselbe Richtung. |
| `AtrPeriod` | Mittelungsperiode für den ATR-Indikator. |
| `AtrStopLossMultiplier` | ATR-Multiplikator für den Schutz-Stop. `0` deaktiviert den Stop. |
| `AtrTakeProfitMultiplier` | ATR-Multiplikator für das Gewinnziel. `0` deaktiviert das Ziel. |
| `UseTrendFilter` | Aktiviert den Filter für die Steigung des gleitenden Durchschnitts. |
| `DistanceFromMaPips` | Mindest-Pip-Abstand zwischen Preis und gleitendem Durchschnitt bei aktivem Trendfilter. |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Gleitende-Durchschnitts-Parameter entsprechend den MetaTrader-Eingaben. |
| `UseRsiFilter` | Aktiviert den RSI-Bestätigungsfilter. |
| `RsiLevelUp`, `RsiLevelDown`, `RsiPeriod`, `RsiPriceType` | RSI-Konfiguration mit Preisquellenauswahl. |
| `TrailingStopPips`, `TrailingStepPips` | Trailing-Stop-Distanz und Schritt in Pips gemessen. |
| `CurrentBarOffset` | Versatz beim Lesen von Indikatorwerten, emuliert die `CurrentBar`-Eingabe des Expert Advisors. |

## Hinweise
- Die Pip-Umrechnung multipliziert den Instrument-`PriceStep` mit 10, wenn das Instrument 3 oder 5 Dezimalstellen hat, um die Punkt-zu-Pip-Anpassung von MetaTrader zu reproduzieren.
- Die Strategie verwendet ein Netting-Positionsmodell; entgegengesetzte Signale schließen bestehende Positionen, bevor Trades in der neuen Richtung eröffnet werden.
- Der Startschutz wird beim Start aktiviert, um gegen unerwartete Wiederverbindungen zu schützen, bevor Trades platziert werden.
