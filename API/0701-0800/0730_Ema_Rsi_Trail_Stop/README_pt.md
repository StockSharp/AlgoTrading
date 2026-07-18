# Estratégia EMA RSI com Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de EMA curta e média filtrados por uma EMA longa. Níveis de RSI fecham operações, e um stop trailing com stop-loss fixo gerencia o risco. As operações podem ser encerradas opcionalmente após um número de barras se forem lucrativas.

## Detalhes

- **Critérios de entrada**: EMA A cruzando EMA B com tendência confirmada por EMA C e direção do candle.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Limiares de RSI, stop trailing ou saída por tempo.
- **Stops**: Stop fixo em percentual que se converte em trailing stop após o preço mover `TrailOffset`.
- **Valores padrão**:
  - `EmaALength` = 10
  - `EmaBLength` = 20
  - `EmaCLength` = 100
  - `RsiLength` = 14
  - `ExitLongRsi` = 70
  - `ExitShortRsi` = 30
  - `TrailPoints` = 50
  - `TrailOffset` = 10
  - `FixStopLossPercent` = 5
  - `CloseAfterXBars` = true
  - `XBars` = 24
  - `ShowLong` = true
  - `ShowShort` = false
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
