# BONK Long-Volatilität
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese reine Long-Strategie steigt bei starken bullischen Bedingungen ein, die gleitende Durchschnitte, Volatilität und Volumenfilter kombinieren. Kauft, wenn der Markt aufwärts tendiert, die Volatilität zunimmt und Momentum-Indikatoren Stärke bestätigen. Ausstiege verwenden festes Take Profit, Stop Loss und einen ATR-basierten Trailing Stop.

## Details

- **Einstiegskriterien**: Schneller MA über langsamem MA, Preisrange größer als ATR * `AtrMultiplier`, RSI zwischen `RsiOversold` und `RsiOverbought`, MACD-Linie über Signal und null, Volumen über SMA * `VolumeThreshold`, Schlusskurs über schnellem MA, Kerze innerhalb der letzten `LookbackDays`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Take Profit, Stop Loss oder ATR-Trailing Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: SMA, ATR, RSI, MACD, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

