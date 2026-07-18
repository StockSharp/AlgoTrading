# Cm Manual Grid — Estratégia de Grade Manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Cm Manual Grid coloca uma grade configurável de ordens stop e limite ao redor do preço atual. Cada nova ordem aumenta o volume em um incremento fixo. A estratégia pode fechar posições compradas ou vendidas separadamente quando as metas de lucro são atingidas e inclui um mecanismo de trailing de lucro.

## Detalhes

- **Tipo**: trading em grade com ordens pendentes
- **Ordens**: Buy Stop, Sell Stop, Buy Limit, Sell Limit
- **Volume**: volume inicial `Lot` com incremento `LotPlus`
- **Gestão de lucro**:
  - `CloseProfitB` fecha posições compradas
  - `CloseProfitS` fecha posições vendidas
  - `ProfitClose` fecha todas as posições
  - `TralStart` e `TralClose` gerenciam o trailing de lucro
- **Valores padrão**:
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 passos
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
