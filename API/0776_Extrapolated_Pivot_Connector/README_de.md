# Extrapolated Pivot Connector-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verbindet aktuelle Pivot-Hochs und -Tiefs, um Support- und Widerstandslinien aufzubauen. Ein Long-Signal tritt auf, wenn der Preis über der Widerstandslinie schließt, während ein Short-Signal ausgelöst wird, wenn der Preis unter die Supportlinie fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt über die Widerstandslinie.
  - **Short**: Schlusskurs kreuzt unter die Supportlinie.
- **Ausstiegskriterien**:
  - Gegensätzliches Signal oder Positionsumkehr.
- **Indikatoren**:
  - Pivot-basierte Support-/Widerstandslinien.
- **Stops**: Keine.
- **Standardwerte**:
  - `PivotLength` = 100
  - `HighStart` = 1
  - `HighEnd` = 0
  - `LowStart` = 1
  - `LowEnd` = 0
- **Filter**:
  - Trendfolge
  - Einzelner Zeitrahmen
  - Indikatoren: Pivot-Linien
  - Stops: keine
  - Komplexität: Niedrig
