# Rücksetzer-Rebound-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Rücksetzer-Rebound-Strategie ist eine C#-Konvertierung des MQL5-Expertenberaters "TST (barabashkakvn's Edition)". Sie überwacht ein einzelnes Instrument auf dem durch den Parameter `CandleType` angegebenen Zeitrahmen und sucht nach starken Bewegungen, die zurück in den Balken-Range zurücksetzen. Wenn ein bullischer Balken von seinem Hoch um mehr als den Rücksetzer-Schwellenwert verblasst, kauft die Strategie, während ein äquivalenter bärischer Rücksetzer einen Verkauf auslöst. Die Implementierung verwendet StockSharp's High-Level-Kerzenabonnement-API und verwaltet alle Schutzorders in Pip-Einheiten, die in absolute Preisoffsets umgerechnet werden.

Pip-Abstände werden aus dem Instrument-`PriceStep` berechnet. Für Symbole, die mit drei oder fünf Dezimalstellen notieren, multipliziert die Strategie den Schritt automatisch mit zehn, um der MetaTrader-Definition eines Pips zu entsprechen. Alle Positionsgrößenbestimmungen werden von der Basis-`Volume`-Eigenschaft der Strategie übernommen.

## Einstiegslogik
- Nur abgeschlossene Kerzen aus der konfigurierten `CandleType`-Serie verarbeiten.
- Mit `ReverseSignal = false` (Standard):
  - **Long-Setup:** die Kerze schließt unter ihrer Eröffnung und die Differenz zwischen dem Kerzenhoch und dem Schluss überschreitet `RollbackRatePips` (in Preis umgerechnet). Dies zeigt an, dass sich der Preis nach oben ausdehnte und dann tief genug zurücksetzte, um für einen konträren Long-Einstieg zu qualifizieren.
  - **Short-Setup:** die Kerze schließt über ihrer Eröffnung und die Differenz zwischen dem Schluss und dem Kerzentief überschreitet `RollbackRatePips`. Dies spiegelt die Long-Logik auf der bärischen Seite wider.
- Wenn `ReverseSignal = true` sind die Rollen der Long- und Short-Bedingungen getauscht, was dem Händler ermöglicht, die Richtung ohne Änderung der anderen Parameter zu wechseln.
- Neue Einstiege werden nur platziert, wenn die aktuelle Position flach oder in der entgegengesetzten Richtung ist. Das ausgeführte Volumen entspricht `Volume + |Position|`, damit eine entgegengesetzte Position geschlossen wird, bevor der neue Trade eröffnet wird.

## Ausstiegslogik
- Beim Einstieg speichert die Strategie Stop-Loss- und Take-Profit-Levels basierend auf den konfigurierten Pip-Offsets. Wenn die Kerzenspanne einen Level berührt, wird die Position mit einer Market-Order geschlossen.
- `StopLossPips = 0` oder `TakeProfitPips = 0` deaktiviert den entsprechenden Schutz-Level.
- Die Trailing-Logik wird aktiv, sobald der schwebende Gewinn `TrailingStopPips + TrailingStepPips` (in Preis-Einheiten) überschreitet.
  - Für Long-Trades ratschiert der Stop auf `höchsten Preis - TrailingStopPips`, wenn der neue Level mindestens `TrailingStepPips` über dem vorherigen Stop liegt.
  - Für Short-Trades ratschiert der Stop auf `niedrigsten Preis + TrailingStopPips`, wenn der neue Level mindestens `TrailingStepPips` unter dem vorherigen Stop liegt.
  - Wenn der Markt sich umkehrt und den Trailing-Stop kreuzt, wird die Position sofort ausgestiegen.
- Wenn keine Position offen ist, werden alle internen Zustandsvariablen geleert, um veraltete Daten zu vermeiden.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzenserie für die Signalberechnung. | 15-Minuten-Zeitrahmen |
| `StopLossPips` | Abstand des Schutz-Stops in Pips. Auf null setzen zum Deaktivieren. | 30 |
| `TakeProfitPips` | Abstand des Take-Profits in Pips. Auf null setzen zum Deaktivieren. | 90 |
| `TrailingStopPips` | Trailing-Stop-Offset in Pips. Auf null setzen zum Deaktivieren des Trailings. | 1 |
| `TrailingStepPips` | Extra-Gewinn (in Pips) erforderlich, bevor der Trailing-Stop wieder bewegt werden kann. Muss positiv sein, wenn Trailing aktiviert ist. | 15 |
| `RollbackRatePips` | Minimaler Rücksetzer vom Balken-Extrempunkt, der ein Signal validiert. | 15 |
| `ReverseSignal` | Kehrt die Einstiegsrichtung um (Long-Signale werden zu Short und umgekehrt). | false |

## Verwendungshinweise
- Die `Volume`-Eigenschaft vor dem Start der Strategie setzen; sie definiert die gehandelte Menge für jede Order.
- Trailing erfordert `TrailingStopPips > 0` und `TrailingStepPips > 0`. Die Strategie wirft beim Start einen Fehler, wenn diese Beziehung verletzt wird.
- Da der ursprüngliche Experte Ticks innerhalb des aktiven Balkens auswertete, verwendet der C#-Port das Hoch/Tief/Schluss der abgeschlossenen Kerze, um dasselbe Verhalten anzunähern. Der Unterschied ist für die meisten Backtests vernachlässigbar und hält die Implementierung mit StockSharp's High-Level-API ausgerichtet.
- Die Strategie funktioniert mit einem einzelnen Wertpapier. Um mehrere Instrumente zu handeln, separate Strategie-Instanzen erstellen.
