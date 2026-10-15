# Keltner Kalman Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Keltner Kalman Filter**-Strategie basiert auf der Kombination von Keltner-Kanälen mit einem Kalman Filter zur Identifizierung von Trends und Handelsmöglichkeiten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 73%. Sie funktioniert am besten auf dem Kryptomarkt.

Signale werden ausgelöst, wenn Keltner gefilterte Einstiege auf Intraday-Daten (15m) bestätigt. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie EmaPeriod, AtrPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2.0m`
  - `KalmanProcessNoise = 0.01m`
  - `KalmanMeasurementNoise = 0.1m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keltner
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
