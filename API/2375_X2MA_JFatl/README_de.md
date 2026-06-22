# X2MA JFatl Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Adaption des MetaTrader-Experten `Exp_X2MA_JFatl`. Sie kombiniert einen schnellen Simple Moving Average (SMA) mit einem langsamen Jurik Moving Average (JMA) und einem zusätzlichen Jurik-Filter zur Bestätigung der Trendrichtung. Trades werden eröffnet, wenn der schnelle Durchschnitt den langsamen kreuzt und der Preis auf derselben Seite des Filters liegt. Positionen werden geschlossen, wenn der Preis gegen den Filter läuft oder ein entgegengesetzter Crossover auftritt.

## Details

- **Einstiegskriterien**:
  - **Long**: `SMA_fast` kreuzt über `JMA_slow` und `Close` > `JMA_filter`.
  - **Short**: `SMA_fast` kreuzt unter `JMA_slow` und `Close` < `JMA_filter`.
- **Ausstiegskriterien**:
  - Preis bewegt sich auf die entgegengesetzte Seite des Filters.
  - Entgegengesetzter Crossover der Durchschnitte.
- **Long/Short**: Beide Seiten.
- **Stops**: Standardmäßig nicht verwendet.
- **Standardwerte**:
  - `Fast MA Length` = 5.
  - `Slow MA Length` = 12.
  - `Filter Length` = 20.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere (SMA, JMA)
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
