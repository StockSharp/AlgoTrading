# Overnight-Effekt-Strategie für Hochvolatilität im Krypto-Bereich
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die an hochvolatilen Abenden eine Long-Position eingeht und vor Mitternacht schließt. Die Volatilität wird durch die Standardabweichung der Log-Returns über einen konfigurierbaren Zeitraum gemessen und mit dem Median der historischen Volatilität verglichen.

## Details

- **Einstiegskriterien**:
  - `currentHour == EntryHour && highVolatility` wenn `UseVolatilityFilter`
  - `currentHour == EntryHour` wenn Filter deaktiviert
- **Long/Short**: Long
- **Stops**: Keine
- **Standardwerte**:
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Zeitbasiert
  - Richtung: Nur Long
  - Indikatoren: StandardDeviation, Median
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
