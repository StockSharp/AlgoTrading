# Estratégia de Cruzamento SMA EMA Refinado com Ichimoku e Filtro de 200 SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina um cruzamento curto de SMA/EMA com filtros de Ichimoku Cloud e SMA de 200 períodos. Vai comprado quando SMA cruza acima da EMA, acima da nuvem e da SMA 200. Vende quando SMA cruza abaixo da EMA, abaixo da nuvem e da SMA 200.

## Detalhes

- **Critérios de entrada:**
  - **Comprado:** SMA cruza acima da EMA, preço acima da Ichimoku Cloud, preço acima da SMA 200.
  - **Vendido:** SMA cruza abaixo da EMA, preço abaixo da Ichimoku Cloud, preço abaixo da SMA 200.
- **Critérios de saída:** sinal inverso.
- **Indicadores:** Ichimoku Cloud, SMA, EMA, 200 SMA.
