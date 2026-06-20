# Santa Claus Rally Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Santa Claus Rally beschreibt die Tendenz von Aktien, in der letzten Dezemberwoche bis zu den ersten zwei Handelstagen im Januar zu steigen.
Urlaubsoptimismus und Jahresend-Positionierungen können diesen kurzen Stärke-Schub antreiben.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 100%. Es funktioniert am besten auf dem Forex-Markt.

Die Strategie kauft zu Beginn des Zeitraums und schließt nach dem zweiten Handelstag des neuen Jahres, um den saisonalen Aufschwung zu erfassen.

Stops werden klein gehalten, um große Verluste zu vermeiden, falls der Markt im Zeitfenster nicht steigt.

## Details

- **Einstiegskriterien**: Kalendereffekt-Auslöser
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Beide
  - Indikatoren: Saisonalität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

