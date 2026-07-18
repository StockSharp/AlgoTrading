# Estratégia de Tabela de Desempenho Mensal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera quando o ADX está entre +DI e -DI e ambas as diferenças em relação ao ADX excedem limites configuráveis.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando |+DI - ADX| ≥ `LongDifference` e |-DI - ADX| ≥ `LongDifference` com ADX entre +DI e -DI.
  - Vendido quando |+DI - ADX| ≥ `ShortDifference` e |-DI - ADX| ≥ `ShortDifference` com ADX entre -DI e +DI.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX, DMI
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
