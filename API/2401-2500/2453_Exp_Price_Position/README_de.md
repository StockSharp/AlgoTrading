# Exp Preisposition
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Exp Price Position**-Strategie adaptiert den ursprünglichen MetaTrader-Expertenberater, der die Preislage mit einem Stufentrendfilter kombiniert.
Sie bewertet die Beziehung zwischen zwei medianen gleitenden Durchschnitten, um das letzte Swing-Niveau zu ermitteln, und prüft dann ein schnelles und langsames geglättetes gleitendes Durchschnittspaar, um die Trendrichtung zu bestimmen.
Orders werden nur eröffnet, wenn sowohl die Preisposition als auch der Stufentrend mit der aktuellen Kerzenstruktur übereinstimmen.

Die Strategie ist für Märkte konzipiert, in denen Trendwechsel auftreten, nachdem der Preis auf ein dynamisches medianes Niveau zurückgezogen hat. Ein Trailing-Stop und eine Take-Profit-Ratio werden zur Risikoverwaltung eingesetzt.

## Details

- **Einstiegskriterien**: Preis über dem letzten Swing-Niveau mit bullishem Stufentrend für Long-Trades; darunter mit bärischem Stufentrend für Short-Trades.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensignal oder Schutz-Stop.
- **Stops**: Ja, über Trailing-Stop mit Take-Profit-Ratio.
- **Standardwerte**:
  - `FastPeriod` = 2
  - `SlowPeriod` = 30
  - `MedianFastPeriod` = 26
  - `MedianSlowPeriod` = 20
  - `TpSlRatio` = 3m
  - `TrailingStopPips` = 10m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Smoothed Moving Average, Simple Moving Average
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
