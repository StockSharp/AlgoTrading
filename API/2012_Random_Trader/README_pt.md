# Estratégia de Operador Aleatório
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Abre uma posição comprada ou vendida aleatoriamente quando não há posição aberta. Cada operação usa valores fixos de take profit e stop loss medidos em unidades de preço.

## Detalhes

- **Critérios de entrada**: sem posição e escolha aleatória
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: o preço atinge o take profit ou o stop loss
- **Stops**: Sim
- **Valores padrão**:
  - `Volume` = 1
  - `TakeProfit` = 10
  - `StopLoss` = 10
- **Filtros**:
  - Categoria: Outro
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Tick
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
