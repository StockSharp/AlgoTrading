# Diagrama da estratégia combinada EMA + MACD + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Três verificações independentes precisam concordar antes de este diagrama operar. A posição relativa da EMA 50 e da EMA 200 diz qual lado está liberado, o cruzamento da linha MACD com o sinal diz quando, e o RSI precisa estar numa faixa intermediária: já existe impulso, mas o movimento ainda não se esgotou. Cada sinal aceito inverte a posição com uma única ordem a mercado.

![schema](schema.svg)

## Visão geral da estratégia

- O filtro de tendência é uma comparação de níveis entre duas médias exponenciais: não se compra enquanto a EMA 50 estiver abaixo da EMA 200 nem se vende enquanto estiver acima.
- A entrada é um evento e não um estado: apenas o candle em que a linha MACD cruza o sinal pode abrir uma operação, portanto o diagrama não dispara continuamente enquanto a tendência dura.
- O corredor do RSI é o que dá prudência à combinação. Uma compra exige RSI acima do nível de compra e ainda abaixo do limite superior; uma venda exige RSI abaixo do nível de venda e ainda acima do limite inferior.
- O original trabalha em candles de trinta minutos; o diagrama foi reduzido para candles de cinco minutos, de acordo com o histórico de amostra incluído. A pausa de dez barras após cada operação não tem equivalente em blocos e foi omitida, o que torna as reentradas mais frequentes do que no código.

## Regras de entrada e saída

- **Entrada comprada**: A EMA 50 está acima da EMA 200, a linha MACD cruza o sinal para cima, o RSI está acima do nível de compra e ainda abaixo do limite superior, e a posição ainda não está comprada. A ordem compra o volume base mais a venda em aberto, invertendo a venda para compra com uma única ordem a mercado.
- **Entrada vendida**: A EMA 50 está abaixo da EMA 200, a linha MACD cruza o sinal para baixo, o RSI está abaixo do nível de venda e ainda acima do limite inferior, e a posição ainda não está vendida. A ordem vende o volume base mais a compra em aberto, invertendo a compra para venda com uma única ordem.
- **Saída**: Não há bloco de saída nem proteção, exatamente como no original: a posição é mantida até surgir o sinal espelhado, e essa mesma ordem encerra a operação antiga e abre a nova.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA length | 50 | Período da média exponencial rápida que carrega a tendência curta. |
| Slow EMA length | 200 | Período da média exponencial lenta contra a qual a rápida é medida. |
| MACD fast length | 12 | Período da EMA rápida dentro do MACD. |
| MACD slow length | 26 | Período da EMA lenta dentro do MACD. |
| MACD signal length | 9 | Período da EMA que suaviza o MACD até a linha de sinal. |
| RSI length | 14 | Período de suavização do índice de força relativa. |
| RSI buy level | 40 | O RSI precisa estar acima deste nível para aceitar uma compra. |
| RSI sell level | 60 | O RSI precisa estar abaixo deste nível para aceitar uma venda. |
| RSI upper bound | 70 | Limite superior do corredor do RSI; acima dele a compra é considerada tardia. |
| RSI lower bound | 30 | Limite inferior do corredor do RSI; abaixo dele a venda é considerada tardia. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um bloco de candles alimenta quatro blocos de indicador: as duas médias exponenciais, o MACD com sua linha de sinal e o índice de força relativa.
- Dois conversores separam o valor do MACD nas linhas Macd e Signal; um bloco de cruzamento transforma esse par no gatilho de alta e um bloco NÃO o inverte no de baixa.
- Oito blocos de comparação formam os filtros: um par para as médias, quatro para o corredor do RSI e dois para a posição diante do zero.
- Cada E lógico une cinco condições antes de acionar um bloco de modificação de posição, e um bloco de fórmula soma o volume base ao valor absoluto da posição para que uma ordem a mercado execute toda a inversão.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
