# SMC-Strategie BTC 1H OB FVG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Smart Money Concepts-basierte Strategie für Bitcoin auf 1-Stunden-Kerzen. Das System geht Long nach einem bullischen Break of Structure, wenn der Preis zum erkannten Order Block oder Fair Value Gap zurückkehrt. Der Stop-Loss verwendet einen ATR-Multiplikator und der Take-Profit wird aus einem Risiko/Rendite-Verhältnis berechnet.

## Details

- **Einstiegskriterien**: Nach bullischem BOS, kaufen wenn der Preis den Order Block oder Fair Value Gap innerhalb von `ZoneTimeout` Bars berührt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Fester Take-Profit und Stop-Loss.
- **Stops**: Fest mittels ATR.
- **Standardwerte**:
  - `UseOrderBlock` = true
  - `UseFvg` = true
  - `AtrFactor` = 6
  - `RiskRewardRatio` = 2.5
  - `ZoneTimeout` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: ATR
  - Stops: Fest
  - Komplexität: Einfach
  - Zeitrahmen: Intraday (1H)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
