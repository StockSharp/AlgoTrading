# Estratégia Grid TLong V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em grade que mantém continuamente uma posição. Reentra em posições quando o lucro ou a perda atinge um passo percentual fixo.

## Detalhes

- **Critérios de entrada**: Sempre no mercado; reabrir posições nos passos da grade.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou reentrada após atingir o passo percentual.
- **Stops**: Não.
- **Valores padrão**:
  - `Percent` = 1
  - `UseLimitOrders` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Grade
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
