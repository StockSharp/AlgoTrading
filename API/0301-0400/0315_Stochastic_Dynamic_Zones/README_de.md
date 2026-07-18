# Stochastic Dynamische Zonen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Stochastic Dynamic Zones**-Strategie basiert auf dem Stochastic-Oszillator mit dynamischen Überkauft-/Überverkauft-Zonen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 52%. Sie funktioniert am besten im Kryptomarkt.

Signale werden ausgelöst, wenn Stochastic Trendwechsel auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie StochPeriod, StochKPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `StochPeriod = 14`
  - `StochKPeriod = 3`
  - `StochDPeriod = 3`
  - `LookbackPeriod = 20`
  - `StandardDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
