# Estratégia de Compra Personalizada BID
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Custom Buy BID usa o indicador Supertrend para identificar reversões altistas. Abre uma posição comprada quando o preço cruza acima da linha Supertrend e aplica alvos de lucro e perda configuráveis para gestão de risco.

## Detalhes

- **Critérios de entrada**: O preço cruza acima do Supertrend.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Take Profit ou Stop Loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `TakeProfitPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StartDate` = 2018-09-01
  - `EndDate` = 9999-01-01
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Supertrend
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
