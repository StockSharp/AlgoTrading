# Big Mover Catcher

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Usa fechamentos concluídos de um minuto e bandas de Bollinger de 40 barras, com dois desvios populacionais. Sem posição, compra `Volume` acima da banda superior ou vende `Volume` abaixo da inferior. `TradeDirection = null` permite ambos os lados; `Buy` ou `Sell` limita entradas.

Os cinco filtros vêm ativos e devem confirmar: RSI(14) estritamente acima de 50 para compra/abaixo de 50 para venda; ADX(28) pelo menos 20; `100 * ATR(14) / Close >= 0.05`; fechamento acima/abaixo da EMA(350); histograma MACD(12,26,9) positivo/negativo. Cada filtro é desligado separadamente. Apenas indicadores ativos precisam estar formados; EMA desligada não exige 350 barras.

Fecha toda a compra quando o fechamento completo fica na/abaixo da banda média, ou toda a venda na/acima dela. `IsForceTakeProfitEnabled` acrescenta saída quando o corpo favorável dividido pela abertura alcança `ForceTakeProfitPercent` por cento, padrão 5. Mede o corpo atual, não lucro desde a entrada, e vem desativado. Saídas têm prioridade, sem reversão na mesma vela.

O stop nativo local a mercado usa `StopLossPercent` por cento desde a entrada executada, padrão 2. Uma ordem ativa evita duplicações.

A proteção local nativa recebe velas e atualizações de preço assinadas do instrumento. As distâncias partem das execuções reais de entrada. As saídas são a mercado; o preço depende do mercado disponível e do passo de preço.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `BollingerPeriod` | `40` |
| `BollingerDeviation` | `2` |
| `IsRsiFilterEnabled` | `true` |
| `RsiPeriod` | `14` |
| `RsiLongThreshold` | `50` |
| `RsiShortThreshold` | `50` |
| `IsAdxFilterEnabled` | `true` |
| `AdxPeriod` | `28` |
| `MinimumAdx` | `20` |
| `IsAtrFilterEnabled` | `true` |
| `AtrPeriod` | `14` |
| `MinimumAtrPercent` | `0.05` |
| `IsEmaFilterEnabled` | `true` |
| `EmaPeriod` | `350` |
| `IsMacdFilterEnabled` | `true` |
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `TradeDirection` | `null` |
| `StopLossPercent` | `2` |
| `IsForceTakeProfitEnabled` | `false` |
| `ForceTakeProfitPercent` | `5` |
| `CandleType` | `1m` |
