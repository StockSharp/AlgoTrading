# Estratégia Charles SMA com Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera usando o cruzamento de duas Médias Móveis Simples e gerenciamento opcional de trailing stop. Quando a SMA rápida cruza acima da SMA lenta, uma posição comprada é aberta. Uma posição vendida é aberta quando a SMA rápida cruza abaixo da SMA lenta. A estratégia suporta stop-loss fixo, take-profit e um trailing stop que se ativa após um limiar de lucro predefinido.

## Detalhes

- **Critérios de entrada**:
  - SMA rápida cruza acima da SMA lenta → abrir comprado.
  - SMA rápida cruza abaixo da SMA lenta → abrir vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Cruzamento inverso.
  - Stop-loss ou take-profit atingido.
  - Trailing stop acionado quando o lucro atinge `TrailStart` e segue com `TrailingAmount`.
- **Stops**:
  - `StopLoss` define um stop protetor fixo em unidades de preço.
  - `TakeProfit` define um alvo de lucro fixo.
  - `TrailStart` e `TrailingAmount` controlam o trailing stop.
- **Valores padrão**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `StopLoss` = 0
  - `TakeProfit` = 25
  - `TrailStart` = 25
  - `TrailingAmount` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
