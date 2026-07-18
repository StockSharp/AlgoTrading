# Moving-Average-Crossover-Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt, wenn ein schneller exponentieller gleitender Durchschnitt einen mittleren kreuzt, mit optionaler Bestätigung durch einen langsamen MA und das MACD-Histogramm. Verwendet ATR-basierte Stop-Loss- und Take-Profit-Werte und kann bei einem sekundären MA-Kreuz aussteigen.

## Details

- **Einstiegskriterien**:
  - Schnelle EMA kreuzt die mittlere EMA nach oben für Long, nach unten für Short.
  - Optional: Preis über/unter der langsamen EMA.
  - Optional: MACD-Histogramm über/unter null.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss und Take-Profit oder optionaler Ausstiegs-MA-Kreuz.
- **Stops**: Ja, ATR-Vielfache.
- **Standardwerte**:
  - `FastPeriod` = 5
  - `MediumPeriod` = 10
  - `SlowPeriod` = 50
  - `FastExitPeriod` = 5
  - `MediumExitPeriod` = 10
  - `AtrPeriod` = 14
  - `AtrStopMultiplier` = 1.4
  - `AtrTakeMultiplier` = 3.2
  - `EnableSlow` = true
  - `EnableMacd` = true
  - `EnableLong` = true
  - `EnableShort` = false
  - `EnableCrossExit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Konfigurierbar
  - Indikatoren: EMA, MACD, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 1m (Standard)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
