# Estratégia ATR Aleatória - Bybit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia gera um sinal aleatório determinístico baseado nos intervalos de preços recentes e na data atual. Entra comprado quando o sinal é 1 e vendido quando é 0. A gestão de risco usa níveis de stop-loss e take-profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o sinal aleatório é igual a 1.
  - **Vendido**: o sinal aleatório é igual a 0.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: stop-loss ou take-profit.
- **Stops**: `SlAtrRatio * ATR` para stop-loss, take-profit em `SlAtrRatio * TpSlRatio * ATR`.
- **Valores padrão**:
  - `AtrLength` = 14
  - `SlAtrRatio` = 3
  - `TpSlRatio` = 1
- **Filtros**: nenhum.
- **Complexidade**: simples.
- **Período**: configurável.
