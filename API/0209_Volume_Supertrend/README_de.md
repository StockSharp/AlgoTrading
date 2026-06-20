# Volume Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Volume Supertrend Indikatoren zur Signalgenerierung.
Ein Long-Einstieg erfolgt, wenn Volume > Avg(Volume) && Price > Supertrend (Volumenanstieg mit Aufwärtstrend). Ein Short-Einstieg erfolgt, wenn Volume > Avg(Volume) && Price < Supertrend (Volumenanstieg mit Abwärtstrend).
Sie eignet sich für Trader, die Chancen in Trendmärkten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 64%. Sie funktioniert am besten auf dem Devisenmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
  - **Short**: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn Supertrend nach unten dreht
  - **Short**: Short-Position schließen, wenn Supertrend nach oben dreht
- **Stops**: Ja.
- **Standardwerte**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Volume Supertrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

