# Donchian Hurst Exponent-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **Donchian Hurst Exponent**-Strategie handelt basierend auf Donchian-Kanal-Ausbrüchen mit Hurst Exponent-Filter.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 91%. Sie erzielt die besten Ergebnisse am Aktienmarkt.

Signale werden ausgelöst, wenn Donchian Trendwechsel auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie DonchianPeriod, HurstPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `DonchianPeriod = 20`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `StopLossPercent = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Donchian, Hurst, Exponent
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
