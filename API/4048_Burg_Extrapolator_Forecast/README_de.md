# Burg-Extrapolator-Prognosestrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Burg Extrapolator-Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „Burg Extrapolator“. Das ursprüngliche System passt ein autoregressives Burg-Modell (AR) an ein Schiebefenster offener Preise (oder ihrer Momentum-/ROC-Transformationen) an und projiziert einen Pfad zukünftiger Preise. Handelsentscheidungen werden aus den extremsten Prognosewerten abgeleitet: Wenn die prognostizierte Abweichung in eine Richtung groß genug ist, baut die Strategie entweder neue Positionen auf oder liquidiert das Engagement in die entgegengesetzte Richtung. Bei der Konvertierung bleiben die gleichen Modellierungsblöcke erhalten, während die Positionsverwaltung und die Geldverwaltung auf StockSharp-Grundelemente abgebildet werden.

## Handelslogik
1. **Datenvorbereitung**
   - Erstellen Sie eine fortlaufende Historie von `PastBars + 1` Eröffnungspreisen für die ausgewählten `CandleType`.
   - Wandeln Sie die Daten optional in einen logarithmischen Impuls (Standard) oder eine prozentuale Änderungsrate um, bevor Sie sie dem AR-Modell zuführen. Die Rohpreise werden anhand ihres gleitenden Durchschnitts zentriert, um den MT4-Code widerzuspiegeln.
2. **Burg lineare Vorhersage**
   - Schätzen Sie Reflexionskoeffizienten bis zur Ordnung `PastBars * ModelOrder` mit dem Burg-Algorithmus.
   - Generieren Sie eine Folge zukünftiger Werte (in der Praxis `PastBars` Schritte voraus), indem Sie das AR-Modell rekursiv erweitern. Die Transformationen werden zurück in den Preisraum invertiert, sodass alle Prognosen in absoluten Preiseinheiten erfolgen.
3. **Signalerkennung**
   - Gehen Sie den Prognosepfad durch und zeichnen Sie den höchsten und niedrigsten prognostizierten Preis auf, bevor ein weiteres Extrem auftritt. Der Abstand zwischen dem ersten Extrem und der anderen Seite des Prognosebereichs wird mit den Schwellenwerten `MaxLoss` und `MinProfit` verglichen (durch Multiplikation mit dem Instrument `PriceStep` in einen absoluten Preis umgewandelt).
   - Ein ausreichend großer Aufschwung löst `OpenSignal = 1` aus, während ein großer Abschwung `OpenSignal = -1` ergibt. Wenn das entgegengesetzte Extrem zuerst auftritt, setzt die Logik `CloseSignal` so, dass die aktuelle Exposition verlassen wird, auch wenn kein neuer Einstieg geplant ist.
4. **Auftragsverwaltung**
   - Schutzausstiege (Stop-Loss, Take-Profit und optionaler Trailing-Stop) werden ausgeführt, bevor ein neues Signal ausgeführt wird. Der Trailing-Stop verwendet den besten Preis seit dem letzten Eintrag wieder und schließt die Position, wenn der Preis um `TrailingStop` Punkte zurückgeht, was dem MT4-Verhalten beim Verschieben der Schutzorder entspricht.
   - Wenn ein Signal dazu auffordert, das Engagement in die entgegengesetzte Richtung zu schließen, sendet die Strategie eine Marktorder, deren Größe die aktuelle Nettoposition glättet.
   - Einstiegssignale stapeln zusätzliche Marktaufträge in der angegebenen Richtung, bis `MaxTrades` erreicht ist. Das Ordervolumen skaliert linear mit der Anzahl der aktiven Trades unter Verwendung des Faktors `1 + existingTrades * MaxRisk`, einem StockSharp-freundlichen Ersatz für die ursprüngliche marginbasierte Größenbestimmungsroutine.

## Indikatoren und Daten
- Kerzenabonnement definiert durch `CandleType` (Standardzeitraum von 30 Minuten).
- Internes autoregressives Burg-Modell (implementiert ohne externe Indikatoren).
- Optionale logarithmische Impuls- und prozentuale Änderungsratentransformationen.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 30-Minuten-Kerzen | Primärer Zeitrahmen, der von der Strategie verarbeitet wird. |
| `MaxRisk` | 0,5 | Risikomultiplikator, der beim Stapeln mehrerer Trades verwendet wird. |
| `MaxTrades` | 5 | Maximale Anzahl gleichzeitiger Trades pro Richtung. |
| `MinProfit` | 160 | Der prognostizierte Mindestgewinn (in Punkten), der zum Eröffnen neuer Geschäfte erforderlich ist. |
| `MaxLoss` | 130 | Maximal tolerierter prognostizierter Verlust (in Punkten) vor der Schließung von Geschäften. |
| `TakeProfit` | 0 | Optionale feste Take-Profit-Distanz in Punkten (0 deaktiviert sie). |
| `StopLoss` | 180 | Optionaler fester Stop-Loss-Abstand in Punkten (0 deaktiviert ihn). |
| `TrailingStop` | 10 | Trailing-Stop-Distanz in Punkten, nur aktiv, wenn `StopLoss > 0`. |
| `PastBars` | 200 | Anzahl der im Burg-Modell verwendeten historischen Kerzen. |
| `ModelOrder` | 0,37 | Bruchteil von `PastBars`, umgewandelt in die Burg-Ordnung. |
| `UseMomentum` | wahr | Wenden Sie eine logarithmische Impulstransformation auf die Eingabedaten an. |
| `UseRateOfChange` | falsch | Prozentuale Änderungsrate anwenden (wird ignoriert, wenn Impuls aktiviert ist). |

Alle Parameter sind `StrategyParam<T>`-Instanzen und können im StockSharp-Designer optimiert oder angepasst werden.

## Implementierungshinweise
- Der Burg-Algorithmus ist direkt in C# implementiert und behält die gleiche Rekursion wie die MT4-Version bei. Alle Berechnungen werden mit doppelter Genauigkeit ausgeführt, während die endgültigen Prognosen vor der Signalprüfung wieder in `decimal` konvertiert werden.
- Das ursprüngliche EA konnte sich zur Größenbestimmung der Positionen auf die Kontoinformationen von MetaTrader stützen. In StockSharp wird der Geldverwaltungsblock durch eine deterministische Skalierungsregel basierend auf `Volume` und `MaxRisk` ersetzt. Stellen Sie `Volume` auf das gewünschte Basislos ein und die Strategie skaliert nachfolgende Einträge proportional.
- Die Schutzlogik schließt Positionen mit expliziten Marktaufträgen, anstatt die Stopps auf Brokerseite zu ändern. Dies entspricht dem übergeordneten API-Design von StockSharp und verhindert einen veralteten Zustand bei der Ausführung in der Simulation.
- Die Prognose-Arrays werden bei jeder Änderung von `PastBars` oder `ModelOrder` neu erstellt, sodass sich spontane Parameteränderungen sofort auf das AR-Modell auswirken, ohne dass die Strategie neu gestartet werden muss.
- Um das Verhalten zu visualisieren, können Sie im Designer ein Diagramm anhängen: Die Strategie zeichnet bereits Kerzen und führt Trades im Standardbereich aus. Das Erweitern der Stichprobe mit benutzerdefinierten Reihen (z. B. Prognosepfad) ist bei Bedarf problemlos möglich.
