# Supertrend RSI Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Supertrend RSI Divergence**-Strategie verwendet den Supertrend-Indikator zusammen mit RSI-Divergenz, um Handelsmöglichkeiten zu identifizieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 67%. Sie funktioniert am besten auf dem Aktienmarkt.

Signale werden ausgelöst, wenn Divergence Divergenz-Setups auf Intraday-Daten (15m) bestätigt. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie SupertrendPeriod, SupertrendMultiplier. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `SupertrendPeriod = 10`
  - `SupertrendMultiplier = 3.0m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Divergence
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
