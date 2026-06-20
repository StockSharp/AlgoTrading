# Estratégia de Linhas de Tendência Automáticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Constrói linhas de tendência dinâmicas de suporte e resistência conectando os recentes máximos e mínimos de pivot. Um sinal de compra ocorre quando o preço fecha acima da linha de resistência, enquanto um sinal de venda é disparado quando o preço cai abaixo da linha de suporte.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento cruza acima da linha de tendência de resistência.
  - **Vendido**: Fechamento cruza abaixo da linha de tendência de suporte.
- **Critérios de saída**:
  - Sinal oposto ou reversão de posição.
- **Indicadores**:
  - Linhas de tendência baseadas em pivots.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LeftBars` = 100
  - `RightBars` = 15
- **Filtros**:
  - Seguidor de tendência
  - Período único
  - Indicadores: linhas de tendência pivot
  - Stops: nenhum
  - Complexidade: Baixo
