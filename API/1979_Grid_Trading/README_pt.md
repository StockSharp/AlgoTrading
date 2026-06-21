# Estratégia de Grid Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema básico de grid trading. Ela coloca ordens buy stop e sell stop em intervalos de preço fixos definidos por `GridStep`. Cada ordem executada usa uma distância de take profit fixa. Um alvo de lucro global fecha todas as posições e reinicia o grid. Opcionalmente, o volume de novas ordens aumenta seguindo um esquema martingale.

## Detalhes

- **Critérios de entrada:**
  - Buy stop um passo acima do último preço.
  - Sell stop um passo abaixo do último preço.
- **Comprado/Vendido:** Ambos.
- **Critérios de saída:**
  - Cada posição fecha no take profit fixo.
  - Quando o lucro total supera `ProfitTarget`, todas as ordens e posições são fechadas.
- **Stops:** Apenas take profit.
- **Filtros:** Nenhum.
