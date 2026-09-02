# Diagrama da estratégia de rompimento de faixa por ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Aqui tudo é decidido por um único número: o quanto o fechamento andou nos últimos candles, medido em Average True Range. Um movimento de pelo menos um ATR é tratado como um rompimento em que vale a pena entrar, e o lado é simplesmente o lado para onde o preço andou. A média móvel simples não participa da entrada - ela é a saída, e a posição é abandonada assim que o fechamento volta a atravessá-la.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de valor anterior guarda o fechamento de quatro candles atrás, e um bloco de fórmula o subtrai do fechamento atual e toma o módulo: essa é a distância percorrida.
- O Average True Range é a régua. Quando a distância o alcança, o mercado andou nesses quatro candles mais do que costuma andar em um, e o diagrama chama isso de rompimento.
- A direção não precisa de indicador: fechamento acima do fechamento anterior significa compra, abaixo, venda.
- A média móvel tem uma única tarefa, encerrar a posição: a compra termina no primeiro fechamento abaixo dela e a venda no primeiro fechamento acima.

## Regras de entrada e saída

- **Entrada comprada**: A distância percorrida nos últimos quatro candles é de ao menos um ATR, o fechamento está acima do fechamento de quatro candles atrás e a posição está zerada. A ordem compra a mercado o volume compartilhado.
- **Entrada vendida**: A distância percorrida nos últimos quatro candles é de ao menos um ATR, o fechamento está abaixo do fechamento de quatro candles atrás e a posição está zerada. A ordem vende a mercado o volume compartilhado.
- **Saída**: A compra é encerrada no primeiro candle que fecha abaixo da média móvel simples e a venda no primeiro que fecha acima. Os dois blocos de saída trazem a condição de encerramento, de modo que cada um só age no seu lado. Não há stop loss nem realização de lucro, como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| ATR Period | 14 | Período de suavização do Average True Range, que define a largura mínima de um rompimento. |
| MA Period | 20 | Período da média móvel simples que encerra a posição. |
| Lookback shift | 4 | Quantos candles atrás o preço é comparado; o original mede sobre a janela de observação menos um, ou seja, quatro candles por padrão. |
| Volume | 1 | Volume da ordem, em lotes, compartilhado pelos dois blocos de entrada. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o ATR, a média móvel e um conversor que lê o preço de fechamento; o bloco de valor anterior parte desse mesmo conversor.
- O bloco de fórmula calcula o módulo da diferença entre os dois fechamentos, e uma comparação o confronta com o ATR para decidir se o movimento foi largo o bastante.
- Outras duas comparações do mesmo par de fechamentos dão a direção, e uma comparação da posição com uma constante zero impede que as entradas se acumulem.
- Cada E lógico junta amplitude, direção e posição zerada e aciona um bloco de abertura; as duas comparações com a média móvel acionam diretamente os blocos de encerramento, pois a direção de um bloco de encerramento já decide qual lado ele pode fechar.
- O original em C# mede apenas a cada quinto candle, em janelas que não se sobrepõem, e congela o preço de referência no candle intermediário. Esse contador modular não tem bloco equivalente, então o diagrama usa uma janela deslizante e verifica a cada candle, o que gera mais sinais que o original.
- A pausa de quinhentos candles que o original mantém após cada operação foi removida pelo mesmo motivo, e o diagrama roda nos candles de cinco minutos do histórico que acompanha a galeria, e não no minuto do código C#.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
