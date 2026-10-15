# Flex ATR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Flex ATR wählt dynamisch EMA-, RSI- und ATR-Perioden basierend auf dem aktuellen Zeitrahmen. Ein Long-Trade wird eröffnet, wenn die schnelle EMA die langsame von unten kreuzt und RSI über 50 liegt. Ein Short-Trade wird beim umgekehrten Crossover mit RSI unter 50 ausgelöst. Ausstiege verwenden ATR-basierte Stops oder einen optionalen Trailing-Stop.

## Details

- **Einstiegskriterien**: Schnelle EMA vs. langsame EMA Kreuzung mit RSI-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Stop oder Ziel, optionaler Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrStopMult` = 3
  - `AtrProfitMult` = 1.5
  - `EnableTrailingStop` = true
  - `AtrTrailMult` = 1
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ATR
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
