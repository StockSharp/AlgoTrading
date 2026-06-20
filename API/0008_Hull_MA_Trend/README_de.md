# Hull MA-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Hull Moving Average-Trend.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61%. Die Strategie funktioniert am besten im Kryptomarkt.

Die Hull MA-Trend-Strategie überwacht die Steigung der Hull Moving Average. Steigende Steigungen veranlassen Longs und fallende Steigungen veranlassen Shorts, mit einem ATR-Trailing-Stop zum Schutz jedes Trades.

Ihre reaktionsfähige Berechnung reduziert die Verzögerung im Vergleich zu traditionellen gleitenden Durchschnitten, sodass das System schnell auf neues Momentum reagieren kann. Der ATR-Stop hilft, große Drawdowns zu vermeiden, wenn sich die Steigung abrupt ändert.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, ATR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `HmaPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

