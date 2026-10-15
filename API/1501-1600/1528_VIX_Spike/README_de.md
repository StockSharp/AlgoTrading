# VIX-Spike-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft, wenn der VIX-Index seinen gleitenden Durchschnitt um ein Vielfaches der Standardabweichung übersteigt, und schließt nach einer festen Anzahl von Bars.

## Details

- **Einstiegskriterien**: VIX > Mittelwert + StdDevMultiplier * Standardabweichung.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Ausstieg nach `ExitPeriods` Bars.
- **Stops**: Ja.
- **Standardwerte**:
  - `StdDevLength` = 15
  - `StdDevMultiplier` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VixSecurity` = "CBOE:VIX"
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Nur Long
  - Indikatoren: SMA, StdDev
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
