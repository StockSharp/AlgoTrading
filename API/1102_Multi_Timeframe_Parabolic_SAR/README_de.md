# Strategie Multi-Timeframe Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Parabolic-SAR-Signale aus mehreren Zeitrahmen. Long-Trades werden ausgelöst, wenn der Kurs über den durch die Parameter gewählten SAR-Niveaus bleibt. Short-Trades erscheinen, wenn der Kurs unter die gewählten SARs fällt. Optionaler Stop-Loss, Trailing-Stop und Take-Profit sind verfügbar.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs über SAR gemäß der Einstellung `LongSource`.
  - **Short**: Kurs unter SAR gemäß der Einstellung `ShortSource`.
- **Ausstiegskriterien**:
  - Gegensätzliches SAR-Crossover oder Auslösung des Schutzes.
- **Indikatoren**:
  - Parabolic SAR im aktuellen Zeitrahmen
  - Optionaler Parabolic SAR in höheren und niedrigeren Zeitrahmen
- **Stops**: Optionaler Stop-Loss, Trailing-Stop, Take-Profit über StartProtection.
- **Standardwerte**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `StopLossPercent` = 1
  - `TrailingPercent` = 0.5
  - `TakeProfitPercent` = 2
- **Filter**:
  - Zeitrahmen: Haupt 5m, höher 1d, niedriger 1m
  - Indikatoren: Parabolic SAR
  - Stops: optional
  - Komplexität: Moderat
