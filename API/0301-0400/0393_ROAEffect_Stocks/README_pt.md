# Estratégia de Efeito ROA em Ações
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Efeito ROA em Ações** foca em ações com alto retorno sobre ativos (ROA). Um feed externo de dados fundamentais fornece os valores de ROA para o universo de negociação. No início de cada mês, as ações são classificadas por ROA, e a carteira assume posições compradas no decil superior e vendidas no decil inferior.

As posições têm igual ponderação e são rebalanceadas mensalmente, capturando a tendência de empresas lucrativas de superar o mercado.

## Detalhes
- **Critérios de entrada**: Classificação mensal por dados externos de ROA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rebalanceamento mensal.
- **Stops**: Sem stop explícito.
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
