# VWAP RSI Scalper-Strategie FINAL v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Scalping-Strategie, die VWAP und RSI mit ATR-basierten Ausstiegen und täglichen Trade-Limits kombiniert.

## Details

- **Einstiegskriterien**: Preis relativ zu VWAP und EMA mit RSI-Schwellenwerten innerhalb der Handelssitzung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Stop und Ziel.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: VWAP, RSI, EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
