# Estratégia Manadi Compra/Venda EMA MACD RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de EMA com confirmações de MACD e RSI. Entradas a mercado com stop-loss e take-profit fixos em percentual.

## Detalhes

- **Critérios de entrada**: Cruzamento de EMA com concordância do MACD e limites do RSI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit baseados em percentual.
- **Stops**: Baseados em percentual.
- **Valores padrão**:
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, MACD, RSI
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
