# EMA-5-Alarmkerzen-Short-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **EMA 5 Alert Candle Short** wartet auf drei Kerzen, die die 5-Perioden-EMA berühren, und identifiziert dann eine Kerze, die über ihr bleibt. Eine Short-Position wird eröffnet, wenn die nächste Kerze das Tief der Alarmkerze unterschreitet, wobei das Take-Profit auf die gleiche Distanz wie der Stop Loss gesetzt wird.

## Details
- **Einstiegskriterien**: nach drei EMA-berührenden Kerzen, Short beim Bruch des Tiefs einer nicht berührenden Kerze.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Stop Loss am Hoch der Alarmkerze, Take Profit auf gleicher Distanz.
- **Stops**: Ja, basierend auf der Range der Alarmkerze.
- **Standardwerte**:
  - `EmaPeriod = 5`
  - `RiskPerTrade = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Short
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
