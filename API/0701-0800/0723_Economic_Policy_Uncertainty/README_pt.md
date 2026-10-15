# Estratégia de Incerteza da Política Econômica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Incerteza da Política Econômica (EPU) assume posição comprada quando a SMA de dois períodos do índice EPU cruza acima de um limiar definido pelo usuário. Após entrar na posição, a estratégia aguarda um número fixo de barras antes de fechá-la.

Esta abordagem busca capturar momentos em que a incerteza de política supera os níveis normais.

## Detalhes

- **Critérios de entrada**: SMA cruza acima do limiar.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Saída após o número especificado de barras.
- **Stops**: Não.
- **Valores padrão**:
  - `Threshold` = 187
  - `SmaLength` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
