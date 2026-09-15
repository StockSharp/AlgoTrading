# Diagrama de força relativa frente a um instrumento de referência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama constrói um instrumento sintético a partir da razão entre o instrumento negociado e um instrumento de referência, mede o quanto essa razão se afastou da sua própria média móvel simples de 60 períodos e negocia o instrumento negociado com base no resultado. Uma razão um por cento acima da sua média significa que o instrumento negociado está superando o de referência e o diagrama abre comprado; um por cento abaixo significa que está ficando para trás e o diagrama abre vendido. As ordens vão sempre para o instrumento negociado; o instrumento sintético participa apenas da decisão.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco Security index constrói um instrumento sintético a partir da expressão `BTCUSDT@BNBFT/TONUSDT@BNBFT`. Seus candles são o preço de um instrumento expresso em unidades do outro, portanto uma série ascendente significa que o instrumento negociado está ganhando do de referência.
- Os candles de cinco minutos finalizados desse instrumento sintético alimentam um Converter que lê o preço de fechamento e uma SimpleMovingAverage de comprimento 60 apenas formada, o que corresponde a cinco horas da mesma série.
- Uma Formula divide o fechamento da razão pela sua média e subtrai um, produzindo a força relativa como fração: `+0.01` significa que a razão está um por cento acima da sua média de cinco horas, `-0.01` um por cento abaixo.
- Uma série separada de candles de cinco minutos finalizados do instrumento negociado conduz o ciclo de decisão. Uma Variable, cuja entrada serve apenas de armazenamento, guarda a última força relativa e a libera no candle negociado, de modo que cada comparação e cada ordem levam o horário da barra negociada, e não o da sintética.
- Dois blocos Comparison testam o valor liberado contra o limiar e contra o seu negativo, produzido por uma Formula `0 - a`, de modo que um único número exposto governa os dois lados de forma simétrica.
- A Position atual é comparada com zero duas vezes, dando `Position <= 0` e `Position >= 0`. Dois blocos Logical condition combinam cada sinal de força com o teste de posição correspondente, de modo que um lado já aberto não pode receber outra entrada.
- A partir de posição zerada, um bloco Position modify configurado como Open position compra ou vende o Order Volume fixo a mercado. Não há bloco de stop-loss nem de take-profit.
- Uma posição aberta contra o sinal novo é primeiro zerada por um bloco Position modify configurado como Close position, o que deixa a reversão para o próximo candle que atenda à condição.

## Regras de entrada e saída

- **Entrada comprada**: Em um candle negociado finalizado, quando a força relativa liberada é maior que o Strength Threshold e a posição não está comprada, o portão de compra dispara. A partir de posição zerada, o bloco Open position compra o Order Volume a mercado. A partir de uma posição vendida, a entrada é recusada nessa barra, porque a ação de fechamento é a que roda primeiro; a compra é aberta no próximo candle que ainda mostrar desempenho superior.
- **Entrada vendida**: Em um candle negociado finalizado, quando a força relativa liberada é menor que o negativo do Strength Threshold e a posição não está vendida, o portão de venda dispara. A partir de posição zerada, o bloco Open position vende o Order Volume a mercado. A partir de uma posição comprada, a entrada é recusada nessa barra, porque a ação de fechamento é a que roda primeiro; a venda é aberta no próximo candle que ainda mostrar desempenho inferior.
- **Saída**: Não há regra de saída independente, nem stop-loss nem take-profit. Uma posição só sai com um sinal na direção oposta: o desempenho superior fecha uma venda, o desempenho inferior fecha uma compra. A ação de fechamento toma o seu tamanho da posição aberta, de modo que a conta fica zerada em seguida, e o lado oposto abre no candle seguinte se o sinal ainda estiver presente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | Expressão a partir da qual o instrumento sintético é construído. O primeiro instrumento é o numerador e o segundo é o denominador de referência, portanto a série sobe quando o numerador ganha do de referência. Altere-a para medir o instrumento negociado contra uma referência diferente. |
| Ratio Candles | 00:05:00 | Tempo gráfico dos candles do instrumento sintético. Ele tem de coincidir com a série negociada, porque a força liberada é um valor por barra negociada. |
| Traded Candles | 00:05:00 | Tempo gráfico dos candles do instrumento negociado. Cada comparação, cada entrada e cada saída é avaliada uma vez por candle finalizado desta série. |
| Reference Average Length | 60 | Número de barras da média móvel simples da razão. Sessenta barras de cinco minutos medem a força das últimas cinco horas; uma média mais longa mede uma divergência mais lenta e mais rara e produz menos negócios. |
| Strength Threshold | 0.01 | Distância da média, em fração, que a razão tem de percorrer antes de um lado ser tomado. `0.01` é um por cento, aplicado acima da média para entradas compradas e abaixo dela para entradas vendidas. Reduza-o para negociar com mais frequência, aumente-o para exigir uma divergência maior. |
| Order Volume | 1 | Quantidade fixa de cada entrada. As ações de fechamento a ignoram e tomam o seu tamanho da posição aberta. |

## Detalhes do diagrama

- O bloco [Security index](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html) carrega a expressão a partir da qual o instrumento sintético é construído e alimenta a entrada Security de um bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html). Um segundo bloco Candles, mantido no instrumento da estratégia, fornece a série negociada. Ambos estão configurados apenas para candles finalizados, de modo que nenhuma barra em formação possa datar uma ordem na abertura da sua própria barra.
- Um [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) lê o preço de fechamento do candle sintético e um [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) apenas formado calcula a média da mesma série sobre 60 barras. A [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `a / b - 1` transforma o par em uma única fração com sinal, e uma segunda Formula `0 - a` espelha o limiar para o lado fraco.
- Um candle sintético é montado a partir de dois feeds e se completa depois de um candle comum do mesmo minuto. Por isso uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) armazena a força na sua entrada e a emite apenas com o gatilho do candle negociado, o que mantém os horários das ordens no relógio negociado. As constantes do limiar, do zero e do volume da ordem são disparadas pelo mesmo candle, para que cada [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) veja os seus dois operandos dentro de uma mesma avaliação.
- A [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) é comparada com zero em cada candle negociado, e dois blocos [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) juntam o resultado da força com o resultado da posição. Apenas um resultado `true` chega a um bloco de negociação; uma comparação `false` é descartada na entrada de gatilho.
- Quatro blocos [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) atuam por ordem a mercado. Os dois blocos de entrada usam a condição Open position, portanto atuam apenas a partir de conta zerada e não podem repetir enquanto um lado estiver aberto. Os dois blocos de saída usam a condição Close position, que dimensiona a si mesma pela posição aberta e não faz nada quando a conta já está zerada ou já está no lado solicitado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
