# Double Supertrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Double Supertrend verwendet zwei ATR-basierte gleitende Durchschnitte mit
unterschiedlichen Perioden und Multiplikatoren. Die erste Linie legt die
Handelsrichtung fest, während die zweite als Ziel oder Trailing-Ausstieg fungieren
kann. Diese Kombination ermöglicht flexibles Trendfolgen mit definierten Gewinn- und
Risikoparametern.

Wenn der Preis über beide Linien steigt und die Strategie auf Long-Handel eingestellt
ist, wird eine Position eröffnet. Für Short-Trades sind die Bedingungen gespiegelt.
Ausstiege hängen vom gewählten Take-Profit-Typ oder einem prozentualen Stop-Loss ab.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**: Preis kreuzt Supertrend-Linien in der erlaubten `Direction`.
- **Ausstiegskriterien**: Gegenseitiger Linienbruch, Take-Profit (`TPType`/`TPPercent`) oder Stop-Loss (`SLPercent`).
- **Stops**: Prozentualer Stop basierend auf `SLPercent`.
- **Standardwerte**:
  - `ATRPeriod1` = 10
  - `Factor1` = 3.0
  - `ATRPeriod2` = 20
  - `Factor2` = 5.0
  - `Direction` = "Long"
  - `TPType` = "Supertrend"
  - `TPPercent` = 1.5
  - `SLPercent` = 10.0
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Konfigurierbar
  - Indikatoren: ATR‑based Supertrend
  - Komplexität: Fortgeschritten
  - Risikolevel: Mittel
