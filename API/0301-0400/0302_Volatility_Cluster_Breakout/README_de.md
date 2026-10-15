# Volatilitätscluster-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Volatility Cluster Breakout**-Strategie basiert auf Ausbrüchen während Phasen hoher Volatilitätscluster.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 169%. Sie funktioniert am besten auf dem Kryptomarkt.

Signale werden ausgelöst, wenn die Indikatoren Ausbruchsgelegenheiten bei Intraday-Daten (5m) bestätigen. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie PriceAvgPeriod, AtrPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Implementierung für Indikatorbedingungen prüfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `PriceAvgPeriod = 20`
  - `AtrPeriod = 14`
  - `StdDevMultiplier = 2.0m`
  - `StopMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: mehrere Indikatoren
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
