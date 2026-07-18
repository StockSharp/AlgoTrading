# Hammer + EMA-Strategie mit Tick-basiertem SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Hammer- und umgekehrte Hammer-Kerzenmuster mit einem EMA-Trendfilter und Tick-basiertem Risikomanagement.

## Details

- **Einstiegskriterien**: Hammer über EMA oder umgekehrter Hammer unter EMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Tick-basierter Take-Profit oder Stop-Loss.
- **Stops**: Tick-basiert.
- **Standardwerte**:
  - `EmaLength` = 50
  - `StopLossTicks` = 1
  - `TakeProfitTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: EMA, Hammer, Inverted Hammer
  - Stops: Tick-basiert
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
