# N-Kerzen-Sequenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die N-Kerzen-Sequenz-Strategie repliziert das Verhalten des ursprünglichen MetaTrader-Expertenberaters „N-_Candles_v7" mit der StockSharp High-Level-API. Sie überwacht abgeschlossene Kerzen und sucht nach einer konfigurierbaren Anzahl aufeinanderfolgender bullischer oder bärischer Körper. Wenn eine qualifizierende Serie vorliegt, öffnet die Strategie eine Position in dieselbe Richtung und verwaltet sie mit konfigurierbarem Take-Profit, Stop-Loss, Trailing-Stop, Handelsstunden-Filter und schwebender Gewinnsperre.

## Handelslogik
- Bewertet jede abgeschlossene Kerze und klassifiziert sie als bullisch, bärisch oder neutral (Doji). Neutrale Kerzen setzen den Serienzähler zurück und können das „schwarzes Schaf"-Verhalten auslösen.
- Pflegt eine laufende Zählung aufeinanderfolgender Kerzen mit derselben Körperrichtung. Sobald die Zählung den konfigurierten Schwellenwert erreicht, wird die aktuelle Richtung zum aktiven Muster.
- Bei einer aktiven bullischen Serie versucht die Strategie, eine Long-Position zu eröffnen; bei einer aktiven bärischen Serie versucht sie, eine Short-Position zu eröffnen. Es wird immer nur eine Netto-Position gehalten.
- Wenn eine Kerze die einheitliche Richtung bricht ("schwarzes Schaf"), reagiert die Strategie gemäß dem ausgewählten Schließmodus: alles schließen, nur entgegengesetzte Positionen schließen oder nur Positionen schließen, die zur vorherigen Serie ausgerichtet sind.
- Optional schränkt sie Einstiege auf ein durch Start- und Endstunden (inklusiv) definiertes Handelszeitfenster ein.
- Überwacht kontinuierlich die offene Position für Take-Profit, Stop-Loss, Trailing-Stop-Aktualisierungen und den schwebenden Gewinnschwellenwert.

## Positions- und Risikomanagement
- Der anfängliche Schutz-Stop und das Ziel werden aus Pip-Distanzen berechnet, die mit dem Preisschritt des Instruments konvertiert wurden. Diese Niveaus werden jedes Mal neu berechnet, wenn eine neue Position eröffnet wird.
- Die Trailing-Stop-Logik ahmt den ursprünglichen Experten nach: Sobald der Preis die Trailing-Distanz plus Schritt zurücklegt, wird der Stop bewegt, um die Trailing-Distanz beizubehalten.
- Ein schwebender Gewinnwächter (`MinProfit`) schließt die gesamte Position, sobald der offene Gewinn den konfigurierten Wert übersteigt.
- Der Parameter `MaxPositionVolume` verhindert Einstiege, wenn das angeforderte Volumen über dem erlaubten Limit liegt. `MaxPositions` fungiert als Schutz gegen Wiedereinstieg, wenn eine Position bereits aktiv ist.
- Alle Ausstiege rufen Market-Orders auf, um die Netto-Position zu flatten, da die StockSharp-Strategie in einer Netting-Umgebung arbeitet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `ConsecutiveCandles` | Anzahl der Kerzen mit identischer Richtung, die erforderlich sind, um ein Signal auszulösen. |
| `OrderVolume` | Market-Order-Volumen für Einstiege und Ausstiege. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. Auf null setzen zum Deaktivieren. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Auf null setzen zum Deaktivieren. |
| `TrailingStopPips` | Trailing-Stop-Distanz. Auf null setzen zum Deaktivieren des Trailings. |
| `TrailingStepPips` | Zusätzliche Distanz erforderlich, bevor der Trailing-Stop bewegt wird. |
| `MaxPositions` | Maximale Anzahl simultaner Einstiege pro Muster (die Strategie hält eine einzelne Netto-Position). |
| `MaxPositionVolume` | Obergrenze für das erlaubte Netto-Volumen. |
| `UseTradeHours` / `StartHour` / `EndHour` | Aktivieren und konfigurieren des Handelszeitfensters (inklusiv). |
| `MinProfit` | Schwebender Gewinnschwellenwert, der einen vollständigen Ausstieg auslöst. |
| `ClosingBehavior` | Definiert, wie auf eine „schwarzes Schaf"-Kerze reagiert wird. |
| `CandleType` | Die für Berechnungen verwendete Kerzenserie. |

## Hinweise und Annahmen
- Die Strategie arbeitet mit Netto-Positionen; mehrere Hedging-Style-Tickets werden nicht erstellt. Dies unterscheidet sich vom ursprünglichen Experten, wo mehrere abgesicherte Positionen gleichzeitig bestehen konnten.
- Der schwebende Gewinn wird approximiert als `(aktueller Preis - Einstiegspreis) * Volumen` für Long-Positionen und umgekehrt für Short-Positionen.
- Die Pip-Konvertierung stützt sich auf den `PriceStep` des Instruments. Für Symbole, bei denen der minimale Schritt nicht angegeben ist, wird ein Standard-Pip von 0.0001 angenommen.
- Es wird keine Python-Portierung bereitgestellt, wie angefordert.
