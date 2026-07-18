# MACD Muster-Trader-Strategie (All)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Positionen bei starken MACD-Umkehrungen eröffnet. Sie sucht nach zwei großen Spikes um einen kleinen Zwischenwert der MACD-Linie. Ein Verkauf wird eröffnet, wenn der vorherige MACD-Wert positiv ist und der aktuelle Wert tief in negatives Territorium fällt. Ein Kauf wird bei der umgekehrten Bedingung eröffnet. Stop-Loss und Take-Profit werden aus den jüngsten Hochs und Tiefs abgeleitet.

Der Algorithmus eignet sich für volatile Märkte, in denen sich der Momentum schnell ändert. Es werden nur Marktaufträge verwendet und Risikoniveaus aus der Kerzenhistorie berechnet.

## Details

- **Einstiegskriterien**: MACD-Spike-Verhältnis basierend auf `RatioThreshold`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop am jüngsten Extremwert plus Offset oder entgegengesetzter Spike.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastEmaPeriod` = 24
  - `SlowEmaPeriod` = 13
  - `StopLossBars` = 22
  - `TakeProfitBars` = 32
  - `OffsetPoints` = 40
  - `RatioThreshold` = 5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
