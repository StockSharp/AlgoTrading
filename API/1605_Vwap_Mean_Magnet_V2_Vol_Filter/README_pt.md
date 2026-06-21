# Estratégia VWAP Mean Magnet v2 (Filtro de Volume)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina o conceito de reversão à média do VWAP com RSI e um filtro de volume. As operações são realizadas quando o preço se desvia do VWAP e o RSI atinge níveis extremos, desde que o volume atual esteja acima de uma média móvel multiplicada por um fator.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: preço < VWAP, RSI < sobrevendido, filtro de volume aprovado.
  - **Vendido**: preço > VWAP, RSI > sobrecomprado, filtro de volume aprovado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Encerrar posição quando o preço retornar ao VWAP.
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Volume lookback` = 20
  - `Volume multiplier` = 3
  - `Stop loss %` = 0.5
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
