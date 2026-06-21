# Estratégia Collector v1.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia abre ordens a mercado quando o preço atinge níveis dinâmicos de compra ou venda separados por uma distância fixa. O volume aumenta após um número especificado de operações. Todas as posições são encerradas quando o lucro acumulado supera um limiar.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço de fechamento >= nível de compra
  - Vendido: preço de fechamento <= nível de venda
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Encerrar tudo quando o lucro total >= ProfitClose
- **Stops**: Nenhum
- **Valores padrão**:
  - `Distance` = 10m
  - `InitialVolume` = 0.01m
  - `VolumeStep` = 0.01m
  - `IncreaseTrade` = 3
  - `MaxTrades` = 200
  - `ProfitClose` = 500000m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Grade
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
