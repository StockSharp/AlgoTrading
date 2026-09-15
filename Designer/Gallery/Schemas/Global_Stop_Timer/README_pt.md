# Diagrama da estratégia Global Stop Timer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

As entradas são decididas pelo preço, as saídas são decididas pelo dinheiro. O cruzamento do momentum com seu nível neutro abre uma posição na direção que a média móvel já aponta e, a partir desse momento, o resultado em aberto da estratégia é o dono da operação: o bloco P&L acompanha cada alteração do valor não realizado e zera a posição assim que ele cai até o stop financeiro ou atinge a meta financeira, independentemente do que os indicadores estejam dizendo.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles, de cinco minutos e apenas barras finalizadas, alimenta tudo: os dois indicadores, o conversor de preço de fechamento e os gatilhos das variáveis constantes.
- O momentum mede quanto o preço percorreu ao longo do seu próprio período; o bloco Crossing o compara com um nível guardado em uma variável e dispara apenas no momento em que os dois trocam de lado, verdadeiro para o cruzamento para cima e falso para o cruzamento para baixo.
- Um NOT lógico transforma o mesmo cruzamento no evento de baixa, de modo que um único bloco Crossing atende às duas direções e os dois nunca podem disparar na mesma barra.
- Um conversor extrai o preço de fechamento do candle e duas comparações o posicionam acima ou abaixo da média móvel, que é o filtro de tendência pelo qual as duas entradas precisam passar.
- O bloco Position é comparado com zero três vezes: a posição zerada protege as entradas, comprado e vendido armam as duas saídas por sinal.
- Cada porta de entrada é um AND lógico de três respostas — o cruzamento, o lado da média móvel e a posição zerada — e a entrada em si é uma ordem a mercado executada somente a partir da posição zerada, de modo que um sinal que chega enquanto uma operação está em andamento não muda nada.
- O bloco P&L não tem entradas nem ritmo próprio de candles: ele emite o resultado não realizado a cada alteração, e duas comparações medem esse número contra o stop financeiro e a meta financeira guardados em variáveis.
- O veredito financeiro e os dois vereditos de cruzamento contrário chegam a um único bloco Combination, que aciona um único Position modify configurado para fechar: qualquer que seja o motivo que venha primeiro, a posição é zerada pela mesma ordem a mercado.

## Regras de entrada e saída

- **Entrada comprada**: O momentum cruza seu nível para cima, o candle fecha acima da média móvel e a posição está zerada. O Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: O momentum cruza seu nível para baixo — o mesmo bloco de cruzamento lido através do NOT lógico —, o candle fecha abaixo da média móvel e a posição está zerada. O Position modify vende o volume da ordem a mercado.
- **Saída**: Três motivos fecham uma operação e todos os três terminam no mesmo bloco. O resultado não realizado da estratégia cai até o stop financeiro; ou atinge a meta financeira; ou o momentum cruza de volta o seu nível enquanto a posição está aberta na direção oposta. Os dois primeiros são verificados a cada alteração do P&L, e não no ritmo dos candles, de modo que um movimento rápido é respondido dentro da barra; o terceiro é verificado uma vez por candle finalizado. A ordem de fechamento é dimensionada a partir da posição atual, portanto ela sempre deixa a estratégia zerada e nunca inverte em uma única etapa.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles sobre a qual todo o diagrama funciona; apenas candles finalizados são usados. |
| Momentum Length | 10 | Período do indicador de momentum — o quanto para trás o movimento do preço é medido. |
| EMA Length | 20 | Período da média móvel que decide de qual lado da tendência uma entrada é permitida. |
| Momentum Level | 0 | Nível com o qual o momentum é comparado. Zero é o seu valor neutro: acima dele o preço está mais alto do que estava um período atrás; abaixo dele, mais baixo. |
| Order Volume | 1 | Tamanho da ordem, em lotes, para as duas entradas. A ordem de fechamento o ignora e é dimensionada a partir da posição. |
| Money Stop | -250 | Prejuízo na posição aberta, na moeda da conta, no qual a posição é fechada. Negativo. |
| Money Target | 500 | Lucro na posição aberta, na moeda da conta, no qual a posição é fechada. |

## Detalhes do diagrama

- Os dois indicadores estão configurados para emitir apenas valores formados e finais, de modo que um candle não finalizado não pode produzir um cruzamento.
- O nível com o qual o momentum é comparado fica em uma variável, e não dentro da comparação, e é isso que o torna um parâmetro do esquema que pode ser alterado e otimizado sem abrir o diagrama.
- Toda variável constante é acionada por um gatilho — as três ligadas aos candles, pela série de candles, e os dois limites financeiros, pelo P&L não realizado —, porque uma variável guarda o seu valor, mas só o emite quando algo o solicita.
- As entradas carregam a condição de abertura de posição, portanto disparam somente a partir da posição zerada. Sem ela, uma ordem a mercado seria enviada a cada alteração da posição e a execução ficaria cheia de operações indesejadas.
- O stop financeiro e a meta financeira são valores na moeda da conta, não distâncias em preço, portanto precisam ser reescalados junto com o volume da ordem quando o diagrama é levado para outro instrumento.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
