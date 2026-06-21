# Ultimate Trading Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Long-Strategie, die RSI-, Moving-Average-, MACD- und Stochastik-Crossover kombiniert, um Einstiege und Ausstiege zu timen.

## Details

- **Einstiegskriterien**: RSI kreuzt überverkaufte Zone nach oben, während der Preis über der MA liegt, MACD und Stochastik kreuzen nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzungsbedingungen.
- **Stops**: Keine expliziten Stops.
- **Standardwerte**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MaLength` = 50
  - `StochLength` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: RSI, MA, MACD, Stochastic
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
