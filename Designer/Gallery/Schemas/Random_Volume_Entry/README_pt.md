# Diagrama da estratégia de entrada alternada com tamanho aleatório
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Todo diagrama da galeria decide quanto negociar; este se recusa a decidir. O bloco Random sorteia um novo tamanho entre meio lote e dois a cada candle e o entrega direto na entrada de volume dos dois blocos de abertura, de modo que nenhuma posição tem o mesmo tamanho da outra. Já a direção não tem nada de aleatória: ela vem de onde está o último negócio executado em relação à abertura do candle em formação.

![schema](schema.svg)

## Visão geral da estratégia

- O fluxo de ticks é o sinal. Um conversor lê o preço do último negócio executado e uma variável o guarda até o candle fechar, e é isso que coloca o negócio e a abertura do candle no mesmo relógio.
- Uma comparação pergunta se esse preço está acima da abertura do candle. A mesma resposta, invertida por um NOT lógico, é o lado vendido, de modo que uma única comparação atende às duas direções.
- O bloco Random é disparado pelo candle e entrega seu número à entrada de volume do bloco de compra e do bloco de venda. Nada mais no diagrama o utiliza.
- As entradas só acontecem a partir de posição zerada, e os dois blocos carregam a condição de posição aberta, de modo que um sinal não consegue aumentar uma posição que já está aberta.
- Um contador de candles desde a última execução mantém doze candles entre as entradas. Sem ele, a saída protetiva e a entrada seguinte ficariam se perseguindo em candles consecutivos.
- O Position protection é a única saída. Ele recebe as execuções de entrada por um Combination, precifica-as pelo fechamento do candle e encerra num alvo de 0.4% ou num stop móvel de 0.5%.
- O stop acompanha o preço, então uma posição que anda na direção certa devolve apenas a última parte do movimento.
- O painel de gráfico desenha os candles, os dois fluxos de ordens e todas as execuções, inclusive as protetivas.

## Regras de entrada e saída

- **Entrada comprada**: O último negócio executado está acima da abertura do candle em formação, a posição está zerada e já se passaram doze candles desde a última execução. O Position modify compra a mercado, com o volume que o bloco Random sorteou para este candle.
- **Entrada vendida**: O último negócio executado está igual ou abaixo da abertura do candle em formação, sob as mesmas condições de posição zerada e de espera. O Position modify vende a mercado com o mesmo volume sorteado aleatoriamente.
- **Saída**: Não há sinal de saída. O Position protection assume a posição desde a primeira execução e a encerra num take-profit de 0.4% ou num stop móvel de 0.5% medido a partir do preço de entrada. Qualquer execução, inclusive as protetivas, reinicia a espera de doze candles antes da próxima entrada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame dos candles sobre os quais todo o diagrama trabalha. |
| Cooldown Bars | 12 | Quantos candles fechados devem passar depois de uma execução até que a próxima entrada seja permitida. |
| Min Volume | 0.5 | Menor tamanho que o bloco Random pode sortear. |
| Max Volume | 2 | Maior tamanho que o bloco Random pode sortear. |
| Take Profit, % | 0.4 | Distância do take-profit, em porcentagem do preço de entrada. |
| Stop Loss, % | 0.5 | Distância do stop móvel, em porcentagem do preço de entrada. |

## Detalhes do diagrama

- O bloco de candles alimenta oito consumidores: os dois conversores, a variável do último negócio, o bloco Random, o contador de espera e suas duas variáveis, e o painel de gráfico.
- A variável do último negócio é a única coisa entre um fluxo de ticks que dispara milhares de vezes por dia e uma condição feita para ser respondida uma vez por candle.
- Os dois blocos de entrada compartilham uma única saída do Random, de modo que compra e venda são dimensionadas pelo mesmo sorteio; o número muda no candle seguinte, não entre um bloco e outro.
- O contador de espera é zerado pelo bloco de execuções da estratégia, e é por isso que uma saída protetiva também dá início à espera, e não apenas uma entrada.
- O tamanho é sorteado com duas casas decimais, o que serve a um instrumento cotado em frações de unidade; num instrumento negociado em lotes inteiros, a faixa é definida em números inteiros.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
