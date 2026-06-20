# Estratégia Big Runner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Big Runner opera quando o preço de fechamento e uma SMA rápida cruzam ambas na direção de uma SMA mais lenta, indicando forte momentum. O tamanho da posição é derivado de um percentual do valor do portfólio multiplicado pela alavancagem. Níveis opcionais de stop-loss e take-profit gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprar quando o fechamento cruza para cima da SMA rápida e a SMA rápida cruza para cima da SMA lenta.
  - Vender quando o fechamento cruza para baixo da SMA rápida e a SMA rápida cruza para baixo da SMA lenta.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - Stop-loss e take-profit opcionais com base no preço de entrada.
  - O sinal contrário fecha a posição existente.
- **Stops**: Percentuais de stop-loss e take-profit configuráveis.
- **Valores padrão**:
  - `FastLength` = 5
  - `SlowLength` = 20
  - `TakeProfitLongPercent` = 4
  - `TakeProfitShortPercent` = 7
  - `StopLossLongPercent` = 2
  - `StopLossShortPercent` = 2
  - `PercentOfPortfolio` = 10
  - `Leverage` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
