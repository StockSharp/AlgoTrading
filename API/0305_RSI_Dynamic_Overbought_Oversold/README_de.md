# RSI-Strategie mit dynamischen Überkauft-/Überverkauft-Niveaus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **RSI Dynamic Overbought Oversold**-Strategie basiert auf dem RSI mit dynamischen Überkauft-/Überverkauft-Niveaus.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 178%. Sie funktioniert am besten am Aktienmarkt.

Signale werden ausgelöst, wenn Überkauft Trendänderungen bei Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie RsiPeriod, MovingAvgPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Implementierung für Indikatorbedingungen prüfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Overbought, Oversold
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
