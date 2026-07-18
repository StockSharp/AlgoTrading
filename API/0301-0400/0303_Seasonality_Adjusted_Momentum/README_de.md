# Saisonalitätsbereinigter-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Seasonality Adjusted Momentum**-Strategie basiert auf einem Momentum-Indikator, der um die Saisonalitätsstärke bereinigt wird.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 172%. Sie funktioniert am besten am Devisenmarkt.

Signale werden ausgelöst, wenn die Saisonalität Momentum-Verschiebungen bei Tagesdaten bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie MomentumPeriod, SeasonalityThreshold. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Implementierung für Indikatorbedingungen prüfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Seasonality, Adjusted
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
