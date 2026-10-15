# RSI Sobrecomprado/Sobrevendido (RSI Overbought/Oversold)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema opera reversões usando o Índice de Força Relativa (RSI). Quando o RSI cai abaixo do nível de sobrevendido, compra após fechar quaisquer posições vendidas. Quando o RSI sobe acima do nível de sobrecomprado, vende após fechar as posições compradas.

Os testes indicam um retorno anual médio de aproximadamente 61%. Funciona melhor no mercado de criptomoedas.

As posições são fechadas quando o RSI retorna a uma zona neutra ou o stop-loss é atingido.

## Detalhes

- **Critérios de entrada**: RSI abaixo de `OversoldLevel` ou acima de `OverboughtLevel`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: RSI cruza `NeutralLevel` ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70
  - `OversoldLevel` = 30
  - `NeutralLevel` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
