# ZMFX Stolid 5a EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Multi-Timeframe-Trendfolgestrategie, die bei durch RSI und Stochastic bestätigten Rücksetzern einsteigt.
Das System identifiziert den Haupttrend anhand des 4-Stunden-Stochastic und stündlicher geglätteter gleitender Durchschnitte.
Positionen werden bei Kerzenumkehrungen mit überkauften/überverkauften RSI-Bedingungen eröffnet und bei gegenteiligen Signalen geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: `UpTrend && PreviousBarDown && PrevRSI < 30 && (RSI15 < 30 => double volume)`
  - Short: `DownTrend && PreviousBarUp && PrevRSI > 70 && (RSI15 > 70 => double volume)`
- **Long/Short**: Beide
- **Stops**: Keine expliziten Stops; Positionen werden durch Indikatorbedingungen geschlossen
- **Standardwerte**:
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI, Stochastic, Smoothed Moving Average
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Timeframe
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
