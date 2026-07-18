# Duales RSI-Differential
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Duale RSI-Differential vergleicht zwei RSI-Perioden und handelt, wenn ihre Differenz einen Schwellenwert überschreitet. Dieser Doppelperioden-Ansatz versucht, Divergenzen zwischen kurzfristigem und langfristigem Momentum zu erfassen.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: `RSI(Long) - RSI(Short)` < `RsiDiffLevel`.
  - **Short**: `RSI(Long) - RSI(Short)` > `RsiDiffLevel`.
- **Ausstiegskriterien**: Entgegengesetzter Schwellenwert, optionale Halteperiode, optionaler Take Profit / Stop Loss.
- **Stops**: Optionaler Take Profit und Stop Loss (`Condition`).
- **Standardwerte**:
  - `ShortRsiPeriod` = 21
  - `LongRsiPeriod` = 42
  - `RsiDiffLevel` = 5
  - `UseHoldDays` = True
  - `HoldDays` = 5
  - `Condition` = None
  - `TakeProfitPerc` = 15
  - `StopLossPerc` = 10
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long und Short
  - Indikatoren: RSI
  - Komplexität: Grundlegend
  - Risikolevel: Mittel
