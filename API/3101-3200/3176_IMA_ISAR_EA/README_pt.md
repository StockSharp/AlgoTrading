# Estratégia de iMA iSAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o Expert Advisor "iMA iSAR EA" do MetaTrader 5 usando a API de alto nível do StockSharp. Combina um filtro de tripla média móvel ponderada com dois trilhos de SAR Parabólico para identificar rompimentos de momentum. Uma posição comprada é aberta quando a média móvel ponderada mais rápida permanece acima das outras duas médias e ambos os trilhos SAR ficam abaixo do fechamento da vela. Uma condição espelhada gera entradas vendidas. Stops protetores, metas de lucro e um trailing stop opcional são gerenciados em pontos de preço (pips).

A implementação trabalha em uma única série de velas configurável através do parâmetro `CandleType`. Todos os indicadores são avaliados neste período. O expert do MetaTrader original usava múltiplos períodos para seus indicadores; no StockSharp esse comportamento é aproximado permitindo deslocamentos individuais das médias móveis que podem atrasar cada sinal por um número de barras concluídas.

## Regras de trading
- **Indicadores**
  - Três médias móveis ponderadas (`Fast`, `Normal`, `Slow`) calculadas no stream de velas configurado. Deslocamentos de barras opcionais emulam os buffers atrasados do código MQ5 original.
  - Dois indicadores SAR Parabólico (`FastSAR`, `NormalSAR`) compartilham o mesmo stream de velas mas têm parâmetros de aceleração e máximo independentes.
- **Condições de entrada**
  - **Comprado**: a MA `Fast` está acima de `Normal` e `Slow`, enquanto ambos os valores SAR estão abaixo do fechamento da vela.
  - **Vendido**: a MA `Fast` está abaixo de `Normal` e `Slow`, enquanto ambos os valores SAR estão acima do fechamento da vela.
  - Quando um sinal de reversão aparece, a estratégia fecha qualquer exposição oposta e muda de direção em uma única ordem a mercado.
- **Controles de risco**
  - Níveis fixos de stop-loss e take-profit são expressos em pips (múltiplos do passo de preço do instrumento). São avaliados em velas concluídas.
  - Trailing stop opcional: uma vez habilitado, o stop segue o preço de fechamento a uma distância configurável e avança apenas após mover-se pelo passo de trailing especificado.
  - Os volumes são ajustados às configurações `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento antes de enviar as ordens.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|--------|-----------|
| `Volume` | `decimal` | `0.1` | Tamanho base da ordem. Automaticamente aumentado para cobrir uma posição oposta ao mudar de direção. |
| `StopLossPips` | `decimal` | `50` | Distância do stop protetor em pips. Definir como `0` para desabilitar. |
| `TakeProfitPips` | `decimal` | `50` | Distância da meta de lucro em pips. Definir como `0` para desabilitar. |
| `UseTrailing` | `bool` | `true` | Habilita o gerenciamento dinâmico de trailing stop. |
| `TrailingStopPips` | `decimal` | `25` | Distância entre o preço e o trailing stop, em pips. |
| `TrailingStepPips` | `decimal` | `5` | Movimento favorável mínimo (pips) antes do trailing stop avançar. |
| `CandleType` | `DataType` | `TimeFrameCandle 15m` | Série de velas usada para todos os cálculos. |
| `FastMaPeriod` | `int` | `10` | Período da média móvel ponderada rápida. |
| `FastMaShift` | `int` | `0` | Número de barras concluídas para deslocar a MA rápida para trás. |
| `NormalMaPeriod` | `int` | `30` | Período da média móvel ponderada normal. |
| `NormalMaShift` | `int` | `3` | Número de barras concluídas para deslocar a MA normal para trás. |
| `SlowMaPeriod` | `int` | `60` | Período da média móvel ponderada lenta. |
| `SlowMaShift` | `int` | `6` | Número de barras concluídas para deslocar a MA lenta para trás. |
| `FastSarStep` | `decimal` | `0.02` | Fator de aceleração para o SAR Parabólico rápido. |
| `FastSarMax` | `decimal` | `0.2` | Aceleração máxima para o SAR Parabólico rápido. |
| `NormalSarStep` | `decimal` | `0.02` | Fator de aceleração para o SAR Parabólico normal. |
| `NormalSarMax` | `decimal` | `0.2` | Aceleração máxima para o SAR Parabólico normal. |

## Notas
- As verificações de trailing stop são realizadas no fechamento da vela. Se precisão intrabarra for necessária, combine a estratégia com um componente protetor no nível de tick.
- O tamanho do pip equivale ao passo de preço do instrumento quando disponível. Caso contrário, um tick padrão de `0.0001` é assumido para pares FX.
- Para consistência com a versão do MetaTrader, todos os sinais de indicadores operam em velas fechadas. Transações pendentes não são preparadas; cada sinal envia uma ordem a mercado imediata.
