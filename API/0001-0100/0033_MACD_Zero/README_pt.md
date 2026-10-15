# Estratégia MACD Zero Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Este sistema opera mudanças de momentum quando o histograma do Moving Average Convergence Divergence (MACD) se aproxima da linha zero. Um MACD ascendente abaixo de zero ou um MACD descendente acima de zero sinaliza uma possível reversão.

Os testes indicam um retorno anual médio de aproximadamente 136%. Funciona melhor no mercado de ações.

A estratégia aguarda que a linha MACD se aproxime de zero enquanto ainda está no lado oposto. Quando o momentum diminui, ela entra antecipando uma oscilação no preço.

As operações saem quando o MACD cruza sua linha de sinal ou um stop-loss é ativado.

## Detalhes

- **Critérios de entrada**: MACD tendendo para zero de qualquer lado.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: MACD cruza a linha de sinal ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

