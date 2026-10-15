# Live RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet mehrere RSI-Berechnungen (close, weighted, typical, median, open) und Parabolic SAR zur Erkennung von Trendumkehrungen. Geht Long, wenn RSI-Werte in bullischer Reihenfolge ausgerichtet sind und der Preis über dem SAR liegt; geht Short, wenn die Ausrichtung bärisch ist und der Preis unter dem SAR liegt. Der SAR-Wert fungiert als Trailing Stop.

## Details

- **Einstiegskriterien**:
  - Long, wenn die RSI-Sequenz bullisch ist und der Preis über dem SAR liegt.
  - Short, wenn die RSI-Sequenz bärisch ist und der Preis unter dem SAR liegt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetztes Trendsignal oder SAR-Trailing Stop.
- **Stops**: Optionaler fester Stop-Loss plus SAR-basierter Trailing Stop.
- **Standardwerte**:
  - `RSI Period` = 30
  - `SAR Step` = 0.08
  - `Stop Loss` = 40
  - `Check Hour` = false
  - `Start Hour` = 17
  - `End Hour` = 1
  - `Candle Type` = 1 Stunde
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: RSI, Parabolic SAR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Optional (Zeitfilter)
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
