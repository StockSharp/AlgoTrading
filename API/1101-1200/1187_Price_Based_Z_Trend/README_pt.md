# Estratégia de Price Based Z-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera com base no Z-score do preço relativo a uma EMA. Entra quando o Z-score cruza limiares definidos pelo usuário e suporta direções comprada, vendida ou ambas.

## Detalhes

- **Critérios de entrada**:
  - Z-score cruza acima de `Threshold` para comprado.
  - Z-score cruza abaixo de `-Threshold` para vendido.
- **Comprado/Vendido**: Configurável via `TradeDirection`.
- **Critérios de saída**: Cruzamento do limiar oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `PriceDeviationLength` = 100
  - `PriceAverageLength` = 100
  - `Threshold` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: EMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: 5 minutos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
