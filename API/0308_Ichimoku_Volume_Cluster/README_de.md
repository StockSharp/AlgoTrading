# Ichimoku-Volumencluster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Ichimoku Volume Cluster**-Strategie basiert auf der Ichimoku-Wolke mit Volumencluster-Bestätigung.

Signale werden ausgelöst, wenn die Indikatoren Trendänderungen bei Intraday-Daten (1h) bestätigen. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie TenkanPeriod, KijunPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Implementierung für Indikatorbedingungen prüfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `VolumeAvgPeriod = 20`
  - `VolumeStdDevMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromHours(1).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: mehrere Indikatoren
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
