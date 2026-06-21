# Estratégia VWAP Mean Magnet v9 (Alerta Simples)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta versão simplificada da estratégia VWAP Mean Magnet usa VWAP e RSI sem filtro de volume. As operações são abertas quando o preço se desvia do VWAP e o RSI atinge níveis extremos. As posições são encerradas quando o preço retorna ao VWAP.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: preço < VWAP e RSI < sobrevendido.
  - **Vendido**: preço > VWAP e RSI > sobrecomprado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Encerrar posição quando o preço retornar ao VWAP.
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Stop loss %` = 0.5
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Simples
  - Período: Intradiário
