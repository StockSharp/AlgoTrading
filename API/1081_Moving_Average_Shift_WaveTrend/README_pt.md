# Estratégia de Média Móvel Shift WaveTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma média móvel configurável com um oscilador estilo WaveTrend. Operações compradas ocorrem quando o preço está acima da média móvel e o oscilador sobe, confirmando uma tendência de alta com uma EMA de longo prazo e filtro de volatilidade. Posições vendidas são acionadas nas condições opostas. As posições são protegidas por stop loss, take profit e trailing stop percentuais.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: preço acima da MA, oscilador > 0 e subindo, tendência de longo prazo para cima, ATR acima de sua média, dentro do horário de negociação, não já em onda.
  - **Vendido**: preço abaixo da MA, oscilador < 0 e caindo, tendência de longo prazo para baixo, ATR acima de sua média, dentro do horário de negociação, não já em onda.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Reversão do oscilador com cruzamento de preço e MA, ou trailing stop, ou proteções.
- **Stops**: Sim.
- **Valores padrão**:
  - `MaType` = SMA
  - `MaLength` = 40
  - `OscLength` = 15
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1
  - `TrailPercent` = 1
  - `LongMaLength` = 200
  - `AtrLength` = 14
  - `StartHour` = 9
  - `EndHour` = 17
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, Hull MA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
