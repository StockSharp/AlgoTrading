# Estratégia de OPR Binned Filtrado por Sigma Spike
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sigma Spike Filtered Binned OPR coleta a distribuição da proporção de posições abertas (OPR) e opera quando a OPR atinge bins extremos após um pico sigma nos retornos.

## Detalhes

- **Critérios de entrada**: OPR em bins extremos (<= `OprThreshold` ou >= `100 - OprThreshold`) com filtro de pico sigma opcional
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal contrário
- **Stops**: Não
- **Valores padrão**:
  - `SigmaSpikeLength` = 20
  - `FilterBySigmaSpike` = true
  - `SigmaSpikeThreshold` = 2
  - `OprThreshold` = 10
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
