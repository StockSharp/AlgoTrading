# Estratégia de Tendência da Teoria de Dow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Tendência da Teoria de Dow utiliza máximas e mínimas pivô para determinar a direção da tendência. A estratégia entra comprado quando aparecem tanto máximas mais altas quanto mínimas mais altas, e entra vendido quando se formam tanto máximas mais baixas quanto mínimas mais baixas.

## Detalhes

- **Critérios de entrada**: Máximas e mínimas mais altas para comprado; máximas e mínimas mais baixas para vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `PivotLookback` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Ação do preço
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
