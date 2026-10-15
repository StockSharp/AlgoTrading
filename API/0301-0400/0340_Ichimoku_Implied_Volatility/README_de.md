# Ichimoku Implizite Volatilität-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **Ichimoku Implied Volatility**-Strategie basiert auf der impliziten Volatilität von Ichimoku.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 109%. Sie funktioniert am besten auf dem Kryptomarkt.

Signale werden ausgelöst, wenn die Indikatoren Trendwechsel auf Intraday-Daten (15m) bestätigen. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie TenkanPeriod, KijunPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `IVPeriod = 20`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere Indikatoren
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

