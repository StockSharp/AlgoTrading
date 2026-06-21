# Estratégia de Pivô Smart Money
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de máximos e mínimos de pivô. Uma posição comprada é aberta quando o preço rompe acima do último pivô alto, enquanto uma posição vendida é aberta quando o preço cai abaixo do último pivô baixo. Cada operação usa seus próprios percentuais de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: Rompimento acima do pivô alto ou abaixo do pivô baixo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `EnableLongStrategy` = true
  - `LongStopLossPercent` = 1m
  - `LongTakeProfitPercent` = 1.5m
  - `EnableShortStrategy` = true
  - `ShortStopLossPercent` = 1m
  - `ShortTakeProfitPercent` = 1.5m
  - `Period` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
