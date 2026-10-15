# Estratégia ALMA Optimized
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma média móvel Arnaud Legoux com uma EMA de longo prazo, ADX, RSI e Bandas de Bollinger. Um filtro baseado em ATR garante volatilidade suficiente. As posições utilizam múltiplos de ATR para stop-loss e take-profit, com uma saída opcional baseada em tempo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: ATR acima do limiar, fechamento acima da EMA e ALMA, RSI > 30, ADX > 30, fechamento abaixo da banda superior de Bollinger e cooldown concluído.
  - **Vendido**: O fechamento cruza abaixo da EMA rápida sob o mesmo filtro de volatilidade.
- **Critérios de saída**:
  - Stop-loss ou take-profit baseados em múltiplos de ATR.
  - Saída opcional baseada em tempo em barras.
- **Valores padrão**:
  - EMA rápida = 20.
  - Comprimento ATR = 14.
  - Comprimento EMA = 72.
  - Comprimento ADX = 10.
  - Comprimento RSI = 14.
  - Cooldown = 7 barras.
  - Multiplicador Bollinger = 3.0.
  - Multiplicador ATR de stop = 5.0.
  - Multiplicador ATR de alvo = 4.0.
  - Saída temporal = 0.
  - ATR mínimo = 0.005.
- **Filtros**:
  - Categoria: Tendência + Momentum
  - Direção: Ambos
  - Indicadores: EMA, ALMA, ADX, RSI, ATR, Bollinger Bands
  - Stops: Baseado em ATR
  - Complexidade: Moderado
  - Período: Curto/médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
