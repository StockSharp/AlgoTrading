# Double-Up-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Double Up-Strategie ist eine direkte Portierung des MetaTrader-Expertenberaters `DoubleUp.mq4`. Es kombiniert einen Rohstoffkanalindex-Oszillator (CCI) mit der Hauptlinie des MACD-Indikators, um extreme Momentumbedingungen zu erkennen, und wendet dann ein Positionsgrößenmodell im Martingal-Stil an. Immer wenn beide Oszillatoren die gleiche Extremzone erreichen, bereitet sich der Algorithmus auf einen Handel in die entgegengesetzte Richtung vor. Sobald CCI zum Mittelpunkt zurückkehrt, eröffnet die Strategie entweder eine neue Long-Position (nach Schließung bestehender Short-Positionen) oder eine neue Short-Position (nach Schließung bestehender Long-Positionen).

Das Volumen jeder neuen Position basiert auf einer exponentiellen Progression (`baseVolume * 2^lossCounter`). Aufeinanderfolgende Verlustausstiege erhöhen den Exponenten, während ein profitabler Ausstieg den Fortschritt entsprechend dem akkumulierten Wartepuffer zurücksetzt. Dieses Verhalten spiegelt die Pyramidenlogik im Originalcode wider, bei dem die Variablen `pos` und `wait` den angewendeten Multiplikator steuern.

## Handelslogik
- Abonnieren Sie eine einzelne Kerzenserie und berechnen Sie die Hauptlinie CCI (Standardlänge 8) und MACD (Standard schnell 13, langsam 33, Signal 2).
- Multiplizieren Sie den Messwert MACD mit einer Million, sodass seine Größe dem Schwellenwert CCI entspricht.
- Wenn beide Oszillatoren `+Threshold` überschreiten, bereiten Sie die Strategie auf einen zukünftigen Short-Einstieg vor. Wenn beide Oszillatoren unter `-Threshold` fallen, machen Sie ihn für einen zukünftigen Long-Einstieg bereit.
- Ein ausstehender Long-Eintrag wird ausgeführt, sobald der Wert CCI wieder unter `+Threshold` fällt. Ein ausstehender Short-Eintrag wird ausgeführt, wenn CCI unter `-Threshold` fällt, während das Short-Flag aktiv ist, wodurch die genaue Reihenfolge des ursprünglichen Codes reproduziert wird.
- Bevor eine neue Position eröffnet wird, schließt der Algorithmus alle entgegengesetzten Positionen vollständig. Die neue Bestellung wird erst versandt, nachdem alle Abschlussgeschäfte abgeschlossen sind.
- Exit-Trades sind Marktaufträge, die bei Signalumkehrungen generiert werden. Der realisierte Gewinn oder Verlust jedes abgeschlossenen Handels speist die Martingal-Zähler.

## Positionsgrößenmodell
- Verlierende Exits erhöhen den Verlustzähler. Nachdem der Zähler `PreWait` erreicht hat, wird sein Wert zum Wartepuffer hinzugefügt und der Verlustzähler wird auf Null zurückgesetzt.
- Ein gewinnbringender Exit überträgt den (abgeschnittenen) Wartepufferwert in den Verlustzähler und löscht den Puffer. Zukünftige Handelsgrößen beginnen daher bei `2^lossCounter` Lots.
- Der Wartepuffer wird ab `InitialWait` initialisiert und wird ansonsten durch die oben genannten Regeln gesteuert.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CciPeriod` | 8 | Zeitraum des Commodity Channel Index. |
| `Threshold` | 230 | Absoluter Pegel zur Erkennung von Oszillatorextremen. |
| `MacdFastPeriod` | 13 | Schnelle EMA-Länge der MACD-Berechnung. |
| `MacdSlowPeriod` | 33 | Langsame EMA-Länge der MACD-Berechnung. |
| `MacdSignalPeriod` | 2 | Signallänge von EMA, erforderlich, um mit der Anrufsignatur von MetaTrader übereinzustimmen. |
| `BaseVolume` | 0,01 | Startvolumenmultiplikator vor Anwendung des Martingal-Exponenten. |
| `InitialWait` | 0 | Anfangswert des Wartepuffers (Variable `wait` im Originalskript). |
| `PreWait` | 2 | Mindestanzahl aufeinanderfolgender Verlustexits, die erforderlich sind, bevor der Wartepuffer den Verlustzähler akkumuliert. |
| `BackShift` | 0 | Historische Verschiebung der Indikatorwerte. In diesem Port wird nur Null unterstützt. |
| `CandleType` | 15-minütiger Zeitrahmen | Vom Datenfeed angeforderter Kerzentyp. Passen Sie es an den in MetaTrader verwendeten Diagrammzeitrahmen an. |

## Notizen
- Der Port unterstützt derzeit nur `BackShift = 0` und spiegelt die Standardkonfiguration des ursprünglichen Expert Advisors wider.
- Bei jeder Auftragserteilung und -schließung werden Marktaufträge verwendet. Fügen Sie bei Bedarf separate Schutzmaßnahmen (Stop-Loss, Take-Profit) hinzu.
- Da die Strategie die Positionsgröße nach verlorenen Trades verdoppelt, stellen Sie sicher, dass Margin-Limits und Risikokontrollen für das gehandelte Instrument angemessen sind.
