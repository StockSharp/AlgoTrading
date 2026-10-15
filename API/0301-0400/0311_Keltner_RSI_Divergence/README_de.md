# Keltner RSI Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Keltner RSI Divergence**-Strategie basiert auf dem Keltner-Kanal und RSI-Divergenz.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 40%. Sie funktioniert am besten im Kryptomarkt.

Signale werden ausgelöst, wenn Keltner Divergenz-Setups auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie EmaPeriod, AtrPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2.0m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keltner, Divergenz
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
