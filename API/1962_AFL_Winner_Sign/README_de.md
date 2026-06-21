# AFL Winner Sign-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem AFL WinnerSign-Indikator. Sie wendet einen doppelt geglätteten Stochastik-Oszillator auf eine volumengewichtete Preisreihe an. Eine Long-Position wird eröffnet, wenn die schnelle Stochastik-Linie über die langsame Linie kreuzt, und eine Short-Position wird eröffnet, wenn die schnelle Linie unter die langsame Linie kreuzt.

## Details

- **Einstiegskriterien**:
  - Long: schnelles %K kreuzt über langsames %D
  - Short: schnelles %K kreuzt unter langsames %D
- **Long/Short**: Beide
- **Ausstiegskriterien**: Das entgegengesetzte Signal schließt oder kehrt die Position um
- **Stops**: Prozentbasiert über `StartProtection`
- **Standardwerte**:
  - `Period` = 10
  - `KPeriod` = 5
  - `DPeriod` = 5
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Stochastik-Oszillator
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
