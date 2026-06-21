# Estratégia de Compra em Queda com Múltiplas Posições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Buy Dip Multiple Positions adiciona posições compradas quando ocorre uma queda de preço junto com alto volume e uma condição de impulso de preço. Cada operação arrisca 2% do patrimônio e compartilha níveis comuns de stop dinâmico e alvo. Uma nova posição é aberta somente se a operação anterior fechada foi lucrativa.

## Detalhes

- **Critérios de entrada**:
  - Fechamento abaixo da mínima anterior em 0,2%.
  - Volume acima de 120% da média das duas últimas barras.
  - Fechamento abaixo do preço de fechamento N barras atrás multiplicado por `PriceSurgePercent` / 100.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Stop inicial como percentual da mínima da barra de entrada.
  - Stop dinâmico crescente a cada barra após o setup.
  - Preço alvo acima da mínima da barra de entrada.
- **Stops**: Sim.
- **Valores padrão**:
  - `MaxPositions` = 20
  - `TrailRatePercent` = 1
  - `InitialStopPercent` = 85
  - `TargetPricePercent` = 60
  - `PriceSurgePercent` = 89
  - `SurgeLookbackBars` = 14
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Volume, Ação do preço
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
