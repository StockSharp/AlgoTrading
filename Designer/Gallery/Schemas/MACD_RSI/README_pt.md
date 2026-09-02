# Diagrama da estratégia MACD + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O MACD dá a direção e o RSI dá o momento. Enquanto a linha MACD está acima da linha de sinal, o diagrama espera o índice de força relativa cair na zona de sobrevenda e compra esse recuo; a regra espelhada vende um RSI sobrecomprado enquanto o MACD está abaixo do sinal. A posição é devolvida assim que as duas linhas do MACD trocam de lado.

![schema](schema.svg)

## Visão geral da estratégia

- O teste de tendência é uma comparação de nível e não um cruzamento: importa de que lado da linha de sinal a linha MACD está agora, de modo que o filtro permanece ativo enquanto a tendência durar.
- A entrada dentro dessa tendência é propositalmente contrária: o RSI precisa estar esticado contra ela, então o diagrama compra recuos em vez de correr atrás de rompimentos.
- A saída usa o mesmo par de linhas: a compra é encerrada quando o MACD cai abaixo do sinal e a venda quando ele sobe acima.
- Não há stop nem alvo no diagrama, exatamente como na estratégia original, em que a virada do MACD é a única saída.

## Regras de entrada e saída

- **Entrada comprada**: A linha MACD está acima do sinal, o RSI abaixo do nível de sobrevenda e não há posição. A ordem compra um lote a mercado.
- **Entrada vendida**: A linha MACD está abaixo do sinal, o RSI acima do nível de sobrecompra e não há posição. A ordem vende um lote a mercado.
- **Saída**: A compra é encerrada no primeiro candle em que o MACD cai abaixo do sinal e a venda no primeiro em que ele sobe acima; os dois blocos de encerramento leem o volume da posição aberta.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MACD Fast Length | 12 | Período da EMA rápida dentro do MACD. |
| MACD Slow Length | 26 | Período da EMA lenta dentro do MACD. |
| MACD Signal Length | 9 | Período da EMA que suaviza o MACD até a linha de sinal. |
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| RSI Oversold | 30 | Nível abaixo do qual o RSI é considerado sobrevendido e a compra é liberada. |
| RSI Overbought | 70 | Nível acima do qual o RSI é considerado sobrecomprado e a venda é liberada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um bloco de indicador contém o MACD com sua linha de sinal; dois conversores retiram dele os valores Macd e Signal, e outro bloco de indicador calcula o índice de força relativa nos mesmos candles.
- Duas comparações colocam a linha MACD diante da linha de sinal, outras duas colocam o RSI diante das constantes de limiar e uma compara a posição com zero.
- Cada E lógico une a condição de tendência, a de RSI e a checagem de posição zerada, e então aciona um bloco de modificação que só abre a partir do zero.
- As comparações de tendência são reaproveitadas como gatilhos de saída, então os dois blocos de encerramento dispensam lógica extra. A pausa de 150 barras entre operações do original não tem equivalente entre os blocos e foi omitida, o que torna as reentradas mais frequentes do que no código.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
