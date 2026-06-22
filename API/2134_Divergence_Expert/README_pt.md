# Especialista em Divergência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera divergências de preço com RSI. Detecta divergência de alta quando o preço forma uma mínima mais baixa, mas o RSI forma uma mínima mais alta, e divergência de baixa quando o preço forma uma máxima mais alta, mas o RSI forma uma máxima mais baixa. Entra em posições compradas ou vendidas de acordo e utiliza um stop loss percentual.

## Detalhes

- **Critérios de entrada:**
  - Comprado: o preço forma uma nova mínima e o RSI forma uma mínima mais alta (divergência de alta)
  - Vendido: o preço forma uma nova máxima e o RSI forma uma máxima mais baixa (divergência de baixa)
- **Comprado/Vendido:** Ambos
- **Critérios de saída:**
  - Comprado: o preço atinge o stop loss ou aparece divergência de baixa
  - Vendido: o preço atinge o stop loss ou aparece divergência de alta
- **Stops:** Percentual do preço de entrada
- **Valores padrão:**
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros:**
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
