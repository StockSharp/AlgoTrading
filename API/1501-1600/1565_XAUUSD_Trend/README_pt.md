# Estratégia de Tendência XAUUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera XAUUSD usando cruzamentos de EMA, extremos de RSI e Bollinger Bands.
Uma posição comprada é aberta quando a EMA rápida cruza acima da lenta, o RSI está abaixo do nível de sobrevenda e o preço fecha acima da banda superior de Bollinger.
Posições vendidas são abertas nas condições opostas.
O gerenciamento de risco define níveis de stop-loss e take-profit com base na porcentagem de risco do portfólio e numa relação take-profit/stop-loss.

## Detalhes

- **Entrada**:
  - Comprado: cruzamento ascendente de EMA rápida, RSI < oversold, close > banda superior.
  - Vendido: cruzamento descendente de EMA rápida, RSI > overbought, close < banda inferior.
- **Saída**: stop-loss ou take-profit calculados a partir das configurações de risco.
- **Indicadores**: EMA, RSI, Bollinger Bands.
