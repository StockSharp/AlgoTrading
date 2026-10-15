# Bollinger Kalman Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Bollinger Kalman Filter**-Strategie basiert auf dem Bollinger Kalman Filter.

Signale werden ausgelöst, wenn Bollinger gefilterte Einstiege auf Intraday-Daten (5m) bestätigt. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie BollingerLength, BollingerDeviation. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `BollingerLength = 20`
  - `BollingerDeviation = 2.0m`
  - `KalmanQ = 0.01m`
  - `KalmanR = 0.1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
