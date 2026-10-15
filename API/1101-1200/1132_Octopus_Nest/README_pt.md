# Estratégia Octopus Nest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia busca rompimentos de compressão usando Bandas de Bollinger e Canais de Keltner. A direção é confirmada com EMA e Parabolic SAR. Os stops são colocados em máximas/mínimas recentes com uma relação risco/recompensa configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço acima de EMA e PSAR, fora da compressão.
  - **Vendido**: Preço abaixo de EMA e PSAR, fora da compressão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss nos extremos recentes e take-profit com base na relação risco/recompensa.
- **Stops**: Sim, fixo pela máxima/mínima recente.
- **Filtros**: Compressão Bollinger/Keltner, tendência EMA, direção PSAR.
