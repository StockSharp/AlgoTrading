# Estratégia de Assimetria de Retorno em Commodities
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Assimetria de Retorno em Commodities** explora a diferença entre retornos positivos e negativos. Para cada futuro de commodity, a janela deslizante soma separadamente todos os movimentos de alta e baixa. Um ratio alto implica uma tendência positiva persistente, enquanto um ratio baixo aponta para pressão vendedora sustentada.

No início de cada mês, as commodities são classificadas por essa medida de assimetria. O sistema compra os N melhores contratos e vende a descoberto os N mais fracos, alocando capital de forma igualitária. O rebalanceamento ocorre mensalmente.

## Detalhes
- **Critérios de entrada**: Classificação mensal da assimetria dos retornos diários em uma janela de retrospectiva.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Posições ajustadas no rebalanceamento mensal.
- **Stops**: Sem stop explícito; tamanho da posição limitado por `MinTradeUsd`.
- **Valores padrão**:
  - `WindowDays = 120`
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Baseados em preço
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
