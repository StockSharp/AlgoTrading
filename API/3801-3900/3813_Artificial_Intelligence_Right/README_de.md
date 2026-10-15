# Richtige Strategie für künstliche Intelligenz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den MetaTrader 4 Expert Advisor **ArtificialIntelligence_Right.mq4**. Es wertet eine Einzelschicht aus
Perzeptron basiert auf dem Beschleunigungs-/Verzögerungsoszillator (AC), um zu entscheiden, wann die Marktdynamik die Richtung ändert. Die
Perceptron verwendet vier verzögerte Wechselstromproben und wandelt diese in ein vorzeichenbehaftetes Signal um, das sowohl Ein- als auch Umkehrungen steuert.

Im Gegensatz zum ursprünglichen EA funktioniert der Port StockSharp mit der High-Level-Kerze API. Preisaktionen werden jeweils am Ende durchgeführt
fertige Kerze, wodurch die Logik für Backtests und Optimierungsworkflows deterministisch bleibt.

## Indikatoren und Berechnungen
- Der **Beschleunigungs-/Verzögerungsoszillator** wird aus dem Awesome Oscillator neu aufgebaut, indem ein 5-Perioden-SMA vom AO subtrahiert wird
Werte (5-Perioden SMA von `HL2` minus 34-Perioden SMA von `HL2`).
- Ein Ringpuffer speichert die 22 aktuellsten AC-Werte, sodass das Perzeptron auf die Offsets 0, 7, 14 und 21 zugreifen kann, die genau übereinstimmen
die MetaTrader-Implementierung.
- Die Perzeptrongewichte werden um `-100` vor dem Skalarprodukt verschoben, wodurch die `w = x - 100`-Logik des Quellcodes reproduziert wird.

## Handelsregeln
1. **Eintrittsbedingungen**
   - Wenn der Perzeptron-Output positiv ist und die Strategie flach ist, wird eine Marktkauforder übermittelt.
   - Wenn der Perzeptron-Output negativ ist und die Strategie flach ist, wird ein Marktverkaufsauftrag erteilt.
2. **Stop-Loss-Management**
   - Nach jedem Eintritt wird in einer Entfernung von `StopLossPoints * PriceStep` ein virtueller Schutzstopp zugewiesen
Eintrittspreis. Dies emuliert den `Point`-Multiplikator von MetaTrader.
   - Wenn der Schlusskurs dieses Niveau überschreitet, wird die Position zum Marktwert aufgelöst, um die Ausführung der Stop-Loss-Order nachzuahmen.
3. **Trailing und Umkehr**
   - Sobald die Position um `(2 * StopLossPoints + SpreadPoints)` Punkte im Gewinn schwankt, startet entweder der ursprüngliche Roboter
Der Stop wird um die Stop-Loss-Distanz nachgezogen oder umgekehrt, wenn das Perzeptron sein Vorzeichen ändert.
   - Die StockSharp-Version verwendet denselben Auslöser: Wenn die Gewinnschwelle erreicht ist und das Perzeptron die Richtung umgekehrt hat,
Es wird eine Marktorder mit dem Doppelten des aktuellen Risikos erteilt, um den Handel rückgängig zu machen. andernfalls wird der virtuelle Stopp angefahren
Behalten Sie den ursprünglichen Abstand vom aktuellen Schlusskurs bei.

Alle Umkehrungen werden durch den Handel mit dem doppelten offenen Volumen durchgeführt, sodass die resultierende Position die MetaTrader `OrderCloseBy` widerspiegelt.
Verhalten, was zu einer entgegengesetzten Richtung, aber derselben Losgröße führt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `X1` … `X4` | Perzeptrongewichte. Die Standardwerte replizieren die `.mq4`-Quelle (135, 127, 16, 93). |
| `StopLoss` | Stop-Loss-Distanz, ausgedrückt in MetaTrader Punkten. Es wird mit dem Instrument `PriceStep` multipliziert, um einen echten Preisversatz zu erhalten. |
| `Spread` | Zusätzlicher Spread-Puffer (Standard 3 Punkte), der in der Trailing-Trigger-Bedingung verwendet wird. |
| `Candle Type` | Für Berechnungen verwendete Kerzenreihe. Standardmäßig ist der Zeitrahmen 1 Minute. |

Die Eigenschaft `Volume` ist voreingestellt auf 1 Los und spiegelt den Eingabeparameter `lots` des ursprünglichen Experten wider.

## Implementierungshinweise
- Indikatorberechnungen und der Perzeptronstatus werden jedes Mal zurückgesetzt, wenn die Strategie zurückgesetzt wird, um zu verhindern, dass veraltete Werte verursacht werden
falsche Auslöser.
- Wenn das Wertpapier keinen `PriceStep` bereitstellt, greift die Strategie auf einen Punktwert von `1` zurück, um die Kompatibilität aufrechtzuerhalten
mit generischen Backtesting-Instrumenten.
- Es werden keine echten Stop-Orders registriert; Stattdessen wird die Stopplogik über Marktaufträge im Candle-Handler ausgeführt. Dadurch bleibt die
konsistentes Verhalten bei allen Brokern und Simulatoren.
