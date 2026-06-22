# NUp1Down-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **NUp1Down-Strategie** ist eine direkte Konvertierung des MetaTrader 5-Experten "N bars up, then one bar down" (Datei `NUp1Down.mq5`). Sie scannt abgeschlossene Kerzen von StockSharp und eröffnet eine Short-Position, wenn eine Baisse-Kerze nach einer konfigurierbaren Sequenz von Hausse-Kerzen erscheint, die immer höhere Schlusspreise erzielen. Die Strategie ist für diskretionäre Trader konzipiert, die ein klassisches Swing-Umkehrmuster innerhalb von StockSharp Designer, Shell oder Runner automatisieren möchten.

## Handelslogik
1. Nur auf abgeschlossenen Kerzen arbeiten, die durch den Parameter `CandleType` bereitgestellt werden.
2. Die letzten `BarsCount + 1` Kerzen im Speicher halten. Die neueste Kerze muss unter ihrer Eröffnung schließen (bärische Setup-Kerze).
3. Die vorherigen `BarsCount` Kerzen müssen alle über ihren Eröffnungen schließen. Jede dieser Hausse-Kerzen (außer der ältesten) muss auch über dem Schlusskurs der unmittelbar vorangegangenen Kerze schließen, was eine "Treppenstufen"-Bewegung nach oben erzwingt.
4. Wenn das Muster validiert und keine aktive Short-Position vorhanden ist, sendet die Strategie eine Marktverkaufsorder.
5. Die Positionsgrößenbestimmung verwendet den Parameter `RiskPercent`. Der Algorithmus schätzt, wie viele Kontrakte eröffnet werden können, so dass das Risikokapital (Abstand zum Stop-Loss in Geldwert umgerechnet) den gewählten Prozentsatz des Portfolios nicht überschreitet. Die Basis-`Volume`-Eigenschaft bleibt die Mindestlotgröße und das Risikomodell kann die Handelsgröße nur erhöhen.

## Positionsverwaltung
- Beim Einstieg werden ein schützender Stop-Loss und ein Take-Profit-Level vom Einstiegspreis berechnet. Beide Abstände werden in Pips ausgedrückt und mit dem `PriceStep` des Instruments in Preise übersetzt. Für Symbole mit drei oder fünf Dezimalstellen wird die Pip-Größe automatisch angepasst, um der Pip-Definition von MetaTrader zu entsprechen.
- Ein Trailing-Stop wird bei jeder abgeschlossenen Kerze neu berechnet. Der Trailing-Abstand entspricht `TrailingStopPips` und der Stop wird nur verschoben, wenn sich der Preis mindestens `TrailingStepPips` zugunsten des Handels bewegt hat. Die Trailing-Logik emuliert den ursprünglichen Experten: Bei Short-Trades folgt er dem Ask-Preis nach unten, während Long-Trades von dieser Strategie nicht generiert werden.
- Ausstiegsbedingungen werden vor der Suche nach neuen Einstiegen bei jeder Kerze bewertet. Die Strategie schließt die Position, wenn entweder der Stop-Loss oder Take-Profit getroffen wird, oder wenn die Trailing-Logik den Stop über den aktuellen Ask-Preis strafft.

## Parameter
| Name | Beschreibung |
| ---- | ------------ |
| `BarsCount` | Anzahl der Hausse-Kerzen vor der bärischen Setup-Kerze (Standard: 3). |
| `TakeProfitPips` | Take-Profit-Abstand in Pips, angewendet auf den Einstiegspreis (Standard: 50). |
| `StopLossPips` | Stop-Loss-Abstand in Pips, angewendet auf den Einstiegspreis (Standard: 50). |
| `TrailingStopPips` | Abstand zwischen Marktpreis und Trailing-Stop (Standard: 10). |
| `TrailingStepPips` | Minimale günstige Bewegung, bevor der Trailing-Stop vorrückt (Standard: 5). |
| `RiskPercent` | Prozentualer Anteil des Portfoliokapitals, der pro Trade riskiert wird (Standard: 5). |
| `CandleType` | Kerzen-Datentyp/Zeitrahmen für die Mustererkennung (Standard: 1 Stunde). |

## Verwendungshinweise
- Konfigurieren Sie die `Volume`-Eigenschaft auf die von Ihrem Broker erlaubte Mindestordergröße. Die risikobasierte Größenbestimmung kann die Handelsgröße erhöhen, reduziert sie aber nie unter `Volume`.
- Die Strategie hält jederzeit nur eine aggregierte Short-Position. Wenn eine Long-Position vorhanden ist, wird diese vor dem Eröffnen der Short-Position geschlossen.
- Der Algorithmus arbeitet mit Kerzendaten. Intrabar-Stop-Loss- oder Take-Profit-Treffer werden mit dem Kerzenhoch/-tief erkannt, sodass der tatsächliche Ausführungszeitpunkt vom Tick-Level-Execution abweichen kann.
- In dieser Version wird keine Python-Version bereitgestellt. Nur die C#-Implementierung in `API/2574/CS/NUp1DownStrategy.cs` ist verfügbar.
