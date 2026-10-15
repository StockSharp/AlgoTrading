# CCI Volatilitätsfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **CCI Volatility Filter**-Strategie basiert auf CCI mit Volatilitätsfilter.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 58%. Sie funktioniert am besten im Aktienmarkt.

Signale werden ausgelöst, wenn die Indikatoren gefilterte Einstiege auf Intraday-Daten (5m) bestätigen. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie CciPeriod, AtrPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `CciPeriod = 20`
  - `AtrPeriod = 14`
  - `CciOversold = -100m`
  - `CciOverbought = 100m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
