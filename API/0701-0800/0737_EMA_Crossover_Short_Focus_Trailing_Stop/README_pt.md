# Estratégia de Cruzamento EMA com Foco Vendido e Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia vai comprado quando a EMA de 13 está acima da EMA de 33 e não existe posição vendida. Vai vendido quando a EMA de 13 está abaixo da EMA de 33 e nenhuma posição comprada está aberta. As posições saem quando a EMA de 13 cruza a EMA oposta e um stop trailing acompanha os extremos recentes.

## Detalhes
- **Critérios de entrada:**
  - **Comprado:** EMA de 13 ≥ EMA de 33 e posição ≤ 0.
  - **Vendido:** EMA de 13 ≤ EMA de 33 e posição ≥ 0.
- **Comprado/Vendido:** ambos.
- **Critérios de saída:** comprado sai quando EMA de 13 < EMA de 33; vendido sai quando EMA de 13 > EMA de 25.
- **Stops:** stop trailing com distância `TrailDistance` e deslocamento `TrailOffset`.
- **Valores padrão:** short EMA = 13, mid EMA = 25, long EMA = 33, trail distance = 10, trail offset = 2.
