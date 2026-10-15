# Januar-Effekt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Januar-Effekt beobachtet, dass Small-Cap-Aktien zu Jahresbeginn häufig besser abschneiden, möglicherweise aufgrund von Steuerverkäufen im Dezember.
Trader versuchen, diese Tendenz zu nutzen, indem sie Ende Dezember kaufen und nach den ersten Januarwochen verkaufen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 103%. Sie funktioniert am besten am Aktienmarkt.

Die Strategie folgt diesem Zeitplan, tritt gegen Jahresende ein und steigt Mitte Januar aus.

Ein Stop-Loss stellt sicher, dass Verluste beherrschbar bleiben, wenn der Effekt ausbleibt.

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

