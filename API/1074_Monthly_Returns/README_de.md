# Monatliche Rendite-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verfolgt Pivot-Hochs und -Tiefs, um Ausbrüche zu handeln, und berechnet zusammengesetzte monatliche und jährliche Renditen des Strategie-Kapitals.

## Details

- **Einstiegskriterien**: Kaufen, wenn der Preis über das letzte Pivot-Hoch ausbricht; verkaufen, wenn der Preis unter das letzte Pivot-Tief fällt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Positionen kehren sich bei entgegengesetzten Signalen um.
- **Stops**: Keine.
- **Standardwerte**:
  - `LeftBars` = 2
  - `RightBars` = 1
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long & Short
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
