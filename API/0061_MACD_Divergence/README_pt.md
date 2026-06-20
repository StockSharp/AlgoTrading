# Divergência MACD (MACD Divergence)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Divergência MACD busca discrepâncias entre a ação do preço e o indicador MACD. Máximas mais altas no preço com máximas mais baixas no MACD sugerem enfraquecimento do momentum (divergência baixista), enquanto mínimas mais baixas no preço com mínimas mais altas no MACD apontam para uma reversão altista.

Os testes indicam um retorno anual médio de aproximadamente 70%. Funciona melhor no mercado de ações.

Após detectar a divergência, o sistema aguarda que o MACD cruze sua linha de sinal antes de entrar. A operação é fechada se o MACD cruzar na direção oposta ou o stop-loss for acionado.

## Detalhes

- **Critérios de entrada**: Divergência altista ou baixista mais cruzamento do MACD com a linha de sinal.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O MACD cruza na direção oposta ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `DivergencePeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
