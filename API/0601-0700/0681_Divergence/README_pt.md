# Estratégia de Divergência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na divergência entre preço e RSI com detecção simples de pivôs.

A Estratégia de Divergência usa máximas e mínimas de pivô no preço e no RSI para detectar divergências de alta e de baixa. Quando o preço faz uma nova máxima mas o RSI não confirma, a estratégia vende. Por outro lado, quando o preço faz uma nova mínima enquanto o RSI sobe, compra.

## Detalhes

- **Critérios de entrada**: Divergências entre preço e RSI.
- **Comprado/Vendido**: Ambas as direções (configurável).
- **Critérios de saída**: Sinal oposto do RSI ou ordens de proteção.
- **Stops**: Sim (stop loss e take profit).
- **Valores padrão**:
  - `TradeDirection` = Both
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
