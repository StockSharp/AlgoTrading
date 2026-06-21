# Moving-Average-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet eine Long-Position, wenn ein kurzer gleitender Durchschnitt einen langen gleitenden Durchschnitt des ausgewählten Preistyps von unten kreuzt. Die Position wird geschlossen, wenn der kurze Durchschnitt den langen wieder von oben kreuzt.

## Details
- **Einstiegskriterien:** Kurze MA kreuzt die lange MA nach oben.
- **Ausstiegskriterien:** Kurze MA kreuzt die lange MA nach unten.
- **Indikatoren:** SMA, EMA, DEMA, TEMA, WMA, VWMA.
- **Preisquelle:** Close, High, Open, Low, Typical, Center.
- **Stops:** Keine.
- **Standardwerte:**
  - `MaType` = EMA
  - `ShortLength` = 1
  - `LongLength` = 20
  - `PriceType` = Typical
  - `CandleType` = 1 minute
- **Filter:**
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Gleitender Durchschnitt
  - Stops: Nein
  - Komplexität: Einfach
  - Risikolevel: Mittel
