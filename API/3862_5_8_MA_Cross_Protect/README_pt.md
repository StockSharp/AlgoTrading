# 5/8 EMA Estratégia de proteção cruzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **5/8 EMA estratégia de proteção cruzada** replica o MetaTrader consultor especialista `5_8macrossv2.mq4` comparando duas médias móveis configuráveis no mesmo símbolo. Um cruzamento de alta da média móvel rápida acima da lenta abre posições longas, enquanto um cruzamento de baixa abre posições curtas. A versão portada segue StockSharp padrões de alto nível e adiciona gerenciamento opcional de take-profit, stop-loss e trailing-stop.

## Lógica de negociação
- Duas médias móveis são calculadas na assinatura da vela selecionada. Por padrão, uma MM exponencial de 5 períodos sobre preços de fechamento é comparada a uma MM exponencial de 8 períodos sobre preços de abertura.
- Quando a MM rápida cruza acima da MM lenta na última vela finalizada, a estratégia abre ou reverte para uma posição longa. Se uma posição curta estiver ativa, seu volume será incluído na nova ordem de compra de mercado para mudar de direção.
- Quando a MM rápida cruza abaixo da MM lenta, a estratégia abre ou reverte para uma posição curta usando a mesma lógica de normalização de volume.
- Os parâmetros de deslocamento MA emulam o deslocamento horizontal original. Valores positivos atrasam o sinal por esse número de velas fechadas; valores negativos são arredondados para zero porque os valores deslocados para frente não estão disponíveis em dados em tempo real.

## Gestão de risco
- As distâncias de **Take-profit** e **stop-loss** são expressas em pips (etapas de preço). Quando uma posição longa é aberta, os níveis de proteção são colocados acima e abaixo do preço de entrada, respectivamente; os espelhos lógicos para shorts.
- **Trailing stop** (também em pips) estreita constantemente o nível de proteção à medida que o preço se move a favor da posição. Para posições compradas, o trailing stop apenas se move para cima; para shorts, ele apenas se move para baixo.
- Se qualquer condição de proteção for atendida em uma vela finalizada (take-profit de altos acertos, stop-loss de acertos baixos ou nível de trailing), a estratégia sai da posição com uma ordem de mercado e redefine seu estado interno.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Volume de pedidos para novas entradas. A estratégia adiciona o tamanho absoluto da posição ao reverter. |
| `TakeProfitPips` | `decimal` | `40` | Distância da entrada em pips para fechamento da posição com lucro. Defina como `0` para desativar. |
| `StopLossPips` | `decimal` | `0` | Distância da entrada em pips para stop loss de proteção. Defina como `0` para desativar. |
| `TrailingStopPips` | `decimal` | `0` | Distância do trailing-stop em pips. Defina como `0` para desativar. |
| `FastPeriod` | `int` | `5` | Período da média móvel rápida. |
| `FastShift` | `int` | `-1` | Mudança horizontal para o MA rápido. Valores negativos são tratados como zero nesta porta. |
| `FastMethod` | `MovingAverageMethod` | `Exponential` | Algoritmo de suavização para MA rápido (Simple, Exponencial, Smoothed, LinearWeighted). |
| `FastPrice` | `AppliedPrice` | `Close` | Preço da vela usado para o MA rápido. |
| `SlowPeriod` | `int` | `8` | Período da média móvel lenta. |
| `SlowShift` | `int` | `0` | Deslocamento horizontal para o MA lento. |
| `SlowMethod` | `MovingAverageMethod` | `Exponential` | Algoritmo de suavização para MA lento. |
| `SlowPrice` | `AppliedPrice` | `Open` | Preço da vela usado para o MA lento. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(30).TimeFrame()` | Série de velas usadas para cálculos. |

## Notas
- A conversão mantém a lógica focada nas velas finalizadas para evitar sinais prematuros.
- Trailing stops e metas de lucro são calculadas com `Security.PriceStep`; se um símbolo não o definir, os parâmetros de risco permanecem inativos.
- A versão Python é omitida intencionalmente de acordo com os requisitos da tarefa.
