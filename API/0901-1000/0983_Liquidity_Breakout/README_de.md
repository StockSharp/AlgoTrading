# Liquiditäts-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus einer kürzlichen Kursrange, die durch Pivot-Hochs und -Tiefs definiert ist. Eine Position wird eröffnet, wenn der Kurs über die vorherigen Range-Extremwerte schließt. Der optionale Stop-Loss kann eine SuperTrend-Linie oder einen festen Prozentsatz verwenden.

## Details

- **Einstiegskriterien**:
  - `Schluss > vorheriges Range-Hoch` → Long
  - `Schluss < vorheriges Range-Tief` → Short
- **Long/Short**: Konfigurierbar (Long, Short, Beide).
- **Ausstiegskriterien**: Gegenläufiger Ausbruch oder Stop-Loss.
- **Stops**: SuperTrend oder fester Prozentsatz.
- **Standardwerte**:
  - `PivotLength` = 12
  - `StopLoss` = SuperTrend
  - `FixedPercentage` = 0.1
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, SuperTrend
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: 1h
