# Parallele Strategien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin Ashi MACD-Ausbruchssystem, das in beide Richtungen handelt. Es steigt ein, wenn ein neuer Heikin Ashi-Trend mit einem Ausbruch über oder unter dem Donchian-Kanal übereinstimmt und der MACD den Momentum bestätigt.

Die Kombination aus Trendidentifikation durch Heikin Ashi und Ausbruchserkennung hält Trades auf frische Bewegungen ausgerichtet. Der MACD fungiert als Momentum-Filter, um Fehlsignale zu vermeiden.

Am besten für Trader geeignet, die frühe Ausbruchseinstiege nach einer Trendumkehr suchen. Funktioniert auf Intraday-Zeitrahmen.

## Details

- **Einstiegskriterien**:
  - Long: `Trend turns bullish && Close > DonchianHigh && MACD > Signal`
  - Short: `Trend turns bearish && Close < DonchianLow && MACD < Signal`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetztes Ausbruchssignal
- **Stops**: Nicht definiert
- **Standardwerte**:
  - `DonchianPeriod` = 5
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, Donchian Channel, MACD
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
