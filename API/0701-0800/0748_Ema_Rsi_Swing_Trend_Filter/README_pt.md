# Filtro de Tendência EMA RSI Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera o cruzamento de EMA20 e EMA50 na direção do filtro de tendência EMA200.
Um filtro RSI opcional limita entradas compradas quando o RSI está sobrecomprado e vendidas quando está sobrevendido.

## Detalhes

- **Critérios de entrada**: EMA20 cruza EMA50 com o preço relativo à EMA200 e filtro RSI opcional.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Saída opcional no cruzamento EMA oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
