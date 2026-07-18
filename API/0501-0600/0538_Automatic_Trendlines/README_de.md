# Strategie mit automatischen Trendlinien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Baut dynamische Unterstützungs- und Widerstandstrendlinien auf, indem aktuelle Pivot-Hochs und -Tiefs verbunden werden. Ein Long-Signal entsteht, wenn der Preis über die Widerstandslinie schließt, während ein Short-Signal ausgelöst wird, wenn der Preis unter die Unterstützungslinie fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss kreuzt über die Widerstands-Trendlinie.
  - **Short**: Schluss kreuzt unter die Unterstützungs-Trendlinie.
- **Ausstiegskriterien**:
  - Gegensignal oder Positionsumkehr.
- **Indikatoren**:
  - Pivot-basierte Trendlinien.
- **Stops**: Keine.
- **Standardwerte**:
  - `LeftBars` = 100
  - `RightBars` = 15
- **Filter**:
  - Trendfolge
  - Einzelner Zeitrahmen
  - Indikatoren: Pivot-Trendlinien
  - Stops: keine
  - Komplexität: Niedrig
