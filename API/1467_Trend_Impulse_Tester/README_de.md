# Trend-Impulse-Tester-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend Impulse Tester eröffnet Trades, wenn ein starker Trend durch EMAs und ADX bestätigt wird und ein RSI-Impuls erscheint.
Kauft bei bullischen Impulsen während Aufwärtstrends und verkauft bei bärischen Impulsen während Abwärtstrends.

## Details

- **Einstiegskriterien**: EMA-Trend + ADX-Bestätigung mit RSI-Schwellenkreuzung
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `AdxLength` = 14
  - `AdxMin` = 18
  - `RsiLength` = 14
  - `RsiUp` = 55
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, ADX, RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
