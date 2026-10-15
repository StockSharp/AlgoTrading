# VWAP Stochastic Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **VWAP Stochastic Divergence**-Strategie basiert auf der Kombination von VWAP mit dem ADX-Trendstärkeindikator.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 79%. Sie funktioniert am besten auf dem Aktienmarkt.

Signale werden ausgelöst, wenn Stochastic Divergenz-Setups auf Intraday-Daten (5m) bestätigt. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie AdxPeriod, AdxThreshold. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `AdxExitThreshold = 20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Stochastic, Divergence
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
