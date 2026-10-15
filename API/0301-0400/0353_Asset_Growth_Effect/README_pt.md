# Estratégia do Efeito de Crescimento de Ativos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra empresas com o menor crescimento em ativos totais e vende a descoberto aquelas com o maior crescimento de ativos. Todo mês de julho o portfólio é rebalanceado usando os dados fundamentais mais recentes.

Os testes indicam um retorno anual médio de aproximadamente 15%. Tem melhor desempenho no mercado de ações.

O crescimento de ativos é calculado a partir dos ativos totais reportados nos relatórios das empresas. As ações são classificadas em quantis e o quantil mais baixo é comprado enquanto o mais alto é vendido a descoberto. As posições são dimensionadas para atingir uma alavancagem-alvo e são ajustadas anualmente.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Ação no quantil de menor crescimento de ativos.
  - Vendido: Ação no quantil de maior crescimento de ativos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Posições ajustadas no rebalanceamento anual.
- **Stops**: Não.
- **Valores padrão**:
  - `Quantiles` = 10
  - `Leverage` = 1m
  - `MinTradeUsd` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Fundamentals
  - Stops: Não
  - Complexidade: Moderado
  - Período: Longo prazo
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
