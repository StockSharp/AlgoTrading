# IU Higher-Timeframe-MA-Kreuz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die IU Higher Timeframe MA Cross Strategie handelt, wenn ein schneller gleitender Durchschnitt, der auf einem benutzerdefinierten Zeitrahmen berechnet wird, einen langsameren gleitenden Durchschnitt aus einem möglicherweise anderen Zeitrahmen kreuzt. Eine Long-Position wird bei einem bullischen Kreuz eröffnet und eine Short-Position bei einem bärischen Kreuz. Der Stop-Loss wird am Extrempunkt der vorherigen Kerze platziert, und der Take Profit verwendet ein konfigurierbares Risiko-Ertrags-Verhältnis.

## Details
- **Daten**: Kerzen aus angegebenen Zeitrahmen.
- **Einstiegskriterien**:
  - **Long**: MA1 kreuzt über MA2.
  - **Short**: MA1 kreuzt unter MA2.
- **Ausstiegskriterien**: Stop-Loss oder Take Profit erreicht.
- **Stops**: Vorheriges Kerzenhoch/-tief mit `RiskToReward`-Multiplikator.
- **Standardwerte**:
  - `Ma1CandleType` = 60m
  - `Ma1Length` = 20
  - `Ma1Type` = MovingAverageTypeEnum.Exponential
  - `Ma2CandleType` = 60m
  - `Ma2Length` = 50
  - `Ma2Type` = MovingAverageTypeEnum.Exponential
  - `RiskToReward` = 2
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long & Short
  - Indikatoren: Gleitender Durchschnitt
  - Komplexität: Niedrig
  - Risikolevel: Mittel
