# Cruzamento MACD AUDUSD D1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera AUDUSD no período diário usando cruzamentos das linhas MACD.

A estratégia abre uma posição comprada quando a linha principal do MACD cruza acima da linha de sinal e uma posição vendida quando cruza abaixo. As operações são permitidas apenas entre 06:00 e 14:00 horário do servidor, e apenas uma posição pode estar aberta por vez. Cada operação define um stop loss de 40 pips e um take profit três vezes maior por padrão.

## Detalhes

- **Critérios de entrada**: A linha principal do MACD cruza a linha de sinal.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `Volume` = 0.1
  - `StopLossPips` = 40
  - `RewardRatio` = 3
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
