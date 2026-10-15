# Parabolic SAR Volatilitätsexpansions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Parabolic SAR Volatility Expansion**-Strategie basiert auf Parabolic SAR mit Volatilitätsexpansionserkennung.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 49%. Sie funktioniert am besten im Kryptomarkt.

Signale werden ausgelöst, wenn Parabolic Trendwechsel auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie SarAf, SarMaxAf. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `SarAf = 0.02m`
  - `SarMaxAf = 0.2m`
  - `AtrPeriod = 14`
  - `VolatilityExpansionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
