# Estratégia BullBear Volume Percentil TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza Bull/Bear Power normalizado por um Z-Score.
Posições compradas são abertas quando o Z-Score cruza acima do limiar,
enquanto posições vendidas são abertas quando cruza abaixo do limiar negativo.
Os níveis de take profit são baseados em multiplicadores de ATR ajustados por volume e percentis de preço.

## Detalhes

- **Critérios de entrada:**
  - **Comprado**: Z-Score cruza acima de `ZThreshold`.
  - **Vendido**: Z-Score cruza abaixo de `-ZThreshold`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Z-Score cruza de volta por zero ou atinge níveis de take profit.
- **Stops**: Take profit via multiplicadores de ATR.
- **Valores padrão:**
  - Comprimento EMA 21, comprimento Z-Score 252, limiar 1.618.
  - Período ATR 20, multiplicadores 1.618 / 2.382 / 3.618.
  - Período MA de volume 100, período de percentil 100.
- **Filtros:**
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sim
  - Complexidade: Médio
  - Período: Médio prazo
