# Aggressive Hohe-IV-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Aggressive Hohe-IV-Strategie kombiniert EMA-Kreuzungen mit einem ATR-Volatilitätsfilter. Trades werden nur eröffnet, wenn die Volatilität ihren Mittelwert um eine Standardabweichung überschreitet, und mit ATR-basierten Zielen geschlossen.

Tests zeigen solide Renditen in sehr volatilen Märkten.

Die Strategie tritt bei EMA-Kreuzungen in Phasen erhöhter Volatilität ein und strebt schnelle Gewinne mit vordefinierten Risikokontrollen an.

Positionen werden über ATR-basierte Stop-Loss- und Take-Profit-Niveaus geschlossen.

## Details

- **Einstiegskriterien**: Schnelle EMA kreuzt langsame EMA mit ATR über seinem Mittelwert plus Standardabweichung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder Take-Profit erreicht.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 30
  - `AtrLength` = 14
  - `AtrMeanLength` = 20
  - `AtrStdLength` = 20
  - `RiskFactor` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
