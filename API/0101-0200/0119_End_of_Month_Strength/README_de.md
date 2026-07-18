# Monatsend-Stärke-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Monatsend-Stärke beobachtet, dass Aktien häufig in den letzten Handelstagen steigen, da Portfoliomanager ihre Positionen anpassen.
Kaufdruck durch Window Dressing kann einen zuverlässigen Aufwärtstrend vor dem Monatsschluss erzeugen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 94%. Es funktioniert am besten auf dem Aktienmarkt.

Die Strategie kauft kurz vor den letzten Tagen des Monats und steigt am ersten Handelstag des neuen Monats aus, um diese Tendenz zu nutzen.

Stops werden unterhalb des jüngsten Supports platziert, um sich gegen unerwartete Schwäche zu schützen.

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

