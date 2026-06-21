# Estratégia de Sinais de Tendência com TP e SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza um canal baseado em ATR para determinar a direção da tendência. Uma nova tendência de alta começa quando o preço rompe acima da banda superior, acionando uma entrada comprada. Uma tendência de baixa começa quando o preço cai abaixo da banda inferior, acionando uma entrada vendida. Cada operação coloca ordens de stop-loss e take-profit usando multiplicadores de ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A tendência vira para cima.
  - **Vendido**: A tendência vira para baixo.
- **Saídas**: Stop-loss em `entry ∓ ATR * SL` e take-profit em `entry ± ATR * TP`.
- **Stops**: Sim, tanto stop-loss quanto take-profit.
- **Valores padrão**:
  - `Sensitivity` = 2
  - `ATR Length` = 14
  - `ATR TP Multiplier` = 2
  - `ATR SL Multiplier` = 1
