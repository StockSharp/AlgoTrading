# Estratégia Entry Fragger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia rastreia sequências de velas vermelhas e verdes em relação à EMA de 50 períodos. Após uma série de velas vermelhas abaixo da EMA, uma vela verde que fecha acima de uma nuvem de volatilidade aciona uma entrada comprada. Uma configuração similar com velas verdes precede as entradas vendidas. O trading reverso opcional permite inverter posições.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `redCount >= Buy Signal Accuracy` && última vermelha abaixo da EMA50 && vela verde fecha acima de `EMA50 + stdev/4`.
  - **Vendido**: `greenCount >= Sell Signal Accuracy` && vela anterior verde && vela vermelha fecha acima de `EMA50 + stdev/4`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal inverso.
- **Indicadores**: EMA, StandardDeviation.
- **Valores padrão**:
  - `Buy Signal Accuracy` = 2
  - `Sell Signal Accuracy` = 2
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
