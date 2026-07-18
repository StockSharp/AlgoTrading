# Estratégia Trend Magic com EMA, SMA e Auto-Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa a linha Trend Magic baseada em CCI junto com os filtros EMA(45), SMA(90) e SMA(180). Uma operação comprada é aberta quando Trend Magic muda para azul durante um alinhamento de alta das médias móveis. Operações vendidas ocorrem quando a linha fica vermelha e as médias alinham em baixa. Cada posição tem um stop no SMA90 e um take profit baseado em uma relação risco/recompensa fixa.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `EMA45 > SMA90 > SMA180` e Trend Magic fica azul.
  - **Vendido**: `EMA45 < SMA90 < SMA180` e Trend Magic fica vermelho.
- **Saídas**: Stop-loss no SMA90 capturado na entrada e take-profit em `entry ± risk * ratio`.
- **Stops**: Tanto stop-loss quanto take-profit.
- **Valores padrão**:
  - `CCI Period` = 21
  - `ATR Period` = 7
  - `ATR Multiplier` = 1.0
  - `Risk Reward` = 1.5
