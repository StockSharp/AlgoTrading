# Estratégia de Anomalia de Acumulação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Accrual Anomaly** implementa o fator de anomalia de acumulação. Rebalanceia anualmente no primeiro dia de negociação de maio, comprando ações de baixa acumulação e vendendo as de alta acumulação.

Os testes indicam um retorno anual médio de aproximadamente 12%. Tem melhor desempenho no mercado de ações dos EUA.

As posições são ajustadas uma vez por ano; nenhum sinal intradiário é utilizado.

## Detalhes
- **Critérios de entrada**: ver implementação para os cálculos de acumulação.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Rebalanceamento na próxima data programada.
- **Stops**: Sem lógica de stop explícita.
- **Valores padrão**:
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Fundamentals
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
