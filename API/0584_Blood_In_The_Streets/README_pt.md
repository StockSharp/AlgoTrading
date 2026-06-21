# Estratégia Sangue nas Ruas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando o drawdown atual a partir da máxima recente cai abaixo de um limiar de desvio padrão. A posição é fechada após um número fixo de barras.

## Detalhes

- **Critérios de entrada**:
  - Comprado: drawdown ≤ média + `StdDevThreshold` × desvio padrão
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: posição fechada após `ExitBars` barras
- **Stops**: Nenhum
- **Valores padrão**:
  - `LookbackPeriod` = 50
  - `StdDevLength` = 50
  - `StdDevThreshold` = -1m
  - `ExitBars` = 35
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: Highest, SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
