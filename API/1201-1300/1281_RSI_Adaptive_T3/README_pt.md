# RSI Adaptativo T3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguidor de tendência baseada na média móvel T3 de Tillson adaptada ao RSI. Entra comprado quando o T3 cruza acima de seu valor de duas barras atrás e sai no cruzamento oposto.

Backtests em gráficos diários mostram desempenho estável em mercados com tendência.

## Detalhes

- **Critérios de entrada**: T3 cruza acima de seu valor de 2 barras atrás.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `RsiLength` = 14
  - `MinT3Length` = 5
  - `MaxT3Length` = 50
  - `VolumeFactor` = 0.7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: RSI, T3
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
