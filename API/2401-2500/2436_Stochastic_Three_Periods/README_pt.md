# Stochastic Três Períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Stochastic Três Períodos** alinha sinais rápidos do estocástico com confirmação de dois períodos temporais superiores. As operações são abertas quando o oscilador rápido cruza enquanto ambos os períodos temporais superiores concordam.

## Detalhes

- **Critérios de entrada**: %K rápido cruza %D com leitura oposta há `ShiftEntrance` barras atrás; ambos os estocásticos de períodos temporais superiores mostram %K acima de %D; o preço de fecho deve mover-se na direção do sinal.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto do estocástico rápido medido na vela anterior.
- **Stops**: Stop loss e take profit fixos em pontos via `StartProtection`.
- **Valores padrão**:
  - `CandleType1` = 5m
  - `CandleType2` = 15m
  - `CandleType3` = 30m
  - `KPeriod1` = 5
  - `KPeriod2` = 5
  - `KPeriod3` = 5
  - `KExitPeriod` = 5
  - `ShiftEntrance` = 3
  - `TakeProfitPoints` = 30
  - `StopLossPoints` = 10
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
