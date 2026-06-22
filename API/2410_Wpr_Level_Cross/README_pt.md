# Estratégia WPR de Cruzamento de Nível
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no oscilador Williams %R ao cruzar níveis predefinidos de sobrecompra e sobrevenda.

Quando o indicador cruza abaixo do **Low Level**, sinaliza uma possível reversão de uma condição de sobrevenda. Quando cruza acima do **High Level**, indica uma possível reversão de uma condição de sobrecompra. Dependendo do **Trend Mode** selecionado, a estratégia pode operar na direção do indicador ou inverter os sinais para negociação contra-tendência.

## Parâmetros

- `WprPeriod` – período de lookback para Williams %R.
- `HighLevel` – limite de sobrecompra.
- `LowLevel` – limite de sobrevenda.
- `Trend` – `Direct` opera com os sinais do indicador, `Against` os inverte.
- `EnableBuyEntry` / `EnableSellEntry` – permitir entrar em posições compradas/vendidas.
- `EnableBuyExit` / `EnableSellExit` – permitir fechar posições vendidas/compradas.
- `StopLoss` – valor do stop-loss em unidades de preço.
- `TakeProfit` – valor do take-profit em unidades de preço.
- `CandleType` – período dos candles usados para cálculos.

## Como Funciona

1. A estratégia subscreve candles e calcula o indicador Williams %R.
2. Em cada candle concluído, verifica se o indicador cruzou os níveis especificados.
3. Dependendo de `Trend` e das ações habilitadas, abre ou fecha posições usando ordens de mercado.
4. A proteção opcional de stop-loss e take-profit é ativada através de `StartProtection`.

## Notas

- Os comentários no código estão em inglês.
- Apenas a versão C# está implementada; a versão Python é omitida intencionalmente.
