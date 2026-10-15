# Fibonacci ATR Fusion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Kaufdruckverhältnisse über mehrere Fibonacci-Perioden mit ATR und verwendet Schwellenwert-Kreuzungen für Ein- und Ausstiege. Optionaler ATR-basierter gestaffelter Take-Profit.

## Details

- **Einstiegskriterien**:
  - **Long**: Gewichteter Durchschnitt kreuzt über `LongEntryThreshold`.
  - **Short**: Gewichteter Durchschnitt kreuzt unter `ShortEntryThreshold`.
- **Ausstiegskriterien**:
  - Gewichteter Durchschnitt kreuzt entgegengesetzte Ausstiegsschwellen oder Positionsumkehr.
- **Indikatoren**:
  - Gewichtete Kaufdruckverhältnisse über ATR.
  - ATR für optionalen Take-Profit.
- **Stops**: Keine.
- **Standardwerte**:
  - `LongEntryThreshold` = 58
  - `ShortEntryThreshold` = 42
  - `LongExitThreshold` = 42
  - `ShortExitThreshold` = 58
  - `Tp1Atr` = 3
  - `Tp2Atr` = 8
  - `Tp3Atr` = 14
  - `Tp1Percent` = 12
  - `Tp2Percent` = 12
  - `Tp3Percent` = 12
- **Filter**:
  - Trendfolge
  - Einzelner Zeitrahmen
  - Indikatoren: ATR
  - Stops: keine
  - Komplexität: Moderat
