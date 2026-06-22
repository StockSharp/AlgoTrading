# Universelle Trailing-Stop-Hedge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie zur Demonstration verschiedener Trailing-Stop-Techniken zum Schutz offener Positionen.
Sie bietet ATR-, Parabolic-SAR-, gleitender-Durchschnitt-, Prozent- und Festpips-basierte Trailing-Stops.
Ein einfacher Einstieg basierend auf der Kerzenrichtung wird ausschließlich zu Lernzwecken verwendet.

## Details

- **Einstiegskriterien**: Long, wenn die Kerze über der Eröffnung schließt; Short, wenn sie darunter schließt
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trailing-Stop ausgelöst
- **Stops**: ATR, Parabolic SAR, Gleitender Durchschnitt, Prozentgewinn oder feste Pips je nach gewähltem Modus
- **Standardwerte**:
  - `Mode` = `TrailingModes.Atr`
  - `Delta` = 10
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `MaPeriod` = 34
  - `PercentProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: ATR, Parabolic SAR, SMA
  - Stops: Trailing-Stop
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
