# Estratégia 20/200 Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre operações com base na diferença entre os preços de abertura de duas barras passadas. Entra comprado quando a abertura no shift2 menos a abertura no shift1 supera um limiar e entra vendido na condição oposta. As posições são abertas apenas em uma hora especificada e fechadas por take profit, stop loss ou após um tempo máximo de manutenção.

## Detalhes

- **Critérios de entrada:**
  - Comprado: open[Shift2] - open[Shift1] > DeltaLong pontos.
  - Vendido: open[Shift1] - open[Shift2] > DeltaShort pontos.
- **Comprado/Vendido:** Ambos.
- **Critérios de saída:** take profit, stop loss ou tempo máximo de manutenção.
- **Stops:** Stop loss e take profit fixos em pontos.
- **Valores padrão:**
  - Shift1 = 6
  - Shift2 = 2
  - DeltaLong = 6 pontos
  - DeltaShort = 21 pontos
  - TakeProfitLong = 390 pontos
  - StopLossLong = 1470 pontos
  - TakeProfitShort = 320 pontos
  - StopLossShort = 2670 pontos
  - TradeHour = 14
  - MaxOpenTime = 504 horas
  - Volume = 0.1
  - Período dos candles = 1 hora
- **Filtros:**
  - Categoria: Momentum
  - Direção: Comprado e Vendido
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Por hora
  - Sazonalidade: Baseada em tempo
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
