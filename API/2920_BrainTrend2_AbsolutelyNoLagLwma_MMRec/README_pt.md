# Estratégia BrainTrend2 + AbsolutelyNoLagLWMA MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria o especialista MetaTrader `Exp_BrainTrend2_AbsolutelyNoLagLwma_MMRec` combinando dois blocos de sinal independentes: o motor de seguimento de tendência BrainTrend2 e o filtro adaptativo AbsolutelyNoLagLWMA. Cada bloco pode abrir e fechar negociações de acordo com suas próprias permissões, imitando os interruptores de gestão de dinheiro do template MMRec original. As ordens são executadas com a API de alto nível do StockSharp usando execuções de mercado e o volume padrão configurável.

## Lógica de negociação
### Bloco BrainTrend2
* Constrói um nível de trailing dinâmico baseado em um intervalo verdadeiro ponderado semelhante ao ATR.
* A direção (`river`) alterna quando a vela perfura o buffer de trailing em mais de `0.7 * ATR`.
* Velas de alta dentro de um river de alta acionam entradas compradas (se habilitadas) e fecham posições vendidas.
* Velas de baixa dentro de um river de baixa acionam entradas vendidas (se habilitadas) e fecham posições compradas.
* Os sinais podem ser atrasados pelo parâmetro `Brain Signal Shift` para trabalhar com barras mais antigas.

### Bloco AbsolutelyNoLagLWMA
* Aplica uma média móvel linear ponderada em dois estágios à fonte de preço selecionada.
* As cores tornam-se **alta (2)** quando a LWMA dupla sobe, **baixa (0)** quando cai e **neutra (1)** caso contrário.
* Uma transição para a cor 2 abre compradas e opcionalmente fecha vendidas; uma mudança para a cor 0 abre vendidas e opcionalmente fecha compradas.
* Os sinais também podem ser deslocados para trás por um número definido pelo usuário de barras.

### Gestão de posição
* A estratégia opera uma única posição líquida. Quando ambos os blocos solicitam negociações na mesma barra, os sinais de fechamento são executados antes de quaisquer novas entradas.
* Se um bloco quer abrir uma negociação mas a posição oposta está aberta e a permissão de fechamento correspondente está desativada, a entrada é ignorada (reflete a impossibilidade de manter posições hedgeadas com um único portfólio líquido).

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| BrainTrend2 | Brain Candle | Tipo de vela usado para o indicador BrainTrend2. |
| BrainTrend2 | Brain ATR | Período ATR para os cálculos internos do BrainTrend2. |
| BrainTrend2 | Brain Signal Shift | Número de barras para atrasar os sinais do BrainTrend2. |
| BrainTrend2 | Brain Buy / Sell | Permitir que o BrainTrend2 abra negociações compradas/vendidas. |
| BrainTrend2 | Brain Close Buys / Close Sells | Permitir que os sinais do BrainTrend2 fechem posições existentes. |
| AbsolutelyNoLag | Abs Candle | Tipo de vela usado para o indicador LWMA. |
| AbsolutelyNoLag | Abs Length | Período da LWMA. |
| AbsolutelyNoLag | Abs Price | Preço aplicado usado para a LWMA. Corresponde ao enum `Applied_price_` do MQL. |
| AbsolutelyNoLag | Abs Signal Shift | Número de barras para atrasar os sinais da LWMA. |
| AbsolutelyNoLag | Abs Buy / Sell | Permitir que o bloco LWMA abra negociações compradas/vendidas. |
| AbsolutelyNoLag | Abs Close Buys / Close Sells | Permitir que o bloco LWMA feche posições. |
| AbsolutelyNoLag | Abs Shift | Adiciona um deslocamento de preço constante à saída da LWMA. |
| General | Order Volume | Volume de ordem de mercado padrão. |

## Notas
* Os cálculos de ATR e LWMA seguem as implementações MQL originais, incluindo a ponderação triangular do ATR e a extensa lista de preços aplicados.
* As informações de spread não estão disponíveis nas velas do StockSharp, portanto o intervalo verdadeiro usa apenas preços de vela. Isso reflete o comportamento do indicador quando o spread é igual a zero.
* Múltiplas posições simultâneas com diferentes magic numbers são consolidadas em uma única posição líquida, o que é padrão nas estratégias do StockSharp.
