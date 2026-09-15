# Diagrama da estratégia modelo de entrada filtrada por spread
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O sinal aqui é propositalmente simples: um candle fecha acima da própria abertura enquanto o preço cruza para cima de uma média móvel lenta. O que o diagrama realmente mostra é tudo aquilo que fica entre esse sinal e a ordem — um spread lido no livro de ofertas, um intervalo de espera que mantém as entradas afastadas umas das outras e um volume de ordem calculado a partir do capital, em vez de um número fixo.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de quatro horas já fechados alimentam uma média móvel simples de 50 períodos e dois conversores que extraem a abertura e o fechamento de cada candle.
- A cor do candle são duas comparações: fechamento acima da abertura é alta, fechamento abaixo da abertura é baixa.
- Um bloco Previous value guarda o candle de um passo atrás e um segundo guarda a média, de modo que o cruzamento é lido como um par de comparações comuns, e não como um indicador à parte.
- O Market depth fornece a melhor oferta de compra e a melhor oferta de venda, e uma fórmula subtrai uma da outra. O resultado fica retido em uma variável que o candle libera, então a condição de entrada compara um spread que pertence ao mesmo instante que o restante dela.
- O Strategy P&L alimenta uma variável que carrega o resultado realizado, uma fórmula soma esse valor ao capital inicial e uma segunda fórmula transforma esse capital em volume de ordem: capital vezes a fração de risco, dividido pelo preço de fechamento, arredondado para três casas decimais.
- Um contador montado com uma variável e a fórmula min(a + 1, n) mede os candles desde a última execução e bloqueia uma nova entrada até que oito deles tenham passado.
- As duas entradas são feitas apenas a partir de posição zerada, de modo que o diagrama mantém uma posição por vez e nunca aumenta essa posição.
- A saída é a cor oposta do candle, e o Position protection acrescenta a ela um take-profit de 0.7% e um stop-loss de 0.5%.

## Regras de entrada e saída

- **Entrada comprada**: Um candle de alta cujo fechamento anterior ficou igual ou abaixo da média anterior e cujo fechamento está acima da média atual, com o spread dentro do limite, posição zerada e o intervalo de espera cumprido. O Position modify compra a mercado com o volume calculado.
- **Entrada vendida**: Um candle de baixa cujo fechamento anterior ficou igual ou acima da média anterior e cujo fechamento está abaixo da média atual, sob as mesmas condições de spread, posição zerada e intervalo de espera. O Position modify vende a mercado com o mesmo volume calculado.
- **Saída**: Um candle de baixa fecha uma posição comprada e um candle de alta fecha uma posição vendida: as duas condições se encontram em um Combination que aciona um único Position modify configurado para fechar a posição, de modo que nenhuma das direções precisa do seu próprio bloco de saída. O Position protection acompanha as execuções de entrada de forma independente e pode encerrar a operação antes, com 0.7% de lucro ou 0.5% de prejuízo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 04:00:00 | Tempo gráfico dos candles sobre os quais todo o diagrama trabalha. |
| SMA Length | 50 | Período da média móvel simples com a qual o fechamento é comparado. |
| Spread Limit | 50 | Maior spread do livro, em unidades de preço, que ainda permite uma entrada. Aumente esse valor em instrumentos cotados com spread largo. |
| Start Capital | 1000000 | Tamanho da conta do qual parte o cálculo do capital; ajuste-o para o tamanho real da conta antes de operar. |
| Risk Fraction | 0.3 | Parcela do capital comprometida em uma posição, expressa como fração: 0.3 são trinta por cento. |
| Cooldown Bars | 8 | Quantos candles fechados devem passar depois de uma execução até que a próxima entrada seja permitida. |
| Take Profit, % | 0.7 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 0.5 | Distância do stop-loss, em percentual do preço de entrada. |

## Detalhes do diagrama

- O bloco de candles alimenta oito consumidores: a média, os dois conversores, o bloco do candle anterior, a variável do spread, a variável do resultado realizado, o contador do intervalo de espera e o painel do gráfico.
- O livro de ofertas se atualiza com muito mais frequência do que os candles, por isso o seu spread não é comparado diretamente. Uma variável guarda o valor mais recente e o libera quando o candle fecha, o que coloca todos os termos da condição de entrada no mesmo relógio.
- A variável do resultado realizado começa em zero e só recebe um valor depois que a primeira operação é encerrada, o que mantém a fórmula de volume abastecida já a partir do primeiro candle.
- Os dois blocos de entrada compartilham a mesma fórmula de volume, então compra e venda são dimensionadas pela mesma regra.
- O contador do intervalo de espera é zerado pelo bloco de execuções da estratégia, ou seja, qualquer execução — uma entrada ou uma saída de proteção — reinicia a espera de oito candles.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
