# Diagrama da estratégia de cruzamento do zero do Momentum com filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas ideias são empilhadas aqui. O Momentum, a diferença entre o fechamento atual e o fechamento de dez candles atrás, diz para onde o mercado empurrou o preço nesse trecho, e a troca de sinal dessa diferença é o gatilho. Uma média móvel simples atua então como juiz: o cruzamento só é aproveitado na direção com que o fechamento já concorda.

![schema](schema.svg)

## Visão geral da estratégia

- O cruzamento da linha zero é escrito com duas comparações, o valor atual contra zero e o valor de um candle atrás contra zero, exatamente a condição do código original.
- O filtro da média móvel separa as direções: o cruzamento para cima só compra enquanto o fechamento está acima da média, o cruzamento para baixo só vende enquanto está abaixo.
- Apesar do nome da pasta, o indicador é o Momentum, uma diferença absoluta de preços em pontos, e não uma taxa percentual de variação.
- Todo sinal inverte a posição: o volume da ordem é o volume compartilhado mais o valor absoluto da posição atual, então uma única execução fecha o lado antigo e abre o novo.
- O original congela as operações por 30 candles após cada execução; não existe bloco contador de barras, então essa pausa fica de fora e o diagrama responde a todos os cruzamentos válidos.

## Regras de entrada e saída

- **Entrada comprada**: No candle anterior o Momentum estava em zero ou abaixo, agora está acima, o fechamento está acima da SMA e a posição não está comprada. A ordem compra a mercado o volume de inversão.
- **Entrada vendida**: No candle anterior o Momentum estava em zero ou acima, agora está abaixo, o fechamento está abaixo da SMA e a posição não está vendida. A ordem vende a mercado o volume de inversão.
- **Saída**: Não há bloco de saída próprio nem stop de proteção, como no original: a posição é mantida até que o cruzamento oposto a inverta com uma única ordem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Momentum Length | 10 | Número de candles que o Momentum olha para trás; o valor é o fechamento atual menos o fechamento desse número de candles atrás. |
| SMA Length | 20 | Período da média móvel simples que filtra a direção do cruzamento. |
| Volume | 1 | Volume base da ordem, em lotes; a ordem de inversão soma a ele o valor absoluto da posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta três ramos: o indicador Momentum, a média móvel simples e um conversor que pega o preço de fechamento.
- Um bloco de valor anterior guarda a leitura do Momentum do candle passado, e quatro blocos de comparação colocam a leitura atual e a anterior de cada lado de uma constante zero compartilhada.
- Outros dois blocos de comparação confrontam o fechamento com a média móvel, e mais dois comparam a posição com essa mesma constante zero.
- Cada E lógico une o lado anterior do zero, o lado atual, o filtro da média e a checagem de posição, e aciona um bloco de modificação de posição cujo volume vem de uma fórmula de volume mais posição absoluta.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
