# Estratégia de Rompimento de Inclinação RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento de Inclinação RSI observa a taxa de variação do RSI. Uma inclinação incomumente acentuada sugere que uma nova tendência está se formando.

Os testes indicam um retorno anual médio de aproximadamente 136%. Funciona melhor no mercado de ações.

As entradas ocorrem quando a inclinação excede seu nível típico em um múltiplo do desvio padrão, realizando operações na direção da aceleração com um stop protetor.

Atrai traders ativos que buscam exposição antecipada à tendência. As posições são encerradas quando a inclinação retorna às leituras normais. `RsiPeriod` padrão = 14.

## Detalhes

- **Critérios de entrada**: O indicador supera a média pelo multiplicador de desvio.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

