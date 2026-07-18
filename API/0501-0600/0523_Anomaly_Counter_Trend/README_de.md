# Anomalie-Gegentrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Algorithmus erkennt starke prozentuale Bewegungen über ein kurzes Fenster und handelt dagegen. Wenn der Preis über den Schwellenwert steigt, verkauft er; wenn der Preis unter den Schwellenwert fällt, kauft er. Stop-Loss und Take-Profit werden in Ticks gesetzt.

## Details

- **Einstiegskriterien**: Prozentuale Veränderung über das Lookback-Fenster überschreitet den Schwellenwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `PercentageThreshold` = 1
  - `LookbackMinutes` = 30
  - `StopLossTicks` = 100
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Gegentrend
  - Richtung: Beide
  - Indikatoren: Preis
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
