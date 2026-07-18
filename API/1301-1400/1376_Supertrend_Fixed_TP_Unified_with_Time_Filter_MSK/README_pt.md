# Estratégia Supertrend com TP Fixo Unificado e Filtro de Tempo MSK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Supertrend com take profit percentual fixo, filtro de preço opcional e filtro de tempo no fuso horário de Moscou.

## Detalhes
- **Critérios de entrada**: Mudança de direção do Supertrend com filtros opcionais de preço e tempo
- **Comprado/Vendido**: Configurável (comprado, vendido ou ambos)
- **Critérios de saída**: Take profit fixo ou sinal oposto
- **Stops**: Somente take profit
- **Valores padrão**:
  - `AtrPeriod` = 23
  - `Factor` = 1.8m
  - `TakeProfitPercent` = 1.5m
  - `PriceFilter` = 10000m
  - `TimeFrom` = 0
  - `TimeTo` = 23
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Supertrend
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
