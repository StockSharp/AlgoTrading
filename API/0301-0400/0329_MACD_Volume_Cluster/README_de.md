# MACD Volume Cluster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **MACD Volume Cluster**-Strategie basiert auf der Analyse von MACD-Volumen-Clustern.

Signale werden ausgelöst, wenn die Indikatoren Trendwechsel auf Intraday-Daten (5m) bestätigen. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie FastMacdPeriod, SlowMacdPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `FastMacdPeriod = 12`
  - `SlowMacdPeriod = 26`
  - `MacdSignalPeriod = 9`
  - `VolumePeriod = 20`
  - `VolumeDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere Indikatoren
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
