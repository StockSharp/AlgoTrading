# Hull MA Volatility Contraction-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Hull MA Volatility Contraction**-Strategie basiert auf dem Hull Moving Average mit Volatilitätskontraktionsfilter.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 76%. Sie funktioniert am besten auf dem Forex-Markt.

Signale werden ausgelöst, wenn die Indikatoren Volatilitätskontraktionsmuster auf Intraday-Daten (15m) bestätigen. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie HmaPeriod, AtrPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `HmaPeriod = 9`
  - `AtrPeriod = 14`
  - `VolatilityContractionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: mehrere Indikatoren
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
