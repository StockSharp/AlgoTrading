# Estratégia Extrapolated Pivot Connector
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conecta máximas e mínimas de pivôs recentes para construir linhas de suporte e resistência. Um sinal de compra ocorre quando o preço fecha acima da linha de resistência, enquanto um sinal de venda é acionado quando o preço cai abaixo da linha de suporte.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento cruza acima da linha de resistência.
  - **Vendido**: Fechamento cruza abaixo da linha de suporte.
- **Critérios de saída**:
  - Sinal oposto ou reversão de posição.
- **Indicadores**:
  - Linhas de suporte/resistência baseadas em pivôs.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `PivotLength` = 100
  - `HighStart` = 1
  - `HighEnd` = 0
  - `LowStart` = 1
  - `LowEnd` = 0
- **Filtros**:
  - Seguidor de tendência
  - Período único
  - Indicadores: linhas de pivô
  - Stops: nenhum
  - Complexidade: Baixo
