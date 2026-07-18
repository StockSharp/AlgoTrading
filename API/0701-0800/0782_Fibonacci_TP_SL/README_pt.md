# Estratégia Fibonacci TP SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa níveis de retração Fibonacci para gerar entradas com stop-loss baseado em ATR e take-profit percentual. O trading é limitado por um intervalo mínimo de barras entre operações e um limite de lucro semanal.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close <= Fib 38.2%` && `Close >= Fib 78.6%` && `Min bars since last trade`
  - **Vendido**: `Close <= Fib 23.6%` && `Close >= Fib 61.8%` && `Min bars since last trade`
- **Comprado/Vendido**: Ambos os lados
- **Critérios de saída**:
  - `ATR stop-loss` ou `Take-profit`
- **Stops**: Sim
- **Valores padrão**:
  - `Take Profit %` = 4
  - `Min Bars Between Trades` = 10
  - `Lookback` = 100
  - `ATR Period` = 14
  - `ATR Multiplier` = 1.5
  - `Max Weekly Return` = 0.15

- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Highest, Lowest, ATR
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
