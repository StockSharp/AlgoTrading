# Estratégia de Tendência Quantitativa — Comprado em Tendência de Alta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando o preço fecha acima do máximo pivô mais recente detectado em janelas de lookback configuráveis. Os níveis de suporte e resistência são obtidos a partir de máximos e mínimos pivô. As posições são protegidas por take-profit e stop-loss baseados em percentual.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento cruza acima do último máximo pivô.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O preço de fechamento cruza abaixo do último mínimo pivô.
  - O último máximo pivô torna-se inferior ao último mínimo pivô.
- **Stops**: Sim, take-profit e stop-loss em percentual.
- **Valores padrão**:
  - `PivotLeft` = 46
  - `PivotRight` = 32
  - `StopLossPercent` = 1.75
  - `TakeProfitPercent` = 2
