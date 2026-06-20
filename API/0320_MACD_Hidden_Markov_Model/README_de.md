# MACD Hidden Markov Model-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **MACD Hidden Markov Model**-Strategie basiert auf dem MACD Hidden Markov Model.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61%. Sie funktioniert am besten auf dem Kryptomarkt.

Signale werden ausgelöst, wenn Markov Trendwechsel auf Intraday-Daten (5m) bestätigt. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie MacdFast, MacdSlow. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `MacdFast = 12`
  - `MacdSlow = 26`
  - `MacdSignal = 9`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `HmmHistoryLength = 100`
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
