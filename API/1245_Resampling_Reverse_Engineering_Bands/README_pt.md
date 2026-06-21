# Bandas de Engenharia Reversa com Reamostagem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

As Bandas de Engenharia Reversa com Reamostagem realizam engenharia reversa dos níveis de preço do RSI a uma taxa de reamostagem configurável. A estratégia compra quando o preço cai abaixo da banda baixa e vende quando o preço sobe acima da banda alta.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: o preço de fechamento cruza abaixo da banda baixa RRSI.
  - **Vendido**: o preço de fechamento cruza acima da banda alta RRSI.
- **Critérios de saída**: sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `HighThreshold` = 70
  - `LowThreshold` = 30
  - `SampleLength` = 1
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado e Vendido
  - Indicadores: RSI
  - Complexidade: Moderado
  - Nível de risco: Médio
