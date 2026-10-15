# Diagrama da estratégia de reversão Heikin-Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os candles Heikin-Ashi eliminam boa parte do ruído por média, de modo que uma sequência mantém a mesma cor enquanto o movimento dura e só vira quando o equilíbrio realmente muda. Este diagrama opera essa virada: o primeiro candle Heikin-Ashi de alta depois de um de baixa compra, o primeiro de baixa depois de um de alta vende, e uma média móvel simples do fechamento comum decide quando a operação termina.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de fórmula monta o corpo Heikin-Ashi como a média de abertura, máxima, mínima e fechamento menos o ponto médio do candle anterior: corpo positivo é um candle Heikin-Ashi de alta, zero ou menos é de baixa.
- Um bloco de valor anterior guarda o corpo do candle precedente, de modo que as duas comparações juntas descrevem uma mudança de cor, e não apenas uma cor.
- A média móvel e o preço de saída vêm dos candles comuns, não dos suavizados.
- A abertura Heikin-Ashi normalmente é definida pelo próprio valor anterior, algo que um diagrama não consegue realimentar em um bloco; em seu lugar usa-se o ponto médio do candle comum anterior, de modo que as mudanças de cor são uma aproximação.

## Regras de entrada e saída

- **Entrada comprada**: O corpo Heikin-Ashi do candle recém-encerrado é positivo, o do candle anterior era zero ou negativo e a posição é zero. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O corpo Heikin-Ashi do candle recém-encerrado é zero ou negativo, o do candle anterior era positivo e a posição é zero. A ordem vende um lote e abre uma venda.
- **Saída**: Uma compra é encerrada por um bloco de modificação de posição em modo de fechamento quando um candle comum fecha abaixo da média móvel; uma venda é encerrada quando um fecha acima dela. O diagrama não tem stop loss nem take profit.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período da média móvel simples sobre o fechamento comum, que encerra as operações. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico de cinco minutos usado por todo o diagrama e pelo histórico empacotado da galeria. |

## Detalhes do diagrama

- O bloco de candles alimenta quatro conversores de abertura, máxima, mínima e fechamento, além da média móvel.
- Dois blocos de valor anterior entregam à fórmula a abertura e o fechamento do candle anterior, com os quais a abertura Heikin-Ashi é aproximada.
- Um terceiro bloco de valor anterior atrasa o resultado da fórmula em um candle, e quatro comparações com uma constante zero transformam os dois corpos na cor atual e na anterior.
- Cada E lógico une a cor nova, a cor antiga oposta e a verificação de posição, e dispara uma entrada; os dois blocos de fechamento são acionados diretamente pelas comparações com a média móvel.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
