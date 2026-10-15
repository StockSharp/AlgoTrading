# Diagrama da estratégia Bands Confirmed Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um candle que abre fora de uma banda de volatilidade e fecha de volta dentro dela é a imagem clássica de um movimento rejeitado, e este diagrama compra e vende exatamente essa imagem. O que o torna mais do que um padrão de um único candle é o que está entre o padrão e a ordem: um canal de preços cuja borda precisa ter se mantido firme, e um bloco de contagem que não libera sua confirmação até que um número definido de candles tenha passado. Somente quando o padrão, o canal e a contagem coincidem no mesmo candle é que uma posição é aberta.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles de quinze minutos alimenta todo o diagrama, e cada valor que entra nele passa antes por um bloco Final, de modo que nada mais adiante jamais vê um candle que ainda está em formação.
- Três indicadores rodam sobre esse fluxo: bandas de volatilidade em torno de uma média móvel, um canal de preços de máximas e mínimas, e um average true range que mede a largura de um candle normal.
- Conversores separam as bandas em uma linha superior e uma inferior, separam o canal em um topo e um fundo, e extraem a abertura, o fechamento, a máxima e a mínima de cada candle fechado.
- Dois blocos Previous value guardam as bordas do canal do candle anterior, e duas comparações perguntam se a borda inferior parou de cair e se a borda superior parou de subir: essa é a definição, no diagrama, de um canal que está se sustentando.
- Cada uma dessas duas respostas arma um bloco N values, que então conta o número configurado de candles fechados e libera um único impulso de confirmação; um novo armamento é ignorado enquanto uma contagem está em curso.
- Uma condição lógica reúne cinco coisas do lado comprado: o candle abriu abaixo da banda inferior, fechou de volta acima dela, o fundo do canal está se sustentando neste momento, o impulso de confirmação acabou de chegar e a posição está zerada. O lado vendido é a mesma condição espelhada em torno da banda superior e do topo do canal.
- Ambas as entradas são ordens a mercado por meio de blocos Position modify configurados para abrir somente a partir de posição zerada, de modo que um sinal que chegue enquanto uma operação está em curso não pode empilhar uma segunda sobre ela.
- Dois blocos Combination reúnem os motivos de saída — uma distância de stop construída a partir do average true range e um fechamento além do canal do candle anterior — e os entregam aos blocos Position modify de fechamento, enquanto um painel de gráfico desenha os candles, os três indicadores, as ordens e as execuções.

## Regras de entrada e saída

- **Entrada comprada**: Um candle fechado abriu abaixo da banda inferior e fechou de volta acima dela, o fundo do canal está no mesmo nível ou acima de onde estava no candle anterior, o bloco de contagem do lado comprado acabou de liberar sua confirmação e a posição está zerada. O bloco Position modify então compra o volume da ordem a mercado. A contagem é o que espaça as operações: após cada liberação, o bloco se rearma no próximo candle cujo fundo do canal ainda esteja se sustentando, de modo que o mesmo padrão no candle seguinte não produz uma segunda entrada.
- **Entrada vendida**: A imagem espelhada. Um candle fechado abriu acima da banda superior e fechou de volta abaixo dela, o topo do canal está no mesmo nível ou abaixo de onde estava no candle anterior, o bloco de contagem do lado vendido liberou sua confirmação e a posição está zerada. O bloco Position modify vende o volume da ordem a mercado.
- **Saída**: Cada lado tem seu próprio bloco Combination guardando dois tipos de motivo. O primeiro é um stop de volatilidade: a compra é encerrada quando a mínima do candle cai abaixo da banda inferior menos o average true range multiplicado pelo multiplicador de stop, e a venda é encerrada quando a máxima do candle sobe acima da banda superior mais a mesma distância. Como a banda se move junto com o mercado, esse stop se ajusta sozinho sem que o diagrama precise lembrar o preço de entrada. O segundo motivo é um rompimento do canal: um fechamento acima do topo do canal do candle anterior, ou um fechamento abaixo do fundo do canal do candle anterior, encerra a operação de qualquer um dos lados — para cima, tira o lucro de uma compra; para baixo, tira o prejuízo dela; e o inverso para uma venda. Ambos os blocos de fechamento estão configurados para fechar a posição, de modo que cada um age apenas sobre o lado ao qual pertence e sempre envia o tamanho inteiro.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:15:00 | Time frame da única série de candles sobre a qual todo o diagrama roda. Um time frame mais curto dá mais padrões e mais operações; um mais longo, menos e mais lentas. |
| Bollinger Length | 100 | Número de candles sobre os quais as bandas de volatilidade são calculadas. Ele também define o aquecimento: nada é negociado até que essa quantidade de candles tenha passado. |
| Bollinger Width | 1 | A quantos desvios-padrão de distância da média fica cada banda. Mantido estreito aqui para que os candles abram regularmente fora de uma banda e fechem de volta dentro; alargue-o para rejeições mais raras e mais extremas. |
| Donchian Length | 100 | Número de candles que o canal de preços abrange. Um canal longo torna suas bordas lentas, que é o que transforma 'a borda parou de se mover contra nós' em um filtro significativo. |
| ATR Length | 21 | Número de candles sobre os quais o average true range é medido. Ele define a unidade em que a distância do stop é expressa. |
| Long Confirm Candles | 5 | Candles que o bloco de contagem do lado comprado espera entre armar e liberar sua confirmação. Um candle elimina a espera por completo e negocia todo padrão; valores maiores rareiam as entradas. |
| Short Confirm Candles | 5 | Candles que o bloco de contagem do lado vendido espera. É um ajuste separado para que os dois lados possam ser calibrados um em relação ao outro. |
| ATR Stop Multiplier | 2 | A quantos average true ranges abaixo da banda inferior fica o stop da compra, e acima da banda superior fica o stop da venda. Reduza-o para saídas mais apertadas e mais frequentes. |
| Order Volume | 1 | Tamanho da ordem, em lotes, enviado na entrada. As saídas sempre fecham o que estiver aberto e não têm tamanho próprio. |

## Detalhes do diagrama

- O bloco de candles está configurado apenas para candles finalizados, e o bloco Final atrás dele impõe a mesma regra dentro do diagrama. Uma atualização de um candle em formação carrega o horário de abertura da barra, e uma ordem construída a partir de um valor desses fica datada atrás do relógio e é recusada, de modo que toda a lógica é mantida sobre candles fechados.
- O bloco N values conta os valores que chegam até ele depois de ter sido armado; ele não verifica se a condição de armamento permaneceu verdadeira o tempo todo. É por isso que a mesma comparação de canal que o arma também é ligada diretamente à condição de entrada: o impulso diz que a espera acabou, e a comparação ao vivo diz se o motivo dela ainda existe.
- As bordas do canal usadas na saída são tomadas um candle atrás. O topo atual de um canal de máximas e mínimas já contém a própria máxima do candle atual, então um fechamento nunca pode subir acima dele; contra a borda do candle anterior, um rompimento é um evento real.
- O stop está ancorado na banda de volatilidade, e não no preço em que a operação foi aberta. Um diagrama não tem memória do preço de entrada a menos que uma variável seja adicionada para guardá-lo, e a banda é de qualquer forma onde a entrada aconteceu, então ela dá a mesma distância de proteção e se move junto com o mercado.
- A posição é protegida duas vezes, de propósito: a comparação de posição zerada fica dentro da condição de entrada, e os próprios blocos de entrada estão configurados para abrir somente quando a posição é zero. A primeira mantém o diagrama legível; a segunda é o que de fato impede uma ordem duplicada se as duas chegarem fora de sincronia.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
