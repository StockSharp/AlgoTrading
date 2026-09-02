# Diagrama da estratégia de impulso Momentum na linha zero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Todo o diagrama se apoia em um único número: a diferença entre o fechamento atual e o fechamento de doze candles atrás. Enquanto essa diferença é positiva o mercado levou o preço para cima ao longo da janela; enquanto é negativa, levou para baixo, e no instante em que troca de sinal o diagrama inverte a posição. Apesar do nome da pasta, o original usa Momentum, uma diferença absoluta de preços, e não uma taxa percentual de variação.

![schema](schema.svg)

## Visão geral da estratégia

- O Momentum de 12 candles é comparado à linha zero, e o valor anterior do mesmo indicador diz de que lado ele veio, de modo que duas comparações formam um cruzamento completo.
- Todo sinal é uma inversão: o volume da ordem é o volume compartilhado mais o valor absoluto da posição atual, então uma única ordem fecha o lado antigo e abre o novo.
- A posição participa dos dois ramos: o cruzamento para cima só é comprado se ainda não houver compra, e o cruzamento para baixo só é vendido se ainda não houver venda.
- O original também congela as operações por 55 candles após cada execução; não existe bloco contador de barras, então essa pausa fica de fora e o diagrama responde a todos os cruzamentos.

## Regras de entrada e saída

- **Entrada comprada**: No candle anterior o Momentum estava em zero ou abaixo, agora está acima e a posição não está comprada. A ordem compra o volume de inversão a mercado, fechando qualquer venda e abrindo a compra em um só passo.
- **Entrada vendida**: No candle anterior o Momentum estava em zero ou acima, agora está abaixo e a posição não está vendida. A ordem vende o volume de inversão a mercado, fechando qualquer compra e abrindo a venda em um só passo.
- **Saída**: Não há bloco de saída próprio. A posição é mantida até que o cruzamento oposto da linha zero a inverta, e o original não possui stop nem o stop por ATR citado no seu README.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Momentum Length | 12 | Número de candles que o Momentum olha para trás: o valor é o fechamento atual menos o fechamento desse número de candles atrás. |
| Volume | 1 | Volume base da ordem, em lotes; a ordem de inversão soma a ele o valor absoluto da posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o indicador Momentum, cuja saída vai tanto para os blocos de comparação quanto para um bloco de valor anterior que guarda a leitura do candle passado.
- Quatro blocos de comparação compartilham uma constante zero, que também serve de referência para as duas checagens de posição.
- Cada E lógico une o lado atual do zero, o lado anterior e a condição de posição, e aciona um bloco de modificação de posição.
- Um bloco de fórmula calcula o tamanho de inversão como o volume compartilhado mais a posição absoluta e alimenta o volume das duas ordens.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
