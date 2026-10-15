# Saisonalitätsbereinigter-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Seasonality Adjusted Momentum**-Strategie basiert auf einem Momentum-Indikator, der um die Saisonalitätsstärke bereinigt wird.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 172%. Sie funktioniert am besten am Devisenmarkt.

Gehandelt wird auf **abgeschlossenen 5-Minuten-Kerzen**, nicht Tagesdaten. `M = Momentum(14)`, `A = SMA(14) von M`, `s = score[candle.OpenTime.Month]`. Aus Flat Long bei `s >= 0.5` und `M > A`, Short bei `s <= -0.5` und `M < A`. Long-Ausgang bei `s < 0.5` oder `M < A`; Short-Ausgang bei `s > -0.5` oder `M > A`. Nach Signalen warten 120 abgeschlossene Kerzen. Schutz durch **nativen 2%-Stop**, nicht ATR.

Monatliche s-Werte Jan–Dez: `0.8, 0.2, 0.5, 0.7, 0.3, -0.2, -0.3, -0.4, -0.7, 0.4, 0.6, 0.9`. BTC/TON enthält nur März (s=0.5): Long erlaubt, neue Shorts verboten. Die genannten 172% sind **nicht verifiziert**.

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
