# Quantitative Trend-Strategie — Long im Aufwärtstrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn der Preis über das zuletzt erkannte Pivot-Hoch schließt, das über konfigurierbare Rückblickfenster ermittelt wird. Unterstützungs- und Widerstandsniveaus werden aus Pivot-Hochs und -Tiefs abgeleitet. Positionen sind durch prozentbasierte Take-Profit- und Stop-Loss-Aufträge geschützt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs überschreitet das letzte Pivot-Hoch.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Schlusskurs unterschreitet das letzte Pivot-Tief.
  - Das letzte Pivot-Hoch wird niedriger als das letzte Pivot-Tief.
- **Stops**: Ja, prozentbasierter Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `PivotLeft` = 46
  - `PivotRight` = 32
  - `StopLossPercent` = 1.75
  - `TakeProfitPercent` = 2
