# Resampling-Filterpaket-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sampelt den Preis alle N Bars und glättet ihn mit einem gleitenden Durchschnitt. Sie geht long, wenn der gefilterte Wert steigt und der Preis darüber liegt, und short, wenn der gefilterte Wert fällt und der Preis darunter liegt.

## Details
- **Einstiegskriterien**:
  - **Long**: Filterneigung ist aufwärts gerichtet und Schlusskurs liegt über dem Filter.
  - **Short**: Filterneigung ist abwärts gerichtet und Schlusskurs liegt unter dem Filter.
- **Ausstiegskriterien**: entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `BarsPerSample` = 5
  - `MovingAverageType` = EMA
  - `MaPeriod` = 9
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: Gleitender Durchschnitt
  - Komplexität: Einfach
  - Risikolevel: Mittel
