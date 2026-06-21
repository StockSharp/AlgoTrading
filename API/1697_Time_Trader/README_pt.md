# Estratégia de Trader por Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no tempo que entra comprado e/ou vendido exatamente em uma hora de relógio especificada e protege a posição com take profit e stop loss configuráveis.

## Detalhes

- **Critérios de entrada**: Em `TradeHour:TradeMinute:TradeSecond` abrir comprado se `AllowBuy`, vendido se `AllowSell`.
- **Comprado/Vendido**: Ambos, dependendo das configurações
- **Critérios de saída**: posição fechada via stop loss ou take profit
- **Stops**: Sim, ambos
- **Valores padrão**:
  - `Volume` = 1
  - `TakeProfit` = 0.2
  - `StopLoss` = 0.2
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `TradeSecond` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `CandleType` = TimeSpan.FromSeconds(1).TimeFrame()
- **Filtros**:
  - Categoria: Tempo
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

