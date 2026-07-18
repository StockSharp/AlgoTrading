# Geglättete Heiken Ashi Nur-Long-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Long-Strategie mit geglätteten Heikin-Ashi-Kerzen. Kauft, wenn die geglättete Kerze von Rot nach Grün wechselt, und beendet die Position, wenn sie wieder Rot wird.

## Details

- **Einstiegskriterien**: Geglättetes HA wechselt von Rot nach Grün
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Geglättetes HA wird Rot
- **Stops**: Keine
- **Standardwerte**:
  - `EmaLength` = 10
  - `SmoothingLength` = 10
