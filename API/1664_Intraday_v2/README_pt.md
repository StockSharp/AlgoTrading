# Estratégia Intradiária v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma abordagem de reversão à média intradiária usando dois conjuntos de Bandas de Bollinger. As bandas externas (desvio 2.4) definem as zonas de entrada, enquanto as bandas internas (desvio 1) gerenciam as saídas. Níveis opcionais de stop-loss e take-profit fecham posições quando o preço se move contra a operação em um valor configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento cai abaixo da banda inferior externa.
  - **Vendido**: O preço de fechamento sobe acima da banda superior externa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Comprado: O preço cruza acima da banda inferior interna ou atinge o stop-loss/take-profit.
  - Vendido: O preço cruza abaixo da banda superior interna ou atinge o stop-loss/take-profit.
- **Stops**: Stop-loss e take-profit absolutos configuráveis.
- **Filtros**: Nenhum.
