# Diagrama da estratégia Night Session Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um oscilador estocástico em candles de quatro horas ao qual só é permitido agir à noite. A parte interessante é o relógio, não o oscilador: uma sessão noturna começa à noite e termina na manhã seguinte, de modo que seu início é uma hora do dia posterior ao seu fim, e um único bloco Working time não consegue descrever um intervalo assim. Por isso o diagrama constrói a noite a partir de duas metades - uma antes da meia-noite, outra depois - e as junta em uma única resposta antes que qualquer outra coisa possa olhar para ela.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de quatro horas finalizados conduzem todo o diagrama, portanto cada decisão é tomada em uma barra fechada e nada reage a uma barra ainda em formação.
- Um oscilador Stochastic com %K de 14 barras e %D de 3 barras roda sobre esses candles, e um Converter extrai a linha %K do seu valor; %D é apenas desenhado, nunca negociado.
- Dois blocos Working time leem o momento em que cada candle abre: um cobre das 21:00 às 23:59:59, o outro das 00:00 às 06:00. Nenhum deles sozinho é a noite.
- Um Logical condition configurado como Exclusive or une as duas metades em um único sinal de noite. As metades não podem se sobrepor, então exatamente uma delas pode estar aberta por vez, e o bloco responde uma vez por candle com as duas metades em mãos.
- Dois blocos Comparison colocam %K contra os níveis de sobrevenda e sobrecompra; outros dois colocam a posição contra zero, o que diz ao diagrama se ele está zerado, comprado ou vendido.
- Quatro portas And de Logical condition combinam esses três fatos - noite, oscilador, posição - em duas entradas e duas saídas, de modo que nenhuma porta pode disparar sem que o relógio concorde com ela.
- As entradas são blocos Position modify que funcionam sob a condição Open position: uma ordem a mercado de Order Volume só sai enquanto a posição for exatamente zero, e é isso que impede que um sinal vire uma sequência de ordens.
- O painel de gráfico desenha os candles, o oscilador e cada ordem e execução que o diagrama produz, de modo que as janelas noturnas podem ser lidas diretamente da imagem.

## Regras de entrada e saída

- **Entrada comprada**: Enquanto a noite está aberta, a posição está zerada e %K está abaixo do nível de sobrevenda, a porta de compra dispara e o Position modify compra Order Volume a mercado. A condição Open position nesse bloco significa que uma repetição da mesma leitura não muda nada até que a posição seja fechada novamente.
- **Entrada vendida**: A imagem espelhada: noite aberta, posição zerada e %K acima do nível de sobrecompra fazem a porta de venda disparar, e o Position modify vende Order Volume a mercado sob a mesma condição Open position.
- **Saída**: Não há take-profit, não há stop-loss e não há zeragem por horário. Uma posição comprada é fechada pelo extremo oposto - %K acima do nível de sobrecompra enquanto a posição está comprada - e uma posição vendida, por %K abaixo do nível de sobrevenda enquanto a posição está vendida, ambas por meio de um bloco Position modify configurado como Close position, que lê sozinho a quantidade em aberto. As saídas também estão dentro da janela noturna, de modo que uma posição aberta à noite é carregada pelo dia e liberada na noite seguinte. Fechar e inverter são dois eventos separados: o extremo que fecha uma compra apenas a zera, e é uma leitura posterior do mesmo tipo que abre a venda.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 04:00:00 | Time frame de quatro horas, construído a partir dos candles menores disponíveis nos dados. Somente candles finalizados são processados, e cada um deles é datado pelo seu momento de abertura, que é o que decide a sessão à qual ele pertence. |
| %K Length | 14 | Janela de observação da linha %K: contra quantos candles o oscilador mede o fechamento. |
| %D Length | 3 | Comprimento de suavização da linha %D. Ela é desenhada no painel e não faz parte de nenhuma condição. |
| Evening Half From | 21:00:00 | Início da metade da noite que fica antes da meia-noite. Mantenha as duas metades separadas: elas devem se encontrar na meia-noite, não se sobrepor. |
| Evening Half Until | 23:59:59 | Fim da metade vespertina. Um segundo antes da meia-noite a encerra sem deixar que ela toque a metade seguinte. |
| Morning Half From | 00:00:00 | Início da metade da noite que fica depois da meia-noite, que é a própria meia-noite. |
| Morning Half Until | 06:00:00 | Fim da metade matutina e, com ela, o fim da noite. Depois dessa hora nenhuma porta do diagrama pode disparar até que a metade vespertina se abra novamente. |
| Oversold Level | 30 | Nível abaixo do qual %K é tratado como sobrevendido: ele abre uma compra quando a posição está zerada e fecha uma venda quando há uma aberta. |
| Overbought Level | 70 | Nível acima do qual %K é tratado como sobrecomprado: ele abre uma venda quando a posição está zerada e fecha uma compra quando há uma aberta. |
| Order Volume | 1 | Quantidade enviada por ambas as entradas. As saídas a ignoram e fecham o que estiver aberto. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) assina uma série de quatro horas com a opção de apenas candles formados ligada, e carimba cada valor que envia com o momento em que o candle abriu. É esse carimbo que os blocos de relógio leem, portanto um candle pertence à sessão em que cai sua hora de abertura, aconteça o que acontecer durante as quatro horas seguintes.
- [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) contém o oscilador Stochastic e só repassa valores depois que ele está formado, de modo que as primeiras barras do histórico preparam o cálculo sem produzir sinais. Um [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) lê o campo %K do valor do oscilador e entrega um número simples às comparações.
- Os dois blocos [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) são o ponto do diagrama. Cada um é verdadeiro enquanto a hora do dia estiver entre seus próprios dois limites, o que significa que um bloco nunca pode descrever uma janela que atravesse a meia-noite: seu início seria posterior ao seu fim e a verificação nunca poderia passar. Dividir a noite na meia-noite dá duas janelas comuns, e o [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) acima delas recompõe a noite.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) é comparado contra uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) igual a zero por três blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) ao mesmo tempo - igual, maior, menor - e essas três respostas são o que separa as duas portas de entrada das duas portas de saída. Os níveis de sobrevenda e sobrecompra e o volume da ordem também são Variables, portanto cada número sobre o qual o diagrama discute é um parâmetro e não um valor enterrado dentro de um bloco.
- Quatro blocos [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) agem sobre as portas. As duas entradas carregam a condição Open position e tiram sua quantidade da Variable de volume; as duas saídas carregam a condição Close position e não precisam de quantidade, porque uma ordem de fechamento é dimensionada pela posição que ela desfaz. Suas ordens e execuções vão para o [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto com os candles e o oscilador.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
