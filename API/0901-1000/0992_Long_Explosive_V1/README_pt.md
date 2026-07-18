# Estratégia Long Explosive V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Long Explosive V1 abre uma posição comprada quando o preço de fechamento sobe um percentual definido em relação à barra anterior. A posição é encerrada quando o preço cai o percentual configurado ou antes de abrir uma nova operação comprada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close - PrevClose > Close * Price increase (%) / 100`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: `Close - PrevClose < -Close * Price decrease (%) / 100` ou antes de uma nova entrada comprada.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Price increase (%)` = 1
  - `Price decrease (%)` = 1
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: Preço
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
