# Trading-Tools-Bibliothek-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Einfache SMA-Kreuzungs-Strategie mit RSI-Filter und Einstiegs-Abkühlzeit.

## Details
- **Einstiegskriterien**:
  - **Long**: schnelle SMA kreuzt die langsame SMA von unten und RSI liegt unter `RsiUpper`
  - **Short**: schnelle SMA kreuzt die langsame SMA von oben und RSI liegt über `RsiLower`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Umgekehrtes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `ShortLength` = 10
  - `LongLength` = 30
  - `RsiLength` = 14
  - `CooldownBars` = 3
  - `RsiUpper` = 60
  - `RsiLower` = 40
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
