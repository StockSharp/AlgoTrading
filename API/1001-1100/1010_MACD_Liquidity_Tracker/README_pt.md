# Estratégia de Rastreamento de Liquidez MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

MACD Liquidity Tracker utiliza os estados de cor do MACD para gerar sinais de negociação. Quatro modos (Fast, Normal, Safe, Crossover) ajustam a sensibilidade dos sinais. Stop loss e take profit opcionais são suportados.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Depende de `SystemType` (padrão `Normal` usa MACD acima da linha de sinal).
  - **Vendido**: Depende de `SystemType` (padrão `Normal` usa MACD abaixo da linha de sinal).
- **Critérios de saída**: Sinal oposto.
- **Stops**: Stop loss e take profit opcionais.
- **Valores padrão**:
  - `FastLength` = 25
  - `SlowLength` = 60
  - `SignalLength` = 220
  - `AllowShortTrades` = false
  - `SystemType` = Normal
  - `UseStopLoss` = false
  - `StopLossPercent` = 3
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 6
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = tf(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado/Vendido
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
