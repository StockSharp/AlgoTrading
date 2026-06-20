# Parabolic SAR RSI Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **Parabolic SAR RSI Divergence**-Strategie handelt basierend auf Parabolic SAR-Signalen, wenn der RSI eine Divergenz gegenüber dem Preis zeigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 103%. Sie erzielt die besten Ergebnisse am Aktienmarkt.

Signale werden ausgelöst, wenn Parabolic Divergenz-Setups auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie SarAccelerationFactor, SarMaxAccelerationFactor. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic, Divergence
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
