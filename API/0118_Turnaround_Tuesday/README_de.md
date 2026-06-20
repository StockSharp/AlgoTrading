# Turnaround Tuesday Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Turnaround Tuesday beschreibt die Tendenz von Märkten, die am Montag gefallen sind, am nächsten Tag zu erholen.
Der Effekt wird oft darauf zurückgeführt, dass Trader nach dem Wochenende überreagieren und dann den Kurs umkehren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 91%. Es funktioniert am besten auf dem Aktienmarkt.

Diese Strategie kauft zur Eröffnung des Dienstags, wenn der Montag rückläufig war, und hält die Position nur für die Session oder bis ein moderates Gewinnziel erreicht wird.

Stops sind eng gesetzt, um bei anhaltender Schwäche zu schützen, falls der Aufschwung ausbleibt.

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

