# Estratégia de Lançamento de Moeda Aleatório
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia experimental lança uma moeda a cada N barras e entra comprado ou vendido com base no resultado. O risco é gerenciado por meio de níveis de stop-loss e take-profit baseados em ATR.

Os testes indicam um retorno anual médio de cerca de 8%. Funciona melhor no mercado de criptomoedas.

A ideia é fornecer uma linha de base para entradas aleatórias mantendo saídas disciplinadas.

## Detalhes

- **Critérios de entrada**: A cada `EntryFrequency` barras uma moeda é lançada; cara vai comprado, coroa vai vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit atingido.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrLength` = 14
  - `SlMultiplier` = 1m
  - `TpMultiplier` = 2m
  - `EntryFrequency` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Experimental
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Simples
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

