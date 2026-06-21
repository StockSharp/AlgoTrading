# Uptrick X PineIndicators: Estratégia Z-Score Flow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência usando Z-Score, EMA e filtros RSI.

## Detalhes

- **Critérios de entrada**: Z-Score cruza os limites de compra/venda com confirmação de tendência e RSI
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto baseado no modo selecionado
- **Stops**: Não
- **Valores padrão**:
  - `ZScorePeriod` = 100
  - `EmaTrendLen` = 50
  - `RsiLen` = 14
  - `RsiEmaLen` = 8
  - `ZBuyLevel` = -2
  - `ZSellLevel` = 2
  - `CooldownBars` = 10
  - `SlopeIndex` = 30
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA, RSI, StandardDeviation
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
