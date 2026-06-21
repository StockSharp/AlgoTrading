# Global Index Spread RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Global Index Spread RSI handelt den E-mini S&P 500, wenn sein Spread zu einem globalen Aktienindex überverkauft wird. Der Spread wird in Prozent gemessen und durch einen kurzperiodigen RSI geleitet. Eine Long-Position wird eröffnet, wenn der RSI unter den Überverkauft-Schwellenwert fällt, und geschlossen, wenn er über den Überkauft-Schwellenwert steigt.

## Details
- **Daten**: Tagesschlusskurse von ES und globalem Index.
- **Einstiegskriterien**:
  - **Long**: Spread-RSI unter `OversoldThreshold`.
- **Ausstiegskriterien**: Spread-RSI über `OverboughtThreshold`.
- **Stops**: Keine.
- **Standardwerte**:
  - `RsiLength` = 2
  - `OversoldThreshold` = 35
  - `OverboughtThreshold` = 78
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: RSI
  - Komplexität: Niedrig
  - Risikolevel: Mittel
