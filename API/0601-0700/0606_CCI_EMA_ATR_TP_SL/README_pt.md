# Estratégia CCI + EMA com TP/SL Percentual ou ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o Commodity Channel Index (CCI) com um filtro de tendência EMA opcional e confirmação RSI.
As posições são abertas quando o CCI sai de zonas extremas e os filtros opcionais permitem a negociação.
O take profit e o stop loss podem ser calculados como percentuais do preço de entrada ou usando níveis baseados em ATR com uma relação risco-recompensa.

## Detalhes

- **Condições de entrada:**
  - **Comprado:** CCI cruza acima do nível de sobrevenda, preço acima da EMA (se ativado), RSI abaixo de sobrevenda (se ativado).
  - **Vendido:** CCI cruza abaixo do nível de sobrecompra, preço abaixo da EMA (se ativado), RSI acima de sobrecompra (se ativado).
- **Condições de saída:**
  - Níveis de take-profit ou stop-loss atingidos.
  - Posições compradas fecham quando o CCI cruza acima do nível de sobrecompra.
  - Posições vendidas fecham quando o CCI cruza abaixo do nível de sobrevenda.

Os parâmetros padrão seguem o script original.
