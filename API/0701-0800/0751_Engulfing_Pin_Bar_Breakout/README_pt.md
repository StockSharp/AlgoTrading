# Estratégia de Rompimento Engulfing e Pin Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia aguarda um candle martelo ou engulfing altista e entra comprado no rompimento acima da máxima do sinal. Para configurações baixistas, usa estrela cadente ou engulfing baixista e vende no rompimento abaixo da mínima do sinal. O stop loss é colocado no lado oposto do candle de sinal e o take profit usa uma relação risco/recompensa.

## Detalhes

- **Critérios de entrada:** martelo ou engulfing altista seguido de rompimento acima da máxima; estrela cadente ou engulfing baixista seguido de rompimento abaixo da mínima.
- **Comprado/Vendido:** Ambos.
- **Critérios de saída:** stop no lado oposto do candle de sinal; take profit a múltiplo do risco.
- **Stops:** Sim.
- **Valores padrão:**
  - Ratio de lucro comprado = 5
  - Ratio de lucro vendido = 4
  - Percentual de risco = 0.02
  - Período do candle = 1 minuto
- **Filtros:**
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
