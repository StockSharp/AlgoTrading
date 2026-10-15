# Estratégia de Fator de Momentum Residual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Fator de Momentum Residual** classifica ativos por uma pontuação externa de momentum residual.
No primeiro dia de negociação de cada mês, assume posições compradas no decil superior e vendidas no decil inferior.

## Detalhes
- **Critérios de entrada**: feed de dados externo de momentum residual.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rebalanceamento mensal.
- **Stops**: Sem lógica de stop explícita.
- **Valores padrão**:
  - `Decile = 10`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Fundamentais
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
