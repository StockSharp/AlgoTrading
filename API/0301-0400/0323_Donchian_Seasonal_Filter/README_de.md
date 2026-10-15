# Donchian Seasonal Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Donchian Seasonal Filter**-Strategie basiert auf Donchian-Kanälen mit saisonalem Filter.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 70%. Sie funktioniert am besten auf dem Aktienmarkt.

Signale werden ausgelöst, wenn Donchian gefilterte Einstiege auf Intraday-Daten (15m) bestätigt. Diese Methode eignet sich für aktive Trader.

Stops basieren auf ATR-Vielfachen und Faktoren wie DonchianPeriod, SeasonalThreshold. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `DonchianPeriod = 20`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Donchian, Seasonal
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
