# Parabolic SAR Bug-3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Parabolic SAR Bug 3 Strategy** ist eine StockSharp High-Level-Portierung des MetaTrader 4 Expert Advisors `pSAR_bug_3.mq4` in `MQL/9786`. Der Roboter reagiert auf den allerersten Parabolic SAR Punkt, der auf der gegenüberliegenden Seite des Preises erscheint. Wenn der SAR unter den Schlusskurs der Kerze fällt, eröffnet die Strategie eine Long-Position, nachdem alle Short-Positionen geschlossen wurden. Wenn der SAR über den Schlusskurs springt, kehrt er sich in eine Short-Position um. Jeder Trade wird durch feste Stop-Loss- und Take-Profit-Levels geschützt, die in Parabolic SAR Punkten gemessen und mit demselben Multiplikator wie im ursprünglichen MQL-Programm skaliert werden.

## Handelslogik
1. **Marktdaten und Indikator** – Die Strategie abonniert einen konfigurierbaren Kerzentyp (standardmäßig 15-Minuten-Zeitrahmen) und bindet einen Parabolic SAR-Indikator mit benutzerdefiniertem Beschleunigungsschritt und maximaler Beschleunigung.
2. **Statusverfolgung** – nach der ersten abgeschlossenen Kerze speichert der Code, ob der Wert Parabolic SAR über oder unter dem Schlusskurs liegt. Die nächsten Kerzen vergleichen den neuen Zustand mit dem vorherigen, um den Wechsel des Indikators zu erkennen.
3. **Long-Einträge** – wenn der Kurs Parabolic SAR von über dem Schlusskurs auf darunter wechselt, sendet die Strategie einen Marktauftrag in der Größe, alle aktiven Short-Positionen zu schließen und das konfigurierte Long-Volumen zu eröffnen. Schützende Stop-Loss- und Take-Profit-Preise werden unmittelbar nach der Eingabe berechnet.
4. **Short-Einträge** – wenn der Kurs Parabolic SAR von unterhalb des Schlusskurses nach darüber kreuzt, spiegelt der Code das Verhalten für Short-Trades wider: Er flacht Long-Positionen ab und eröffnet eine Short-Order.
5. **Exits** – bei jeder fertigen Kerze werden die Höchst- und Tiefstpreise mit den hinterlegten Schutzniveaus verglichen. Das Überschreiten des Stop-Loss oder des Take-Profit löst eine Marktorder aus, die die offene Position schließt, was dem MetaTrader-Ansatz von Schutzaufträgen auf Brokerseite entspricht.

## Risikomanagement
- Die Stop-Loss- und Take-Profit-Abstände werden durch Multiplikation von `StopLossPoints` oder `TakeProfitPoints` mit dem `StopMultiplier` und dem Instrument `PriceStep` (oder `0.0001`, wenn das Symbol keine Stufe vorsieht) umgerechnet.
- Marktaufträge werden nur gesendet, wenn `IsFormedAndOnlineAndAllowTrading()` bestätigt, dass das Abonnement aktiv ist und der Handel zulässig ist.
- Bei jeder Änderung der Positionsrichtung werden die ungenutzten Schutzebenen für die alte Seite gelöscht, um veraltete Ausgänge zu verhindern.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Bestellvolumen in Losen. Durch die Aktualisierung des Werts wird auch die Basiseigenschaft `Strategy.Volume` geändert. |
| `StopLossPoints` | `90` | Stop-Loss-Distanz ausgedrückt in Parabolic SAR Punkten, später skaliert durch `StopMultiplier` und den Preisschritt des Instruments. |
| `TakeProfitPoints` | `20` | Take-Profit-Distanz ausgedrückt in Parabolic SAR Punkten, später skaliert durch `StopMultiplier` und den Preisschritt. |
| `StopMultiplier` | `10` | Multiplikator, der die Eingabe MetaTrader `StopMult` reproduziert und so die Kompatibilität mit Bruchteil-Pip-Brokern ermöglicht. |
| `SarStep` | `0.02` | Anfänglicher Beschleunigungsfaktor für den Indikator Parabolic SAR. |
| `SarMaximum` | `0.2` | Maximaler Beschleunigungsfaktor für den Indikator Parabolic SAR. |
| `CandleType` | `15m time-frame` | Kerzentyp, der für Indikatorberechnungen und Signalerkennung verwendet wird. |

## Konvertierungshinweise
- MetaTrader schlossen Positionen, bevor sie den entgegengesetzten Handel mit separaten Aufträgen eröffneten. Die StockSharp-Version erzielt das gleiche Ergebnis, indem sie eine einzelne Marktorder sendet, deren Größe so dimensioniert ist, dass sie etwaige gegenteilige Risiken ausgleicht und das neue Positionsvolumen festlegt.
- Stop-Loss- und Take-Profit-Orders auf Brokerseite werden durch die Überwachung von Candle-Extremen und die Übermittlung von Marktausstiegen nachgeahmt, sobald die Schwellenwerte überschritten werden.
- Der zusätzliche Parameter `StopMultiplier` akzeptiert jeden positiven Wert, ist jedoch standardmäßig `10`, der einzige Multiplikator, der in den ursprünglichen Codekommentaren dokumentiert ist.
- Für diese Konvertierung wird keine Python-Version bereitgestellt, genau wie in der Aufgabenbeschreibung gefordert.
