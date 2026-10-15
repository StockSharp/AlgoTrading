# Cm Manual Grid — Estrategia de Cuadrícula Manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Cm Manual Grid coloca una cuadrícula configurable de órdenes stop y límite alrededor del precio actual. Cada nueva orden aumenta el volumen en un incremento fijo. La estrategia puede cerrar posiciones largas o cortas por separado cuando se alcanzan los objetivos de beneficio e incluye un mecanismo de trailing de beneficio.

## Detalles

- **Tipo**: trading en cuadrícula con órdenes pendientes
- **Órdenes**: Buy Stop, Sell Stop, Buy Limit, Sell Limit
- **Volumen**: volumen inicial `Lot` con incremento `LotPlus`
- **Gestión de beneficio**:
  - `CloseProfitB` cierra posiciones largas
  - `CloseProfitS` cierra posiciones cortas
  - `ProfitClose` cierra todas las posiciones
  - `TralStart` y `TralClose` gestionan el trailing de beneficio
- **Valores predeterminados**:
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 pasos
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
