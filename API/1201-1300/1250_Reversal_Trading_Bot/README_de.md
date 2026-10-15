# Umkehr-Trading-Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Umkehr-Trading-Bot-Strategie nutzt RSI-Divergenz mit optionalen Volumen-, ADX-, Bollinger-Bands- und RSI-Crossover-Filtern, um Marktumkehrungen zu erfassen. Positionen werden mit festem Prozent-Stop-Loss und Take-Profit geschützt.

## Details

- **Einstiegskriterien**: RSI-Divergenz mit optionalen Volumen-, ADX-, Bollinger-Bands- und RSI-Crossover-Filtern
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Fester Prozentsatz
- **Standardwerte**:
  - `RsiLength` = 8
  - `FastRsiLength` = 14
  - `SlowRsiLength` = 21
  - `BbLength` = 20
  - `AdxThreshold` = 20
  - `DivLookback` = 5
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI, ADX, Bollinger Bands, SMA
  - Stops: Fest
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

