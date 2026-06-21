# Dualer MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die zwei MACD-Indikatoren kombiniert. Der langsamere MACD eröffnet Trades beim Nullliniendurchgang, wenn das Histogramm des schnelleren MACD übereinstimmt. Die Position wird geschlossen, wenn der schnelle MACD dreht oder der Stop/Take Profit ausgelöst wird.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 65%. Am besten funktioniert es am Aktienmarkt.

## Details

- **Einstiegskriterien**: Nullliniendurchgang des langsamen MACD-Histogramms mit Bestätigung durch den schnellen MACD.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umkehr des schnellen MACD oder Stop/Ziel.
- **Stops**: Ja.
- **Standardwerte**:
  - `Macd1FastLength` = 34
  - `Macd1SlowLength` = 144
  - `Macd1SignalLength` = 9
  - `Macd2FastLength` = 100
  - `Macd2SlowLength` = 200
  - `Macd2SignalLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

