# Ausbruchskauf-Strategie für Preis und Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie steigt ein, wenn Preis und Volumen gleichzeitig über ihre jeweiligen Lookback-Hochs ausbrechen, während der Preis über dem Trend-SMA bleibt. Short-Trades werden ausgelöst, wenn der Preis unter das Lookback-Tief fällt, unter der gleichen Volumenbedingung und dem SMA-Filter. Positionen werden nach fünf aufeinanderfolgenden Schlusskursen auf der entgegengesetzten Seite des SMA geschlossen.

## Details
- **Einstiegskriterien**:
  - **Long**: Close > vorheriges höchstes Hoch && Volume > vorheriges höchstes Volumen && Close > SMA
  - **Short**: Close < vorheriges niedrigstes Tief && Volume > vorheriges höchstes Volumen && Close < SMA
- **Long/Short**: Konfigurierbar
- **Ausstiegskriterien**:
  - **Trend**: Fünf Schlusskurse jenseits des SMA
- **Stops**: Nein
- **Standardwerte**:
  - `PriceBreakoutPeriod` = 60
  - `VolumeBreakoutPeriod` = 60
  - `TrendlineLength` = 200
  - `OrderDirection` = "Long"
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Konfigurierbar
  - Indikatoren: Highest, SMA, Volume
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
