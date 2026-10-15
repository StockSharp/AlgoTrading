# MACD + Bollinger Bands + RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses zusammengesetzte Setup sucht nach Rückläufern gegen das vorherrschende MACD-Momentum, die über die Bollinger-Bänder hinausgehen. Wenn MACD positiv ist und dennoch der Preis unter der unteren Bande schließt, während der RSI überverkauft ist, kauft die Strategie in Erwartung einer Trendfortsetzung. Für Shorts gilt das Gegenteil.

## Details

- **Einstiegskriterien**:
  - **Long**: `MACD > 0` und `Close < LowerBand` und `RSI < 30`
  - **Short**: `MACD < 0` und `Close > UpperBand` und `RSI > 70`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, Bollinger Bands, RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
