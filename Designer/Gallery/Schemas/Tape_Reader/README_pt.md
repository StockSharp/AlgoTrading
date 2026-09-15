# Diagrama da estratégia Tape Reader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um candle diz o que aconteceu durante a barra; a fita diz como aconteceu. Este diagrama assina o fluxo de negócios executados, mede cada negócio contra o tamanho médio dos últimos cem negócios e trata um negócio de várias vezes essa média como a pegada de alguém com pressa. A leitura é feita uma vez por candle encerrado, de modo que um fluxo que dispara milhares de vezes por dia ainda produz uma decisão por barra.

![schema](schema.svg)

## Visão geral da estratégia

- O fluxo de ticks carrega as duas metades do sinal: um conversor lê o tamanho de cada negócio executado, outro lê o seu preço.
- Uma média móvel sobre os tamanhos dos últimos cem negócios dá ao diagrama uma noção corrente de como é um negócio comum neste instrumento, de modo que nada nele fica preso a um nível de preço ou a um tamanho de contrato específico.
- Uma fórmula divide o tamanho de cada negócio por essa média e o transforma num múltiplo simples: um é um negócio comum, cinco é um negócio cinco vezes maior que a norma recente.
- Uma variável guarda esse múltiplo, e uma segunda variável guarda o preço do negócio, até o candle fechar. O candle é o gatilho de ambas, e é isso que coloca uma medição na velocidade dos ticks e uma decisão na velocidade dos candles no mesmo relógio.
- Uma comparação contra o Size factor responde se o negócio foi grande. O fechamento do candle anterior, obtido com um bloco Previous value e um conversor, responde para que lado ele foi.
- O bloco Position é comparado com zero três vezes, o que dá um teste de posição zerada para as entradas e um teste comprado e um vendido para as saídas.
- O bloco Strategy trades reporta cada execução própria e zera um contador de candles, o que mantém o diagrama fora do mercado por um número definido de candles depois de qualquer execução, de entrada ou de saída.
- As duas entradas são blocos Position modify configurados apenas para abrir, e a saída é um terceiro configurado para fechar, de modo que apenas uma posição é mantida por vez e ela é zerada antes que o lado oposto possa ser tomado.

## Regras de entrada e saída

- **Entrada comprada**: O negócio que passou por último antes do fechamento do candle teve pelo menos Size factor vezes o tamanho médio dos negócios, o seu preço está acima do fechamento do candle anterior, a posição está zerada e o cooldown já decorreu. O Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: O mesmo negócio grande, mas com preço abaixo do fechamento do candle anterior, novamente a partir de posição zerada e com o cooldown decorrido. O Position modify vende o volume da ordem a mercado.
- **Saída**: Não há take-profit nem stop-loss. Uma posição comprada é fechada quando um negócio grande passa abaixo do fechamento anterior enquanto a posição está comprada, e uma vendida é fechada por um negócio grande acima dele. Os dois portões de saída alimentam um único Combination, que aciona o bloco Position modify configurado para fechar; a execução que zera a posição também reinicia o cooldown, de modo que a próxima entrada aguarda o mesmo número de candles.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame dos candles com que todo o diagrama trabalha; cada leitura da fita é feita quando um deles fecha. |
| Average Prints | 100 | Quantos dos negócios mais recentes formam o tamanho médio contra o qual o múltiplo é medido. Uma janela mais curta acompanha mais rápido uma mudança de atividade e deixa a própria média mais instável. |
| Size Factor | 5 | Quantas vezes o tamanho médio um negócio precisa atingir para contar como grande. Aumente para sinais mais raros e mais seletivos; reduza quando a fita estiver parada e nada se qualificar. |
| Cooldown Bars | 3 | Quantos candles encerrados devem passar depois de qualquer execução antes que a próxima entrada seja permitida. |
| Order Volume | 1 | Tamanho de cada ordem de entrada, em unidades do instrumento. A ordem de fechamento é dimensionada pela posição aberta e não lê este valor. |

## Detalhes do diagrama

- A medida é uma razão, e não um número de contratos, de modo que o mesmo Size factor é lido da mesma forma num instrumento cotado em frações de unidade e num cotado em lotes inteiros.
- A média inclui o negócio que está sendo medido contra ela, de modo que um único negócio muito grande eleva um pouco a sua própria régua; com cem negócios na janela, um negócio cinco vezes maior que a norma ainda é lido acima de quatro.
- O negócio que é lido é o último a chegar antes do fechamento do candle. A variável que o guarda é o que fica entre um fluxo que dispara milhares de vezes por dia e uma decisão que deve ser tomada uma vez por barra; sem ela, o teste de tamanho e o teste de preço responderiam em relógios diferentes e nunca concordariam.
- O contador do cooldown é zerado a partir do bloco de execuções da estratégia, e não a partir dos blocos de entrada, de modo que uma execução de fechamento também inicia a espera. Sem isso, a saída de uma operação e a entrada da seguinte cairiam em candles vizinhos.
- O painel do gráfico desenha os candles, o tamanho médio dos negócios, o múltiplo guardado e a linha do Size factor, as ordens de entrada e de saída e cada execução própria, de modo que o negócio que produziu uma operação pode ser encontrado ao lado do candle a que pertence.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
