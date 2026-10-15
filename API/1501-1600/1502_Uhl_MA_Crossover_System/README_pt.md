# Sistema Uhl MA Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Sistema Uhl MA Crossover constrói duas linhas adaptativas (CTS e CMA) usando variância para ajustar a suavização. Uma posição comprada é aberta quando CTS cruza acima de CMA e uma posição vendida quando cruza abaixo.

## Detalhes

- **Critérios de entrada**: CTS cruza acima de CMA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: CTS cruza abaixo de CMA.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 100
  - `Multiplier` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, Variance
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
