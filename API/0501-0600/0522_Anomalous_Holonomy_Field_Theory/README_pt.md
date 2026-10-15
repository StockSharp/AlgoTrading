# Estratégia Anomalous Holonomy Field Theory
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina EMA, RSI, MACD, ATR, taxa de variação e distância ao VWAP em um sinal composto. Posições compradas são abertas quando o sinal supera um limiar definido pelo usuário, enquanto posições vendidas são abertas quando cai abaixo do limiar negativo. Um stop baseado em ATR protege as operações abertas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: sinal ≥ limiar.
  - **Vendido**: sinal ≤ −limiar.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop ATR.
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `SignalThreshold` = 2
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
