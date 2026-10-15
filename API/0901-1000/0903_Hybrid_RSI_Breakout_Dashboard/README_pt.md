# Painel de Rompimento RSI Híbrido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina reversão à média do RSI com entradas de rompimento filtradas por ADX e uma EMA de 200.

O sistema compra quando o mercado está em consolidação e o RSI cai abaixo de `RsiBuy` na tendência altista da EMA. Vende a descoberto quando o RSI sobe acima de `RsiSell` na tendência de baixa. No regime de tendência, entra em rompimentos acima/abaixo de fechamentos recentes e rastreia a posição usando ATR.

Inclui um filtro de data de início e variáveis simples de painel para o último tipo de trade e direção.

## Detalhes

- **Critérios de entrada**: Sinais de RSI no regime de consolidação com viés de EMA, ou rompimentos acima/abaixo dos `BreakoutLength` fechamentos anteriores quando ADX > `AdxThreshold`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Trades de RSI saem no `RsiExit`. Trades de rompimento usam trailing stop ATR.
- **Stops**: Trailing stop ATR para trades de rompimento.
- **Valores padrão**:
  - `AdxLength` = 14
  - `AdxThreshold` = 20m
  - `EmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuy` = 40m
  - `RsiSell` = 60m
  - `RsiExit` = 50m
  - `BreakoutLength` = 20
  - `AtrLength` = 14
  - `AtrMultiplier` = 2m
  - `StartDate` = 2017-01-01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência, Reversão à média
  - Direção: Ambos
  - Indicadores: ADX, EMA, RSI, ATR, Highest/Lowest
  - Stops: Trailing
  - Complexidade: Moderado
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
