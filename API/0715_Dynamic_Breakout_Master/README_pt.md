# Estratégia Dinâmica de Rompimento Mestre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento usando Canais de Donchian com filtro de tendência de média móvel, filtros RSI e ATR, além de restrições de volume e tempo.

## Regras da estratégia

- Comprado: o preço rompe acima da banda superior de Donchian ou recua após o rompimento, MA1 > MA2, RSI entre `RsiOversold` e `RsiOverbought`, ATR acima de `AtrMultiplier`, volume acima da média e dentro do horário de operação.
- Vendido: o preço rompe abaixo da banda inferior de Donchian ou recua após o rompimento, MA1 < MA2, RSI entre os limites, ATR acima de `AtrMultiplier`, volume acima da média e dentro do horário de operação.
- Saídas: stop loss/trailing, take profit, RSI extremo ou cruzamento de médias móveis.

## Parâmetros

- `DonchianPeriod` – período de retrocesso do canal.
- `Ma1Length`, `Ma1IsEma` – primeira média móvel.
- `Ma2Length`, `Ma2IsEma` – segunda média móvel.
- `RsiLength`, `RsiOverbought`, `RsiOversold` – filtro RSI.
- `AtrLength`, `AtrMultiplier` – filtro de volatilidade.
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – dimensionamento de posição.
- `TradingStartHour`, `TradingEndHour` – sessão de negociação.
- `CandleType` – período de tempo das velas.
