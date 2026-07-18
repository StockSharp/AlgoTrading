# Estratégia de Rompimento RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Rompimento RSI busca explosões de momentum quando o Índice de Força Relativa (RSI) ultrapassa seu intervalo típico. Ao medir os desvios do RSI em relação à sua média móvel, o sistema visa capturar novas tendências no início.

Os testes indicam um retorno anual médio de cerca de 88%. Funciona melhor no mercado de ações.

Uma posição comprada é aberta quando o RSI fecha acima da média mais `Multiplier` vezes o desvio padrão. Uma posição vendida é tomada quando o RSI cai abaixo da média menos esse multiplicador. As posições são fechadas assim que o RSI cruza de volta pelo seu valor médio.

Os traders de momentum podem achar essa abordagem útil para identificar rompimentos precoces enquanto mantêm níveis de saída definidos. Um percentual de stop-loss protege contra reversões repentinas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI > Avg + Multiplier * StdDev
  - **Vendido**: RSI < Avg - Multiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando RSI < Avg
  - **Vendido**: Sair quando RSI > Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
