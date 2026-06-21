# Estratégia de Testador de Impulso de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Trend Impulse Tester entra em operações quando uma tendência forte é confirmada por EMAs e ADX e surge um impulso RSI.
Compra em impulsos de alta durante tendências de alta e vende em impulsos de baixa durante tendências de baixa.

## Detalhes

- **Critérios de entrada**: tendência EMA + confirmação ADX com RSI cruzando o limiar
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `AdxLength` = 14
  - `AdxMin` = 18
  - `RsiLength` = 14
  - `RsiUp` = 55
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, ADX, RSI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
