# Diagrama da estratégia de alinhamento de timeframes do MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um MACD é uma opinião; dois deles concordando em timeframes diferentes já são um sinal. O diagrama mede a que distância o MACD está da sua própria linha de sinal em candles de meia hora e em candles de quatro horas, e abre posição apenas quando as duas leituras apontam para o mesmo lado e o livro de ofertas está estreito o bastante para operar.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de candles trabalham sobre o mesmo instrumento em dois timeframes: meia hora para operar e quatro horas para confirmar.
- Cada série alimenta o seu próprio MACD, e dois conversores extraem a linha do MACD e a linha de sinal de cada indicador.
- Uma fórmula subtrai a linha de sinal da linha do MACD, de modo que cada timeframe se resume a um único número: positivo significa que o lado rápido está à frente, negativo que está atrasado.
- O Market depth é reduzido ao seu melhor nível e dois conversores leem a melhor oferta de venda e a melhor oferta de compra; uma terceira fórmula transforma-as no spread.
- O Sync retém os três números e os libera juntos no compasso de quatro horas. É isso que torna a comparação honesta: o livro se atualiza centenas de vezes por barra e, sem ele, as leituras nunca pertenceriam ao mesmo momento.
- Depois do Sync, as duas diferenças são comparadas com zero e o spread com o seu limite, e uma condição lógica reúne as três respostas junto com a posição zerada.
- As duas entradas são ordens a mercado de volume fixo, executadas apenas a partir de posição zerada.
- O Position protection conduz a operação: acompanha as execuções de entrada, lê o livro para obter o preço atual e fecha no take-profit ou no stop-loss expressos em percentual.

## Regras de entrada e saída

- **Entrada comprada**: No compasso de quatro horas as duas diferenças são positivas — o MACD está acima da sua linha de sinal tanto no timeframe de operação quanto no de confirmação —, o spread está dentro do limite e a posição está zerada. O Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: No mesmo compasso as duas diferenças são negativas, sob as mesmas condições de spread e de posição zerada. O Position modify vende o volume da ordem a mercado.
- **Saída**: Não há sinal de saída no diagrama: uma vez aberta a posição, o Position protection assume o controle e a fecha com 1.5% de lucro ou 1% de prejuízo em relação ao preço de entrada. Leituras opostas são ignoradas enquanto a posição existe, de modo que uma operação nunca é revertida no meio do caminho.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Trading Candles | 00:30:00 | Timeframe em que trabalha o MACD de operação. |
| Confirming Candles | 04:00:00 | Timeframe do MACD de confirmação e compasso em que o Sync libera tudo. |
| Maximum Spread | 50 | Maior spread, em unidades de preço, que ainda permite uma entrada. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |
| Take Profit, % | 1.5 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 1 | Distância do stop-loss, em percentual do preço de entrada. |

## Detalhes do diagrama

- Os dois blocos MACD estão configurados para emitir apenas valores formados e finais, de modo que um candle inacabado não consiga alterar a decisão.
- O MACD de confirmação é deliberadamente mais curto que o de operação: candles de quatro horas são escassos em um mês de histórico, e o padrão 12/26/9 gastaria a maior parte dele apenas se formando.
- O Sync dá nome a cada linha que retém, e as duas pontas de cada linha estão conectadas — um valor que entra e nunca sai deixaria o bloco esperando e a estratégia não iniciaria.
- O spread é comparado depois do Sync, e não no ponto em que chega, e é só por isso que um filtro guiado pelo livro pode conviver com uma condição guiada por candles.
- O Position protection é alimentado pelo livro de ofertas e não por um preço de candle, então precifica a saída pelo melhor nível corrente.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
