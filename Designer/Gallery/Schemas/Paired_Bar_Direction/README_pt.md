# Diagrama da estratégia Paired Bar Direction
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois instrumentos, uma barra. O diagrama lê a direção do mesmo candle de cinco minutos no instrumento negociado e em um segundo instrumento, o de referência, e compra apenas quando os dois divergem: o candle de referência fechou em alta enquanto o candle negociado fechou em baixa. A posição é devolvida assim que o preço fecha acima da máxima da barra anterior.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de candles alimentam o diagrama. O negociado roda no próprio instrumento da estratégia; o de referência é apontado para um instrumento nomeado por uma variável Security, de modo que o par é um parâmetro e não uma decisão de ligação.
- As duas séries estão configuradas somente para candles finalizados, portanto uma barra inacabada nunca pode alterar a decisão.
- O Sync mantém uma linha por instrumento e libera os dois candles juntos no ritmo de cinco minutos. Os dois fluxos chegam de forma independente, e só depois dessa retenção os dois candles pertencem à mesma barra — que é justamente o que torna significativa a comparação entre eles.
- Conversores extraem a abertura e o fechamento de cada candle liberado, reduzindo cada instrumento aos dois números que dizem para que lado foi a sua barra.
- Duas comparações leem esses números: fechamento de referência acima da abertura de referência significa que a barra de referência fechou em alta; fechamento negociado abaixo da abertura negociada significa que a barra negociada fechou em baixa.
- O Previous value guarda o candle negociado um passo atrás, e um conversor lê a máxima dele. O bloco guarda o próprio candle e o campo é lido depois, e é essa sequência que garante um valor em toda barra.
- A posição é encaixada na barra por uma variável que emite no gatilho do candle e em seguida é comparada com zero duas vezes: igual ou abaixo de zero admite uma entrada, acima de zero admite uma saída.
- Dois blocos lógicos AND acionam dois blocos Position modify — um abre com a condição Open position, o outro fecha com Close position — e o painel do gráfico mostra os candles negociados, o nível de saída, as ordens e as execuções.

## Regras de entrada e saída

- **Entrada comprada**: Em uma barra liberada, o instrumento de referência fechou acima da própria abertura, o instrumento negociado fechou abaixo da própria abertura e a posição está igual ou abaixo de zero. O bloco AND dispara e o Position modify compra o volume da ordem a mercado com a condição Open position, de modo que uma barra que repete o padrão enquanto a posição já está aberta não acrescenta nada.
- **Entrada vendida**: Não há lado vendido. O diagrama opera apenas comprado: uma barra de baixa no instrumento negociado é lida como o desconto para comprar, nunca como motivo para vender.
- **Saída**: Enquanto a posição está acima de zero, uma barra liberada cujo fechamento fica acima da máxima da barra anterior aciona o bloco lógico de saída, e o segundo Position modify fecha a mercado com a condição Close position. Não há stop loss nem take profit: a máxima da barra anterior é toda a regra de saída e, como o bloco fecha o que está aberto em vez de vender um tamanho fixo, uma saída não pode inverter a posição para vendida.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | O instrumento cuja direção da barra confirma a entrada; ele precisa estar disponível na conexão junto com o negociado. |
| Reference Candles | 00:05:00 | Time frame dos candles de referência. |
| Traded Candles | 00:05:00 | Time frame dos candles negociados. |
| Sync Interval | 00:05:00 | O ritmo no qual o Sync libera os dois instrumentos; mantenha-o igual ao time frame dos candles, caso contrário o par liberado não é a barra que as comparações pressupõem. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |

## Detalhes do diagrama

- Os dois blocos Position modify estão configurados para não exigir conexão online, portanto o mesmo diagrama se comporta de forma idêntica no histórico e em um fluxo ao vivo.
- A entrada carrega a condição Open position de propósito. Sem ela, o bloco atuaria em toda mudança de posição pela qual fosse acionado, e um único sinal viraria uma sequência de ordens.
- Toda constante do diagrama — o zero e o volume da ordem — é acionada pelo candle liberado. Uma variável emite no seu gatilho, e não pelo próprio valor, então uma constante não acionada deixaria mudas, durante toda a execução, as comparações ao seu lado.
- As duas pontas de cada linha do Sync estão ligadas. Um valor enviado ao bloco e nunca retirado o deixa esperando por uma linha que nunca se fecha, e a estratégia nem chegaria a iniciar.
- O nível de saída é desenhado no gráfico como uma linha própria, de modo que a barra que fecha acima dele pode ser lida direto no painel, ao lado da execução que veio em seguida.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
