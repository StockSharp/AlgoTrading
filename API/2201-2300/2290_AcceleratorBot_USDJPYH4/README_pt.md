# Estratégia AcceleratorBot USDJPY H4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia AcceleratorBot é uma conversão do expert MQL4 original projetado para USDJPY no período H4. Ela combina a força da tendência do Índice Direcional Médio (ADX), o momentum do Oscilador Estocástico e os valores de Aceleração/Desaceleração (AC) em múltiplos períodos. Padrões de candles são utilizados como filtros direcionais.

## Detalhes

- **Critérios de entrada**: Sinais de tendência ou momentum confirmados por filtros de candles.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto, stop loss, take profit ou trailing stop.
- **Stops**: Fixo e Trailing.
- **Valores padrão**:
  - `StopLossPoints` = 750
  - `TakeProfitPoints` = 9999
  - `TrailPoints` = 0
  - `AdxPeriod` = 14
  - `AdxThreshold` = 20m
  - `X1` = 0
  - `X2` = 150
  - `X3` = 500
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência e momentum
  - Direção: Ambos
  - Indicadores: ADX, Stochastic, AC
  - Stops: Sim
  - Complexidade: Avançado
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
