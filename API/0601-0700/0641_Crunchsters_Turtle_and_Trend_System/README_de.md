# Crunchster's Turtle-und-Trend-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert einen schnellen/langsamen EMA-Trendfilter mit Donchian-Kanal-Ausbrucheinstiegen und ATR-basiertem Stop-Management. Ein Trailing-Donchian-Kanal beendet Positionen, wenn das Momentum dreht.

## Details

- **Einstiegskriterien**: EMA-Differenzkreuzung oder Donchian-Kanal-Ausbruch.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Trailing-Kanal oder ATR-Stop.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `CandleType` = 1 Stunde
  - `FastEmaPeriod` = 10
  - `BreakoutPeriod` = 20
  - `TrailPeriod` = 1000
  - `StopAtrMultiple` = 20
  - `OrderPercent` = 10
  - `TrendEnabled` = true
  - `BreakoutEnabled` = false
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long & Short
  - Indikatoren: EMA, Donchian, ATR
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
