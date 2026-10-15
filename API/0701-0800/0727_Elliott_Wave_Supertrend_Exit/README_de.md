# Elliott Wave Supertrend-Ausstieg-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die bei ZigZag-ähnlichen Umkehrungen einsteigt und bei Richtungswechseln des Supertrend mit einem festen prozentualen Stop-Loss aussteigt.

## Details

- **Einstiegskriterien**:
  - Long: Preis bildet ein lokales Tief
  - Short: Preis bildet ein lokales Hoch
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Supertrend-Richtungswechsel oder Stop-Loss-Level
- **Stops**: Fester Prozentsatz vom Einstiegspreis
- **Standardwerte**:
  - `WaveLength` = 4
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3
  - `StopLossPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, SuperTrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
