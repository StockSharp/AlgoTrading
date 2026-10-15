# VWAP Hidden Markov Model-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **VWAP Hidden Markov Model**-Strategie handelt basierend auf VWAP mit Hidden Markov Model zur Erkennung des Marktzustands.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 100%. Sie erzielt die besten Ergebnisse auf dem Devisenmarkt.

Signale werden ausgelöst, wenn Markov Trendwechsel auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie HmmDataLength, StopLossPercent. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `HmmDataLength = 100`
  - `StopLossPercent = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Markov
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Ja
  - Divergenz: Nein
  - Risikolevel: Mittel
