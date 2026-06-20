# Estratégia de Força de Bias e Sentimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia agrega múltiplos indicadores de momentum e volume (MACD, RSI, Stochastic, Awesome Oscillator, médias Alligator e bias de volume) em um único valor de bias. Uma posição comprada é aberta quando o bias combinado está acima de zero e uma posição vendida quando está abaixo de zero.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Bias combinado > 0.
  - **Vendido**: Bias combinado < 0.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Percentual de stop-loss via `StopLossPercent`.
- **Valores padrão**:
  - MACD rápido 12, lento 26, sinal 9.
  - Período RSI 14.
  - Períodos Stochastic 21/14/14.
  - Períodos Awesome Oscillator 5/34.
  - Comprimento do bias de volume 30.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Complexo
  - Período: Médio prazo
