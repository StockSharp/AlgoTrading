# Estratégia Xmacd com Modos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador MACD que suporta quatro modos de entrada diferentes:

- **Breakdown**: abrir operações quando o MACD cruza a linha zero.
- **MacdTwist**: reagir a uma mudança na direção do MACD de descendente para ascendente ou vice-versa.
- **SignalTwist**: usar os pontos de inflexão da linha de sinal como gatilhos.
- **MacdDisposition**: operar nos cruzamentos entre o MACD e sua linha de sinal.

A estratégia assina velas de 4 horas e calcula um MACD clássico (EMA 12/26 com sinal de 9 períodos). Pode abrir e fechar posições em sinais opostos. O risco é gerenciado por stop loss e take-profit opcionais expressos como porcentagens do preço de entrada.

## Detalhes

- **Critérios de entrada**: Sinais baseados no MACD dependendo do modo selecionado.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
  - `Mode` = MacdDisposition
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Swing (4h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
