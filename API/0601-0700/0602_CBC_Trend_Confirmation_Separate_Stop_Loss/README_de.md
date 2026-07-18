# CBC-Strategie mit Trendbestätigung und getrenntem Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den CBC-Zustand (Color Bar Change), um Wendepunkte zu erkennen, wenn der Preis das Hoch oder Tief der vorherigen Kerze durchbricht. Einstiege erfordern eine Trendbestätigung über EMA und VWAP und sind auf ein Handelssitzungsfenster beschränkt. Ausstiege verwenden ein ATR-basiertes Gewinnziel und die Extrema der vorherigen Kerze als Stop-Loss-Niveaus.

## Details

- **Einstiegskriterien**: CBC-Wendungen, optionaler Filter für starke Wendungen, langsame EMA relativ zu VWAP, innerhalb der Handelszeiten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Take-Profit mit ATR-Multiplikator, Stop-Loss am Hoch/Tief der vorherigen Kerze.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrLength` = 14
  - `ProfitTargetMultiplier` = 1.0m
  - `StrongFlipsOnly` = true
  - `EntryStartHour` = 10
  - `EntryEndHour` = 15
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, VWAP, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
