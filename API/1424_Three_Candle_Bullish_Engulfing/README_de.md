# Drei-Kerzen-Bullish-Engulfing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach einem bullischen oder bärischen Drei-Kerzen-Engulfing-Muster. Sie unterstützt optionale RSI-Ausbruch-Einstiege sowie einen Trailing-Stop mit zeitbasierten Ausstiegen.

## Details

- **Einstiegskriterien**:
  - **Long**: Bullische Kerze, kleiner Doji und bullische Engulfing-Kerze.
  - **Short**: Bärische Kerze, kleiner Doji und bärische Engulfing-Kerze.
- **Long/Short**: Beide (Nur-Long-Modus verfügbar).
- **Ausstiegskriterien**:
  - Trailing-Stop, gegenläufiger Kerzenbruch oder Sitzungsende.
- **Stops**: Ja.
- **Standardwerte**:
  - `TrailPerc` = 1.5
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `RsiLength` = 14
  - `RsiLevel` = 80
  - `StopLossPerc` = 5
