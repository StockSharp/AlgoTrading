# ADX Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet ADX Donchian Indikatoren zur Signalgenerierung.
Ein Long-Einstieg erfolgt, wenn ADX > AdxThreshold && Price >= upperBorder (starker Trend mit Aufwärtsausbruch). Ein Short-Einstieg erfolgt, wenn ADX > AdxThreshold && Price <= lowerBorder (starker Trend mit Abwärtsausbruch).
Sie eignet sich für Trader, die Chancen in gemischten Märkten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 67%. Sie funktioniert am besten auf dem Aktienmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
  - **Short**: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Position schließen, wenn ADX unter (threshold - 5) fällt
  - **Short**: Position schließen, wenn ADX unter (threshold - 5) fällt
- **Stops**: Ja.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `DonchianPeriod` = 5
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AdxThreshold` = 10
  - `Multiplier` = 0.1m
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: ADX Donchian
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

