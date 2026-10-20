# BBTrend SuperTrend Decision

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Duas bandas de Bollinger são calculadas sobre fechamentos concluídos: 20 e 50 barras, com duas vezes o desvio padrão populacional. Para bandas inferiores `Ls/Ll`, superiores `Us/Ul` e média curta `Ms`: `BBTrend = 100 * (abs(Ls-Ll) - abs(Us-Ul)) / Ms`.

SuperTrend opera sobre esse indicador. Os valores anterior/atual formam abertura/fechamento sintéticos e seu mínimo/máximo define os extremos. O true range é a variação absoluta; ATR começa com a média de dez amplitudes e depois usa Wilder. As bandas partem do ponto médio com distância `7 * ATR`, mantendo limites anteriores até o rompimento. Ambas as bandas e ATR precisam estar formados.

Tendência de alta visa `+Volume`; baixa visa `-Volume`. A ordem pela diferença reverte a posição inteira. `TradeDirection = Buy/Sell` limita entradas, mas o sinal contrário continua fechando a posição existente; `null` permite ambos. Uma ordem pendente impede nova submissão.

`TpSlCondition` seleciona `None`, `TP`, `SL` ou `Both` (Python 0/1/2/3), desativado por padrão. Distâncias ativadas são percentuais definidos por `TakeProfitPerc` e `StopLossPerc`.

A proteção local nativa recebe velas e atualizações de preço assinadas do instrumento. As distâncias partem das execuções reais de entrada. As saídas são a mercado; o preço depende do mercado disponível e do passo de preço.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `ShortBbLength` | `20` |
| `LongBbLength` | `50` |
| `StdDev` | `2` |
| `SupertrendLength` | `10` |
| `SupertrendMultiplier` | `7` |
| `TradeDirection` | `null` |
| `TpSlCondition` | `None` |
| `TakeProfitPerc` | `30` |
| `StopLossPerc` | `20` |
| `CandleType` | `5m` |
