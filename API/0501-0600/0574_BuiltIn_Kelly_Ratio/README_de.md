# Integriertes Kelly-Verhältnis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kanal-Ausbruch-Strategie mit einem gleitenden Durchschnitt und ATR-Bändern, bei der die Positionsgröße auf dem Kelly-Verhältnis basiert.

## Details

- **Einstiegskriterien**: Preis kreuzt über oder unter ATR-basierte Bänder.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Optionaler Take-Profit und Stop-Loss.
- **Stops**: Optional.
- **Standardwerte**:
  - `Length` = 20
  - `Multiplier` = 1
  - `AtrLength` = 10
  - `UseEma` = true
  - `UseKelly` = true
  - `UseTakeProfit` = false
  - `UseStopLoss` = false
  - `TakeProfit` = 10
  - `StopLoss` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MA, ATR
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
