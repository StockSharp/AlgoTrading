# OsHMA Rompimento Twist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia construída sobre o oscilador OsHMA (diferença entre as Hull Moving Averages rápida e lenta). Pode operar em dois modos:

- **Breakdown** – negocia quando o oscilador cruza a linha zero.
- **Twist** – negocia quando o oscilador muda de direção.

A estratégia assina candles do período selecionado e usa indicadores de Hull Moving Average para calcular o oscilador.

## Detalhes

- **Critérios de entrada**: Cruzamento de zero do OsHMA ou mudança de direção.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Take profit e stop loss.
- **Valores padrão**:
  - `FastHma` = 13
  - `SlowHma` = 26
  - `Mode` = Twist
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
