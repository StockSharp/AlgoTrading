# Estratégia Hancock RSI Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula um Índice de Força Relativa (RSI) ponderado por volume, inspirado no script Hancock do TradingView. O RSI usa o volume altista e baixista para medir a força da tendência. Uma posição comprada é aberta quando a tendência do RSI vira para cima, e uma posição vendida quando vira para baixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A tendência do RSI muda para cima.
  - **Vendido**: A tendência do RSI muda para baixo.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal de tendência oposta.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RSI Length` = 14.
  - `Threshold` = 0.1.
  - `Use Wicks` = true.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, Volume
  - Stops: Não
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
