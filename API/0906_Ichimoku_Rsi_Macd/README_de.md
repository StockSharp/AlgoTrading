# Ichimoku RSI MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die Ichimoku Cloud, RSI und MACD-Kreuzungssignale kombiniert.

## Details

- **Einstiegskriterien**: Preis ober-/unterhalb der Ichimoku-Wolke mit RSI-Filter und MACD-Linie, die die Signallinie kreuzt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte MACD-Kreuzung.
- **Stops**: Keine.
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Ichimoku, RSI, MACD
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
