# Diagrama da estratégia Every Nth Bar Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas médias móveis exponenciais se cruzam, e um lado do mercado é comprado enquanto o outro é vendido. O ponto deste diagrama é o que alimenta as médias. Em vez de ler cada candle, elas leem um preço a cada cinco candles, e esse afinamento é feito por um bloco N values ligado de forma a contar o fluxo de candles contra si mesmo. Tudo o que vem depois — as médias, o cruzamento, as entradas — vive nesse relógio mais lento, de modo que o diagrama olha para o mercado uma vez por janela de amostragem, e não uma vez por barra.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos finalizados alimentam um conversor que extrai o preço de fechamento de cada candle e um bloco N values que transforma o mesmo fluxo em um pulso de amostragem.
- O bloco N values recebe o fluxo de candles em suas duas entradas ao mesmo tempo: o gatilho arma a contagem regressiva e a entrada a decrementa, de modo que ele emite um pulso a cada quinto candle finalizado e se rearma imediatamente.
- Esse pulso é o gatilho de um Variable que guarda o último preço de fechamento. A variável armazena cada fechamento à medida que ele chega, mas não libera nada até o pulso chegar, de modo que o que sai dela é uma série de preços afinada — um valor por janela de amostragem.
- Ambas as médias móveis leem essa série afinada em vez dos candles, de modo que uma média de quatorze períodos abrange setenta candles de tempo de mercado e uma de quarenta abrange duzentos.
- Um bloco Crossing observa a média rápida contra a lenta e só se manifesta no momento em que as duas trocam de lugar: verdadeiro quando a média rápida cruza para cima, falso quando cruza para baixo. Um NOT lógico transforma o caso de baixa em um sinal próprio.
- O bloco Position é comparado com uma variável zero por duas comparações — não comprado e não vendido — e cada resultado é unido ao seu sinal de cruzamento por um AND lógico, de modo que uma entrada exige um cruzamento novo e uma posição que ainda não esteja naquele lado.
- Ambos os blocos de entrada estão configurados apenas para abrir, de modo que o diagrama carrega uma posição por vez e nunca a amplia; o volume da ordem vem de uma variável atualizada a cada candle.
- O cruzamento oposto aciona dois blocos de fechamento, a Position protection é armada por cada execução, e o painel de gráfico desenha os candles, as duas médias, as ordens de entrada e de saída, as ordens de proteção e cada execução.

## Regras de entrada e saída

- **Entrada comprada**: A média rápida cruza acima da lenta em um pulso de amostragem e a posição não está comprada. O Position modify compra o volume da ordem a mercado, apenas abrindo, de modo que a entrada é feita com a posição zerada e nunca se soma a um negócio aberto.
- **Entrada vendida**: A média rápida cruza abaixo da lenta em um pulso de amostragem e a posição não está vendida. O NOT lógico transforma o cruzamento de baixa em um sinal, e o Position modify vende o volume da ordem a mercado, apenas abrindo.
- **Saída**: Há duas saídas. A Position protection, armada por cada execução, encerra o negócio com 1.5% de lucro ou em um stop de 1%. Se nenhum dos dois for alcançado antes de as médias trocarem de lugar de volta, o cruzamento oposto assume: ele dispara o bloco de fechamento do lado que está mantido, e esse bloco envia toda a posição a mercado. Como os blocos de entrada apenas abrem, o cruzamento que encerra um negócio não abre o oposto — a próxima entrada aguarda o próximo cruzamento que chegue com a posição zerada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame dos candles com que todo o diagrama trabalha; a janela de amostragem é contada nesses candles. |
| Bars Per Sample | 5 | Quantos candles finalizados formam uma amostra. Aumente o valor e as médias enxergam o mercado com menos frequência e negociam menos; defina-o como um e o diagrama vira um cruzamento comum tomado a cada candle. |
| Fast EMA Length | 14 | Comprimento da média rápida, contado em amostras e não em candles: com cinco candles por amostra, ele cobre cinco vezes essa quantidade de candles de tempo de mercado. |
| Slow EMA Length | 40 | Comprimento da média lenta, em amostras. Mantenha-o bem afastado do comprimento rápido, ou as duas linhas trocam de lugar no ruído e os cruzamentos deixam de significar algo. |
| Order Volume | 1 | Tamanho de cada ordem de entrada, em unidades do instrumento. Os blocos de fechamento o ignoram e enviam o que a posição mantiver. |
| Take Profit, % | 1.5 | Distância do take-profit, em porcentagem do preço de execução. |
| Stop Loss, % | 1 | Distância do stop-loss, em porcentagem do preço de execução. |

## Detalhes do diagrama

- O bloco N values é usado aqui como um afinador, e não como um atraso. Suas duas entradas vêm do mesmo fluxo de candles, de modo que a contagem regressiva recomeça no instante em que expira e o pulso continua caindo a cada quinto candle finalizado durante toda a execução.
- O Variable entre o pulso e as médias é o que torna a reamostragem real: sua entrada aceita cada preço de fechamento, sua saída permanece silenciosa até o gatilho chegar, e assim as médias recebem um valor por janela e nunca veem os candles intermediários.
- Ambas as médias leem a mesma variável, de modo que avançam em sincronia e o bloco Crossing pode emparelhar seus valores conforme eles chegam. Ele relata apenas a amostra em que as duas linhas trocaram de lugar, e é por isso que entradas são impossíveis nos candles entre as amostras.
- Os testes de posição são escritos como não comprado e não vendido, e não como zerado, de modo que um cruzamento que chega enquanto o lado oposto ainda está aberto continua alcançando o bloco de entrada; a configuração de apenas abrir é o que mantém o diagrama em uma única posição, e os blocos de fechamento são o que a liberam.
- Cada execução, tanto de entradas quanto de saídas, é unida por um Combination e enviada à Position protection, de modo que o lado protetivo sempre enxerga a posição que a conta realmente mantém e recua assim que um cruzamento encerra o negócio.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
