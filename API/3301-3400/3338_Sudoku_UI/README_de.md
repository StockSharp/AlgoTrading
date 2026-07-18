# Sudoku-UI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 5-Skripts **SudokuUI.mq5**. Das ursprüngliche MQL-Programm bietet eine grafische Sudoku-Rätseloberfläche mit Parametern, die die Rätselerstellung, das Mischen und die automatischen Aktualisierungen steuern. Da sich die StockSharp-Umgebung auf automatisierten Handel statt auf interaktive Diagramm-Widgets konzentriert, wandelt der Port die zugrunde liegenden Konzepte in eine gitterbasierte Mean-Reversion-Strategie um, die auf Puzzle-Statistiken basiert.

Das Sudoku-Brett wird als 9x9-Ziffernmatrix interpretiert. Spaltendurchschnitte definieren symmetrische Abweichungsschwellenwerte um einen einfachen gleitenden Durchschnitt (SMA). Wenn der Preis von SMA über diese vom Sudoku abgeleiteten Niveaus hinaus abweicht, geht die Strategie eine Position in die entgegengesetzte Richtung ein und strebt eine Rückkehr zurück zum Mittelwert an. Durch die Rückkehr in eine neutrale Zone wird die Position geschlossen und die Fähigkeit des Originalwerkzeugs zum Zurücksetzen der Platine nachgeahmt.

## Handelslogik

1. **Puzzle-Vorbereitung**
   - Die Strategie kann eine 81-stellige Sudoku-Spezifikation aus einer Datei oder einer Rohzeichenfolge laden. Nicht-stellige Zeichen werden ignoriert und Nullen werden übersprungen, um den Sudoku-Ziffernanforderungen zu entsprechen.
   - Wenn kein gültiges Puzzle bereitgestellt wird, wird durch wiederholtes Mischen der Ziffernpools ein Pseudozufallsbrett generiert. Die Logik berücksichtigt sowohl die *Shuffling*- als auch die *Composition*-Seeds, die in der MQL-Version verfügbar gemacht wurden, damit Händler reproduzierbare Layouts erhalten können.
   - Eine bestimmte Ziffer kann eliminiert werden, bevor Statistiken berechnet werden. Dies ahmt die ursprüngliche GUI-Option nach, mit der bestimmte Beschriftungen ausgeblendet wurden, und bietet eine einfache Möglichkeit, das aktive Raster zu verkleinern.

2. **Levelaufbau**
   - Für jede Rätselspalte wird nach dem Eliminierungsschritt der Durchschnitt ermittelt. Der Durchschnitt wird auf den Bereich [-1, 1] normalisiert und mit `ThresholdRange` multipliziert, wodurch sich Preisabweichungsniveaus ergeben, die als Bruchteile des SMA-Werts ausgedrückt werden.
   - Negative oder positive Fallback-Werte werden eingefügt, wenn das Puzzle nur Werte auf einer Seite von SMA erzeugt, wodurch sichergestellt wird, dass sowohl lange als auch kurze Auslöser vorhanden sind.

3. **Signalerzeugung**
   - Die Strategie abonniert den konfigurierten Kerzentyp und bindet ihn an einen SMA-Indikator. Es werden nur fertige Kerzen gemäß den Best Practices von StockSharp verarbeitet.
   - Wenn der prozentuale Abstand zwischen dem Schlusskurs und dem SMA den negativsten Wert unterschreitet, wird eine Long-Position eröffnet (nachdem die Shorts abgeflacht wurden). Das Überschreiten des höchsten positiven Niveaus eröffnet auf die gleiche Weise eine Short-Position.
   - Ein neutrales Band um die Nullabweichung (`NeutralBand`) erzwingt eine flache Belichtung. Dies ersetzt den Sudoku-„Assistenten“, der den Rätselstatus automatisch anpasst.

4. **Automatische Aktualisierung**
   - Wenn Sie `EnableAutoUpdate` auf `true` setzen, wird das Sudoku-Raster zu Beginn jedes Handelstages neu generiert. Die Shuffling-Seeds, die Eliminierungseinstellungen und die Shuffle-Anzahl beeinflussen alle die neu berechneten Schwellenwerte und sorgen für ein dynamisches und dennoch reproduzierbares Raster.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `PuzzleDefinition` | Dateipfad oder Inline-Ziffern, die das für Levelberechnungen verwendete Sudoku-Rätsel beschreiben. |
| `ShufflingRandomSeed` | Primärer Samen für die Puzzle-Generierung. `-1` leitet den Startwert vom Handelstag ab. |
| `CompositionRandomSeed` | Sekundärer Seed, der den Mischprozess zur Erstellung alternativer Layouts stört. |
| `ShufflingCycles` | Anzahl zusätzlicher Mischdurchgänge, die auf den Ziffernpool angewendet werden. Höhere Werte erzeugen mehr zufällige Boards. |
| `EliminateLabel` | Vor der Berechnung der Durchschnittswerte wurde die Ziffer (1-9) von der Tafel entfernt. `0` behält alle Ziffern. |
| `EnableAutoUpdate` | Bauen Sie die Puzzle-Level neu auf, wenn sich das Handelsdatum ändert. |
| `SmaPeriod` | Länge des Indikators SMA, der als Umkehranker verwendet wird. |
| `ThresholdRange` | Maximale absolute Abweichung (ausgedrückt als Bruchteil des Preises), die durch das Puzzle erzeugt wird. |
| `NeutralBand` | Abweichungszone, die eine Abflachung der Position auslöst, wenn der Preis wieder in diese Zone eintritt. |
| `Volume` | Auftragsvolumen für Markteintritte. |
| `CandleType` | Kerzenabonnement, das für Indikatoraktualisierungen verwendet wird. |

## Nutzungshinweise

- Die Strategie reagiert nur auf vollständig ausgebildete Kerzen und ignoriert Nullpreise, wodurch ein stabiles Verhalten bei allen Datenanbietern gewährleistet wird.
- Geben Sie eine 81-stellige Ziffernfolge (ohne Nullen) oder eine Textdatei mit solchen Ziffern an, um ein Sudoku-Brett aus der MetaTrader-Version exakt nachzubilden.
- Wenn Sie ein stationäres Gitter benötigen, deaktivieren Sie `EnableAutoUpdate` und legen Sie explizite Seeds fest. Durch die Aktivierung der Option wird der „automatische Assistent“ MQL gespiegelt, der das Board mit den Benutzeraktionen synchronisiert hält.
- Schwellenwerte werden aus Spaltenstatistiken abgeleitet. Bei asymmetrischen Rätseln sollten Sie erwägen, die dominante Ziffer zu eliminieren, um eine ausgewogene Kauf-/Verkaufsabdeckung aufrechtzuerhalten.

## Unterschiede zum Originalskript

- Alle Funktionen der Benutzeroberfläche (Dialogfenster, Schaltflächen, Diagrammereignisse) werden entfernt. Ihre funktionalen Äquivalente werden als Strategieparameter offengelegt.
- Anstatt Sudoku-Rätsel manuell zu lösen, beeinflusst das Brett algorithmische Handelsstufen. Dieselben Zufallskontrollen bestimmen, wie aggressiv oder konservativ diese Ebenen werden.
- Die StockSharp-Version läuft autonom. Die automatische Aktualisierung reagiert jetzt auf Handelstage und nicht mehr auf Schaltflächenklicks, und die Positionsverwaltung erfolgt über Standardaufrufe `BuyMarket`/`SellMarket`/`ClosePosition`.
