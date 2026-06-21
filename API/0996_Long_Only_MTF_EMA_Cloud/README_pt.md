# Somente Comprado MTF EMA Nuvem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de nuvem EMA que opera comprado quando a EMA curta cruza acima da EMA longa. Utiliza stop loss e take profit com percentual fixo.

## Detalhes

- **Critérios de entrada**: EMA curta cruza acima da EMA longa.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: O preço atinge o stop loss ou take profit.
- **Stops**: Stop loss e take profit com percentual fixo.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `ShortLength` = 21
  - `LongLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 2.0m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
