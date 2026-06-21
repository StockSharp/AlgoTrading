# MACD Volumen XAUUSD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

15-Minuten-Strategie für XAUUSD, die MACD-Nulllinienkreuzungen mit einem Volumenoszillator-Filter und fixen Risikoparametern kombiniert.

## Details

- **Einstiegskriterien**: MACD kreuzt die Nulllinie mit positivem Volumenoszillator und Volumenvergleich.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss- oder Take-Profit-Niveaus.
- **Stops**: Fester Stop-Loss und Take-Profit-Multiplikator.
- **Standardwerte**:
  - `ShortLength` = 5
  - `LongLength` = 8
  - `FastLength` = 16
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Leverage` = 1.0
  - `StopLoss` = 10100
  - `TakeProfitMultiplier` = 1.1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, EMA, Volumen
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
