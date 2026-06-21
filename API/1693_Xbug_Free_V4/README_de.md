# Xbug Free V4 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Positionen, wenn ein gleitender Durchschnitt des Medianpreises den Medianpreis selbst kreuzt. Ein symmetrischer Take-Profit und Stop-Loss werden in einem festen Abstand vom Einstiegspreis platziert.

## Details

- **Einstiegskriterien**:
  - Long: Der gleitende Durchschnitt liegt über dem Medianpreis und lag vor zwei Kerzen darunter
  - Short: Der gleitende Durchschnitt liegt unter dem Medianpreis und lag vor zwei Kerzen darüber
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Take-Profit im Abstand `StopPoints` über/unter dem Einstieg
  - Stop-Loss im Abstand `StopPoints` auf der gegenüberliegenden Seite
- **Stops**: Ja
- **Standardwerte**:
  - `MaPeriod` = 19
  - `StopPoints` = 270
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Crossover
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
