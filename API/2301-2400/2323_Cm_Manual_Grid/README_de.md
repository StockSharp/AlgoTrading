# Cm Manual Grid — Manuelle Gitter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Cm Manual Grid platziert ein konfigurierbares Gitter aus Stop- und Limit-Orders um den aktuellen Preis. Jede neue Order erhöht das Volumen um einen festen Betrag. Die Strategie kann Long- oder Short-Positionen separat schließen, wenn Gewinnziele erreicht werden, und enthält einen Trailing-Gewinn-Mechanismus.

## Details

- **Typ**: Gitterhandel mit ausstehenden Orders
- **Orders**: Buy Stop, Sell Stop, Buy Limit, Sell Limit
- **Volumen**: Startvolumen `Lot` mit Inkrement `LotPlus`
- **Gewinnverwaltung**:
  - `CloseProfitB` schließt Long-Positionen
  - `CloseProfitS` schließt Short-Positionen
  - `ProfitClose` schließt alle Positionen
  - `TralStart` und `TralClose` verwalten den Trailing-Gewinn
- **Standardwerte**:
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 Schritte
  - `StepBuyStop` = 10
  - `StepSellStop` = 10
  - `StepBuyLimit` = 10
  - `StepSellLimit` = 10
  - `Lot` = 0.1
  - `LotPlus` = 0.1
  - `CloseProfitB` = 10
  - `CloseProfitS` = 10
  - `ProfitClose` = 10
  - `TralStart` = 10
  - `TralClose` = 5
