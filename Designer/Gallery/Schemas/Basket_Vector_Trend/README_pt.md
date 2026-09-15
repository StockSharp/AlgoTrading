# Diagrama da estratégia de tendência de cesta ponderada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama negocia dois instrumentos como uma única cesta e decide sobre três séries de preço, e não duas. Um instrumento sintético é montado a partir dos dois instrumentos negociados, com um divisor dentro de sua expressão que reduz a escala da perna cara até que a barata ainda consiga mover o resultado; a tendência desse instrumento sintético é o que o diagrama chama de direção da cesta. Cada perna é então medida da mesma forma, por conta própria. Uma perna só é comprada ou vendida quando sua própria tendência concorda com a da cesta e, quando a cesta vira, toda perna que passa a ficar na direção errada é fechada. Nada mais fecha uma posição: não há alvo nem stop, e o sinal da cesta é tanto a razão para estar posicionado quanto a razão para sair.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de índice de instrumentos constrói um instrumento sintético a partir dos dois instrumentos negociados. O peso vive dentro da expressão, de modo que as duas pernas contribuem em uma escala comparável, em vez de o preço maior abafar o menor.
- Três séries de candles rodam no mesmo timeframe: uma sobre a cesta sintética e uma sobre cada perna. As três são assinadas apenas como candles finalizados, de modo que toda leitura a jusante pertence a uma barra que já fechou.
- Cada série alimenta uma média móvel suavizada rápida e uma lenta, e uma fórmula subtrai a lenta da rápida. O resultado é uma diferença de tendência com sinal, e há três delas: uma para a cesta e uma para cada perna.
- Cada diferença é capturada por uma variável que a retém e a libera quando um candle da primeira perna se completa, de modo que uma decisão nunca é montada a partir de uma leitura da cesta tomada em um momento e de uma leitura da perna tomada em outro.
- O valor vem da série que o produziu por último, o momento vem do candle negociado e, por isso, tudo o que está a jusante carrega o timestamp da barra em que a ordem é enviada.
- Seis comparações transformam as três diferenças retidas em flags com sinal contra zero: cesta para cima ou para baixo, primeira perna para cima ou para baixo, segunda perna para cima ou para baixo.
- Dois blocos de posição, cada um vinculado a um instrumento, informam o que já está mantido, e mais duas comparações dizem se aquela perna está atualmente zerada. É isso que impede que um sinal repetido empilhe uma segunda entrada na mesma perna.
- Quatro condições lógicas reúnem três flags cada uma — direção da cesta, direção da perna, perna zerada — e cada uma dispara um bloco de ordem a mercado. Outros quatro blocos de ordem cuidam das saídas, acionados diretamente pelos dois flags da cesta.

## Regras de entrada e saída

- **Entrada comprada**: Uma perna é comprada quando a diferença da cesta é positiva, a diferença da própria perna é positiva e nada está mantido nessa perna. As duas pernas são decididas de forma independente na mesma barra, então ambas podem ficar compradas ao mesmo tempo, uma pode ficar comprada enquanto a outra permanece de fora, ou nenhuma delas pode se qualificar.
- **Entrada vendida**: Uma perna é vendida quando a diferença da cesta é negativa, a diferença da própria perna é negativa e nada está mantido nessa perna. O lado vendido é o espelho exato do lado comprado, e vale a mesma independência entre as pernas.
- **Saída**: A única saída é uma mudança no sinal da diferença da cesta. Uma cesta negativa dispara os dois blocos de fechamento que carregam a direção de venda, e uma cesta positiva dispara os dois que carregam a direção de compra. A direção em um bloco de fechamento é um filtro, e não uma instrução: o bloco atua apenas sobre uma posição voltada para o lado oposto, de modo que um bloco de fechamento do lado vendido toca uma perna comprada e não faz absolutamente nada quando essa perna está zerada ou já vendida. Uma perna cuja própria diferença virou, mas cuja cesta ainda não virou, é deixada em paz até que a cesta concorde.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| First Leg | BTCUSDT@BNBFT | O primeiro instrumento negociado. Ele alimenta sua própria série de candles, seu próprio par de médias, seu próprio bloco de posição e seus próprios quatro blocos de ordem; alterá-lo move toda essa perna. |
| Second Leg | TONUSDT@BNBFT | O segundo instrumento negociado, ligado da mesma forma que o primeiro. As duas pernas são simétricas, e nenhuma é subordinada à outra. |
| Basket Index | BTCUSDT@BNBFT / 20000 + TONUSDT@BNBFT | A expressão a partir da qual o instrumento sintético da cesta é construído. O divisor é o que coloca as duas pernas em uma escala comparável — aumente-o para deixar a segunda perna dominar, reduza-o para dar mais peso à primeira perna — e o sinal dessa série é o que autoriza cada entrada. |
| Basket Candles | 00:15:00 | Timeframe dos candles da cesta. Mantenha-o igual aos timeframes das pernas: os três fluxos devem ser lidos como uma única barra. |
| First Leg Candles | 00:15:00 | Timeframe dos candles da primeira perna. Essa série também é o relógio de negociação: ela dispara as retenções, as constantes e, portanto, o momento em que cada ordem é enviada. |
| Second Leg Candles | 00:15:00 | Timeframe dos candles da segunda perna. Mantido igual ao da primeira perna para que as duas pernas sejam julgadas em barras do mesmo tamanho. |
| Basket Fast Length | 3 | Comprimento da média rápida da cesta. Mais curto reage mais cedo e inverte o sinal da cesta com mais frequência, o que abre e fecha posições com mais frequência. |
| Basket Slow Length | 7 | Comprimento da média lenta da cesta. A distância entre este e o comprimento rápido define quão decisivo um movimento precisa ser antes de a cesta ser considerada revertida. |
| First Leg Fast Length | 3 | Comprimento da média rápida da primeira perna. Ele apenas decide se essa perna concorda com a cesta; nunca define a direção da cesta em si. |
| First Leg Slow Length | 7 | Comprimento da média lenta da primeira perna. Ampliar a distância entre os dois comprimentos faz esta perna confirmar com menos frequência, então a cesta pode virar sem que ela acompanhe. |
| Second Leg Fast Length | 3 | Comprimento da média rápida da segunda perna, cumprindo o mesmo papel de confirmação para aquele instrumento. |
| Second Leg Slow Length | 7 | Comprimento da média lenta da segunda perna. As duas pernas podem ser ajustadas de forma diferente de propósito, caso uma delas seja a mais ruidosa do par. |
| First Leg Volume | 0.1 | Tamanho de uma ordem na primeira perna, nas unidades do próprio instrumento. Vale a pena defini-lo junto com o volume da segunda perna, para que uma cesta completa coloque um valor comparável em cada lado. |
| Second Leg Volume | 2000 | Tamanho de uma ordem na segunda perna. Os dois volumes são separados porque os instrumentos são precificados em escalas completamente diferentes, e um único número compartilhado tornaria uma das pernas irrelevante. |

## Detalhes do diagrama

- O peso que equilibra as duas pernas faz parte da expressão do índice, e não é um número ligado ao diagrama. Rebalancear a cesta significa editar essa única string de parâmetro, e toda a série sintética é reconstruída a partir dela.
- Os candles são assinados apenas como finalizados. Uma ordem roteada a partir de um candle em atualização carrega o timestamp da abertura da barra, que é anterior ao momento em que ela é de fato enviada, e é recusada por esse motivo; tomar apenas barras fechadas mantém toda ordem carimbada com o momento a que ela pertence.
- As três variáveis de retenção são o que torna a cesta utilizável. Um instrumento sintético é montado a partir de dois feeds e sua barra se completa mais tarde do que a de um instrumento comum, de modo que um bloco que espera que as três leituras caiam dentro de uma mesma janela espera por um conjunto que nunca chega. Reter cada leitura no candle negociado toma o valor como ele está e o momento a partir do candle, e as comparações, as condições lógicas e as ordens rodam todas no relógio da barra negociada.
- Os blocos de entrada estão configurados para abrir posição, então uma entrada só atua a partir do zerado. Um sinal que permanece verdadeiro ao longo de várias barras produz, portanto, uma ordem, e não uma por barra, e os blocos de saída são o único caminho de volta ao zerado.
- O painel de gráfico desenha as três séries de candles, as seis médias móveis e cada ordem e execução dos oito blocos de ordem, de modo que uma barra pode ser lida contra a cesta que a autorizou e a perna que a confirmou.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
