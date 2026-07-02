# BandOsMaEstratégia Personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma porta direta do consultor especialista MetaTrader 5 localizado em
`MQL/45596/mql5/Experts/MQL5Book/p7/BandOsMACustom.mq5`. O robô original
combina o histograma MACD (também conhecido como OsMA) com bandas Bollinger e um
média móvel aplicada aos valores do histograma em vez dos preços brutos.
Sempre que o histograma ultrapassa a banda inferior, o especialista abre uma negociação longa,
enquanto toques na banda superior acionam entradas curtas. O histograma cruzando um
a média móvel separada fecha a posição. Uma parada protetora e um
passo de trailing-stop (igual a um quinquagésimo do stop) mantém o risco sob controle.

A implementação StockSharp preserva esse comportamento usando o API de alto nível,
portanto, a lógica de negociação permanece legível e depurável dentro da estrutura.

## Destaques de conversão

* O histograma MACD é implementado por meio
`MovingAverageConvergenceDivergenceHistogram`, alimentado com o preço da vela que
corresponde ao modo MetaTrader `PRICE_*` selecionado pelo `AppliedPrice`
parâmetro.
* Bollinger As bandas e a média móvel de saída processam a saída OsMA em vez
do que dados de preços. Um buffer de histórico compacto reproduz o MetaTrader `shift`
argumentos para ambos os indicadores.
* A estratégia mantém a sinalização longa/curta original: cruzamentos abaixo do
banda inferior iniciam posições compradas, cruzamentos acima da faixa superior iniciam posições vendidas e
OsMA cruzando sua média móvel fecha a negociação.
* `StartProtection` espelha o bloco MetaTrader de stop-loss mais trailing-stop.
A etapa final é calculada como `StopLossPoints / 50`, assim como MQL
classe `TrailingStop` fez.

## Indicadores

| Indicador | Objetivo |
| --- | --- |
| `MovingAverageConvergenceDivergenceHistogram` | Recria a saída `iOsMA` de `iOsMA`. |
| `BollingerBands` | Calcula os limites superior e inferior no histograma. |
| Média móvel (SMA/EMA/SMMA/LWMA) | Os filtros saem quando o histograma o cruza. |

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Período de 1 hora | Prazo principal usado para todos os cálculos dos indicadores. |
| `FastOsmaPeriod` | 12 | Comprimento EMA rápido do cálculo OsMA. |
| `SlowOsmaPeriod` | 26 | Comprimento EMA lento do cálculo OsMA. |
| `SignalPeriod` | 9 | Comprimento do sinal SMA do cálculo OsMA. |
| `AppliedPrice` | Típico | Preço aplicado no estilo MetaTrader que alimenta o histograma. |
| `BandsPeriod` | 26 | Comprimento das bandas Bollinger desenhadas nos valores do histograma. |
| `BandsShift` | 0 | Deslocamento para a direita (em barras) aplicado aos valores Bollinger. |
| `BandsDeviation` | 2,0 | Multiplicador de desvio padrão para as bandas. |
| `MaPeriod` | 10 | Comprimento da média móvel de saída calculada no histograma. |
| `MaShift` | 0 | Deslocamento para a direita (em barras) aplicado à média móvel de saída. |
| `MaMethod` | Simples | Método de média móvel (SMA, EMA, SMMA, LWMA). |
| `StopLossPoints` | 1000 | Distância de parada protetora expressa em etapas de preço. |
| `OrderVolume` | 0,01 | Volume de negociação, idêntico à entrada MetaTrader “Lotes”. |

## Regras de negociação

1. Assine a série de velas selecionada e alimente o preço aplicado escolhido
no histograma MACD.
2. Passe cada valor do histograma para as bandas Bollinger e a média móvel de saída.
3. Detecte sinais usando os buffers deslocados:
   * Se o histograma cair na banda inferior, defina um sinal de alta.
   * Se o histograma ultrapassar a banda superior, defina um sinal de baixa.
   * Quando o histograma cruzar a média móvel de saída, limpe o ativo
sinal, que permite que a posição seja fechada.
4. Gerenciar posições:
   * Feche as posições compradas existentes sempre que o sinal de alta desaparecer; shorts próximos
quando o sinal de baixa desaparecer.
   * Abra uma posição comprada quando o sinal de alta estiver ativo e não houver abertura
posição; abrir uma posição curta quando o sinal de baixa estiver ativo e a posição for
plano.
5. Aplique `StartProtection` com a distância de stop-loss configurada e um trailing
passo igual a `StopLossPoints / 50` passos de preço.

## Notas

* Todos os comentários no código-fonte estão em inglês para estar em conformidade com o repositório
diretrizes.
* Os buffers de histórico garantem que a versão StockSharp respeite MetaTrader
Parâmetros `BandsShift` e `MaShift` sem solicitar valores de indicador por
índice.
* A estratégia está alinhada com as convenções de alto nível API: `SubscribeCandles`
impulsiona atualizações de indicadores e direciona chamadas para imitar `BuyMarket`/`SellMarket`
a colocação do pedido do especialista original.
