# StepMA NRTR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolgestrategie basierend auf dem StepMA NRTR-Indikator. Der Indikator kombiniert einen stufenweisen gleitenden Durchschnitt mit einem Nick Rar Trend-Umkehrmechanismus und erzeugt Kauf- oder Verkaufssignale bei Trendwechseln.

## Details

- **Einstiegskriterien**: StepMA NRTR Kauf-/Verkaufssignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes StepMA NRTR-Signal
- **Stops**: Keine
- **Standardwerte**:
  - `Length` = 10
  - `Kv` = 1
  - `StepSize` = 0
  - `UseHighLow` = true
  - `CandleType` = Zeitrahmen 1 Stunde
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: StepMA NRTR
  - Stops: Keine
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
