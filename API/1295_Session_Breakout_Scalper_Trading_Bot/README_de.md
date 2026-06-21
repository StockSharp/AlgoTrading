# Sitzungs-Ausbruch-Scalping-Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Session Breakout Scalper handelt Ausbrüche aus der Kursrange, die sich während einer vordefinierten Sitzung gebildet hat.

## Details

- **Einstiegskriterien**: Kurs bricht über das Sitzungshoch oder unter das Sitzungstief
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss
- **Stops**: ATR oder fest
- **Standardwerte**:
  - `SessionStart` = 01:00
  - `SessionEnd` = 02:00
  - `TakeProfit` = 100
  - `StopLoss` = 50
  - `UseAtrStop` = true
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `CandleType` = time frame 1 minute
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: ATR
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
