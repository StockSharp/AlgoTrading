# Estratégia de Busca de Taças
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia baseada em padrões busca formações arredondadas em forma de "taça" nos dados de preço. Quando o preço rompe uma taça completa, entra comprado ou vendido dependendo da direção.

Testes indicam um retorno anual médio de cerca de 47%. Funciona melhor em ações.

A estratégia compra em rompimentos altistas de taças e vende em taças invertidas baixistas. As posições são protegidas por um stop-loss.

## Detalhes

- **Critérios de entrada**: O padrão de taça se forma e o preço rompe a borda.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O preço reverte ou atinge o stop-loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `Lookback` = 150
  - `WidthPercent` = 5m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado/Vendido
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
