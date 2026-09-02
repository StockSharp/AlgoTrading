# Diagrama da estratégia martelo / martelo invertido com filtro de SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um martelo é um candle de corpo pequeno, sombra inferior longa e praticamente sem sombra superior: dentro da barra o preço foi empurrado bem para baixo e recomprado antes do fechamento. O martelo invertido é a sua imagem espelhada. Sozinhas, essas formas aparecem em todo lugar, então uma média móvel simples decide onde vale a pena aproveitá-las: o martelo só é comprado abaixo da média e o invertido só é vendido acima dela.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de padrão de candles carregam exatamente as fórmulas da estratégia original: corpo maior que zero, uma sombra mais longa que o dobro do corpo e a sombra oposta menor que metade do corpo.
- Os padrões nativos Hammer e Inverted Hammer são propositalmente evitados, porque medem as sombras contra o comprimento do candle e não contra o corpo.
- A média móvel simples do preço de fechamento divide o gráfico numa metade barata e outra cara, servindo ao mesmo tempo de filtro de entrada e de linha de saída.
- A verificação da posição garante que um padrão só seja operado a partir do zero.

## Regras de entrada e saída

- **Entrada comprada**: O bloco de padrão informa um martelo, o candle fechou abaixo da média móvel e a posição está zerada. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O bloco de padrão informa um martelo invertido, o candle fechou acima da média móvel e a posição está zerada. A ordem vende um lote e abre uma venda.
- **Saída**: A compra é encerrada quando um candle fecha acima da média móvel e a venda quando fecha abaixo dela, ambas por blocos de modificação de posição em modo de fechamento. A estratégia original sai pelo mesmo lado da média por onde entrou e segura a operação com uma pausa de várias centenas de barras; aqui não existe bloco contador de barras, então copiar essa saída ao pé da letra encerraria cada operação já no candle seguinte. O retorno à média é a regra mais próxima que ainda mantém a posição por um trecho razoável.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período de suavização da média móvel simples que filtra os padrões e encerra as operações. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois blocos de padrão, a média móvel e um conversor que extrai o preço de fechamento do candle.
- Dois blocos de comparação colocam esse fechamento contra a média e são reaproveitados duas vezes cada: como filtro de entrada de um lado e como gatilho de saída do outro.
- O bloco de posição é comparado com uma constante zero, e cada E lógico une o padrão, o lado da média e essa proteção.
- Os dois blocos de entrada enviam ordens a mercado e tiram o volume de uma constante compartilhada; os dois blocos de saída trabalham em modo de fechamento e só agem quando há algo a encerrar.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
