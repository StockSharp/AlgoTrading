# Estratégia de Amplitude de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Bar Range entra comprada quando a amplitude da barra atual está entre as mais altas das barras recentes e a vela fecha abaixo de sua abertura. A posição é encerrada após um número fixo de barras.

## Detalhes

- **Critérios de entrada**:
  - Amplitude = High − Low
  - Rank percentual da amplitude em `LookbackPeriod` ≥ `PercentRankThreshold`
  - Close < Open
- **Critérios de saída**: Encerrar posição após `ExitBars` barras.
- **Valores padrão**:
  - `LookbackPeriod` = 50
  - `PercentRankThreshold` = 95
  - `ExitBars` = 1
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Comprado
  - Indicadores: Percent Rank
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
