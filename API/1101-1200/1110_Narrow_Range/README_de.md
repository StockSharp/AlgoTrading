# Narrow-Range-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche nach einer Inside-Bar, bei der die aktuelle Kerzenlänge schmaler ist als die Referenzkerze `Length` Perioden zuvor. Stop-Orders werden am Referenzhoch und -tief platziert, mit einem Take-Profit in Höhe der Referenzspanne und einem Stop-Loss als Prozentsatz dieser Spanne.

## Details

- **Einstiegskriterien**:
  - Long: Kurs bricht über das Referenzhoch nach einer Narrow-Range-Bar aus
  - Short: Kurs bricht unter das Referenztief nach einer Narrow-Range-Bar aus
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Take-Profit bei der Referenzspanne
  - Stop-Loss als Prozentsatz der Spanne
- **Stops**: Ja
- **Standardwerte**:
  - `Length` = 4
  - `StopLossPercent` = 0.35m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
