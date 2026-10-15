# Market Slayer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen gewichteten gleitenden Durchschnitt-Crossover mit einer SSL-Trendbestätigung auf einem höheren Zeitrahmen. Eine Long-Position wird eröffnet, wenn der kurze WMA den langen WMA von unten nach oben kreuzt und der Trend bullisch ist; eine Short-Position bei umgekehrten Bedingungen. Ein optionaler absoluter Take Profit und Stop Loss kann aktiviert werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurzer WMA kreuzt über den langen WMA und der SSL des höheren Zeitrahmens ist bullisch.
  - **Short**: Kurzer WMA kreuzt unter den langen WMA und der SSL des höheren Zeitrahmens ist bärisch.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Trendfilter dreht in die entgegengesetzte Richtung.
  - Optionaler Stop Loss oder Take Profit bei Aktivierung.
- **Stops**: Optional.
- **Standardwerte**:
  - `ShortLength` = 10.
  - `LongLength` = 20.
  - `ConfirmationTrendValue` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
  - `TrendCandleType` = TimeSpan.FromMinutes(240).TimeFrame().
  - `TakeProfitEnabled` = false.
  - `TakeProfitValue` = 20.
  - `StopLossEnabled` = false.
  - `StopLossValue` = 50.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: WMA, SSL
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
