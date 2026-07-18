# Estratégia Heikin Ashi ROC Percentil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia converte velas em Heikin Ashi, suaviza o fechamento com uma SMA e mede sua Rate of Change. Bandas de percentil dos máximos e mínimos recentes do ROC formam níveis de rompimento. Um cruzamento acima da banda inferior abre ou reverte para comprado, enquanto um cruzamento abaixo da banda superior reverte para vendido.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o ROC cruza acima da linha de percentil inferior.
  - Vendido: o ROC cruza abaixo da linha de percentil superior.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Stop percentual.
- **Valores padrão**:
  - `RocLength` = 100
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `StartDate` = new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Heikin Ashi, RateOfChange, Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
