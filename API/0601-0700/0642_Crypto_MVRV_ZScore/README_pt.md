# Estratégia Crypto MVRV ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia aplica o conceito MVRV Z-Score para detectar extremos entre o valor de mercado e o valor realizado.
As posições são abertas quando o z-score do spread cruza limiares predefinidos e fechadas em cruzamentos opostos.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando o z-score do spread cruza acima de `LongEntryThreshold`.
  - Vendido quando o z-score do spread cruza abaixo de `ShortEntryThreshold`.
- **Comprado/Vendido**: Configurável (`TradeDirection`).
- **Critérios de saída**:
  - Cruzamento do limiar oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `ZScoreCalculationPeriod` = 252
  - `LongEntryThreshold` = 0.382
  - `ShortEntryThreshold` = -0.382
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation, Z-Score
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
