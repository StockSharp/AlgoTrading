# Estratégia de Modelos de Crescimento Automático Simplista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia forma bandas de média acumulada de máximas e mínimas e opera quando o preço rompe esses níveis.

## Detalhes

- **Critérios de entrada**:
  - Preço de fechamento acima da banda superior abre uma posição comprada.
  - Preço de fechamento abaixo da banda inferior abre uma posição vendida.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal oposto fecha a posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 10
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
