# Universum 3.0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Auf dem DeMarker-Oszillator basierende Strategie, die bei jeder abgeschlossenen Kerze Positionen öffnet und das Volumen nach dem Martingale-Schema anpasst.

## Details

- **Einstiegskriterien**:
  - Long: `DeMarker > 0.5`
  - Short: `DeMarker < 0.5`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Positionen werden durch Take Profit oder Stop Loss geschlossen
- **Stops**: Absolute Punkte über `TakeProfitPoints` und `StopLossPoints`
- **Standardwerte**:
  - `DemarkerPeriod` = 10
  - `TakeProfitPoints` = 50m
  - `StopLossPoints` = 50m
  - `InitialVolume` = 1m
  - `LossesLimit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: DeMarker
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
